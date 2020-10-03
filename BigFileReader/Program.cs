using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace BigFileReader
{
    public class Program
    {
        public const string FileName = "BigFile.txt";
        public const string SearchWord = "giraffe";

        static async Task Main(string[] _)
        {
            Console.WriteLine(await GetLineNumberUsingStreamAsync(FileName, SearchWord));
            Console.WriteLine(await GetLineNumberUsingPipeAsync(FileName, SearchWord));
        }

        /// <summary>
        /// Implementation 1: Stream-based approach
        /// </summary>
        public static async Task<int> GetLineNumberUsingStreamAsync(string file, string searchWord)
        {
            using var fileStream = File.OpenRead(file);
            using var lines = new StreamReader(fileStream, bufferSize: 4096);

            int lineNumber = 1;
            while (await lines.ReadLineAsync() is string line) // ReadLine returns null on stream end, exiting the loop
            {
                if (line.Contains(searchWord))
                    return lineNumber;

                lineNumber++;
            }
            return -1;
        }

        /// <summary>
        /// Implementation 2: System.IO.Pipelines-based approach
        /// </summary>
        public static async Task<int> GetLineNumberUsingPipeAsync(string file, string searchWord)
        {
            var searchBytes = Encoding.UTF8.GetBytes(searchWord);
            using var fileStream = File.OpenRead(file);
            var pipe = PipeReader.Create(fileStream, new StreamPipeReaderOptions(bufferSize: 4096));

            var lineNumber = 1;
            while (true)
            {
                var readResult = await pipe.ReadAsync().ConfigureAwait(false);
                var buffer = readResult.Buffer;

                if(TryFindBytesInBuffer(buffer, searchBytes, ref lineNumber))
                {
                    return lineNumber;
                }

                pipe.AdvanceTo(buffer.End);

                if (readResult.IsCompleted) break;
            }

            await pipe.CompleteAsync();

            return -1;
        }

        /// <summary>
        /// Look for <paramref name="searchBytes"/> in <paramref name="buffer"/>, incrementing the <paramref name="lineNumber"/> every
        /// time we find a new line.
        /// </summary>
        /// <returns>true if we found the sequence, false otherwise</returns>
        private static bool TryFindBytesInBuffer(in ReadOnlySequence<byte> buffer, byte[] searchBytes, ref int lineNumber)
        {
            var bufferReader = new SequenceReader<byte>(buffer);
            while (TryReadLine(ref bufferReader, out var line))
            {
                if (ContainsBytes(ref line, searchBytes))
                    return true;

                lineNumber++;
            }
            return false;
        }

        private static bool TryReadLine(ref SequenceReader<byte> bufferReader, out SequenceReader<byte> line)
        {
            var foundNewLine = bufferReader.TryReadTo(out ReadOnlySequence<byte> match, (byte)'\n', advancePastDelimiter: true);
            if (!foundNewLine)
            {
                line = default;
                return false;
            }

            line = new SequenceReader<byte>(match);
            return true;
        }

        private static bool ContainsBytes(ref SequenceReader<byte> line, in ReadOnlySpan<byte> searchBytes)
        {
            return line.TryReadTo(out var _, searchBytes);
        }
    }
}
