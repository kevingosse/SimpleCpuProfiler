using Silhouette;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CpuProfiler;
internal class CorProfiler : CorProfilerCallback11Base
{
    private Timer? _timer;
    private readonly ConcurrentDictionary<ThreadId, int> _threads = new();

    protected override HResult Initialize(int iCorProfilerInfoVersion)
    {
        if (iCorProfilerInfoVersion < 10)
        {
            return HResult.E_FAIL;
        }

        var result = ICorProfilerInfo.SetEventMask(COR_PRF_MONITOR.COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS);

        if (result)
        {
            _timer = new Timer(OnTick, null, 0, 2000);
        }

        return result;
    }

    protected override HResult ThreadAssignedToOSThread(ThreadId managedThreadId, int osThreadId)
    {
        _threads[managedThreadId] = osThreadId;
        return HResult.S_OK;
    }

    protected override HResult ThreadDestroyed(ThreadId threadId)
    {
        _threads.TryRemove(threadId, out _);
        return HResult.S_OK;
    }

    private unsafe void OnTick(object? _)
    {
        var result = ICorProfilerInfo10.SuspendRuntime();

        if (!result)
        {
            return;
        }

        StackWalkContext context = default;

        foreach (var (threadId, osThreadId) in _threads)
        {
            context.Count = 0;

            result = ICorProfilerInfo2.DoStackSnapshot(threadId, &WalkThread, COR_PRF_SNAPSHOT_INFO.COR_PRF_SNAPSHOT_DEFAULT, &context, null, 0);

            if (!result)
            {
                Console.WriteLine($"[Profiler] Stackwalk failed for thread {osThreadId}: {result}");
                continue;
            }

            Console.WriteLine($"[Profiler] Thread {osThreadId}:");

            for (int i = 0; i < context.Count; i++)
            {
                var ip = context.Frames[i];
                string name = ResolveMethodName(ip);
                Console.WriteLine($"[Profiler] - {name}");
            }
        }

        _ = ICorProfilerInfo10.ResumeRuntime();
    }

    private string ResolveMethodName(nint ip)
    {
        try
        {
            var functionId = ICorProfilerInfo.GetFunctionFromIP(ip).ThrowIfFailed();
            var functionInfo = ICorProfilerInfo.GetFunctionInfo(functionId).ThrowIfFailed();
            using var metaDataImport = ICorProfilerInfo.GetModuleMetaData(functionInfo.ModuleId, CorOpenFlags.ofRead, KnownGuids.IMetaDataImport).ThrowIfFailed().Wrap();
            var methodProperties = metaDataImport.Value.GetMethodProps(new MdMethodDef(functionInfo.Token)).ThrowIfFailed();
            var typeDefProps = metaDataImport.Value.GetTypeDefProps(methodProperties.Class).ThrowIfFailed();

            return $"{typeDefProps.TypeName}.{methodProperties.Name}";
        }
        catch (Win32Exception)
        {
            return "<unknown>";
        }
    }

    private struct StackWalkContext
    {
        public const int MaxFrames = 1024;
        public FramesArray Frames;
        public int Count;

        [InlineArray(MaxFrames)]
        public struct FramesArray
        {
            private nint _frames;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe HResult WalkThread(FunctionId funcId, nint ip, COR_PRF_FRAME_INFO frameInfo, uint contextSize, byte* context, void* clientData)
    {
        ref StackWalkContext stackWalkContext = ref *(StackWalkContext*)clientData;

        if (stackWalkContext.Count >= StackWalkContext.MaxFrames)
        {
            return HResult.E_ABORT;
        }

        stackWalkContext.Frames[stackWalkContext.Count++] = ip;

        return HResult.S_OK;
    }
}
