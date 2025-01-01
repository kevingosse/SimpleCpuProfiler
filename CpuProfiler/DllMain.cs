using Silhouette;
using System.Runtime.InteropServices;

namespace CpuProfiler;
internal class DllMain
{
    private static ClassFactory? _instance;

    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject")]
    public static unsafe HResult DllGetClassObject(Guid* rclsid, Guid* riid, nint* ppv)
    {
        if (*rclsid != new Guid("0A96F866-D763-4099-8E4E-ED1801BE9FBC"))
        {
            return HResult.E_NOINTERFACE;
        }
        _instance = new Silhouette.ClassFactory(new CorProfiler());
        *ppv = _instance.IClassFactory;

        return HResult.S_OK;
    }
}
