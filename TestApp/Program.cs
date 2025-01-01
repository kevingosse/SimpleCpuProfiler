namespace TestApp;

internal class Program
{
    static void Main(string[] args)
    {
        var threads = new Thread[]
        {
            new Thread(Method1) { IsBackground = true },
            new Thread(Method2) { IsBackground = true },
            new Thread(Method3) { IsBackground = true },
            new Thread(Method4) { IsBackground = true },
            new Thread(Method5) { IsBackground = true }
        };

        foreach (var thread in threads)
        {
            thread.Start();
        }

        Console.WriteLine("Hello, World!");
        Console.ReadLine();
    }

    static void Method1()
    {
        Thread.Sleep(Timeout.Infinite);
    }

    static void Method2()
    {
        Thread.Sleep(Timeout.Infinite);
    }

    static void Method3()
    {
        Thread.Sleep(Timeout.Infinite);
    }

    static void Method4()
    {
        Thread.Sleep(Timeout.Infinite);
    }

    static void Method5()
    {
        Thread.Sleep(Timeout.Infinite);
    }
}
