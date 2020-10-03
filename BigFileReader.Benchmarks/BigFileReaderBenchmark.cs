using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace BigFileReader.Benchmarks
{
    [MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter]
    [MemoryDiagnoser]
    public class BigFileReaderBenchmark
    {
        static void Main(string[] _) => BenchmarkRunner.Run<BigFileReaderBenchmark>();

        [Benchmark]
        public Task<int> GetLineNumberWithStreamAsync() =>
            Program.GetLineNumberUsingStreamAsync(Program.FileName, Program.SearchWord);

        [Benchmark]
        public Task<int> GetLineNumberWithPipeAsync() =>
            Program.GetLineNumberUsingPipeAsync(Program.FileName, Program.SearchWord);
    }
}
