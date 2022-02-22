using BenchmarkDotNet.Running;

namespace SimpleRequestLogger.Benchmarks
{
    public class Program
    {
        public static void Main() => BenchmarkRunner.Run<Benchmarks>();
    }
}
