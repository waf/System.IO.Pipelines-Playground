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
        public const byte NewLine = (byte)'\n';

        static async Task Main(string[] _)
        {
            Console.WriteLine(await GetLineNumberUsingStreamAsync(FileName, SearchWord));
            Console.WriteLine(await GetLineNumberUsingByteStreamAsync(FileName, SearchWord));
            Console.WriteLine(await GetLineNumberUsingPipeAsync(FileName, SearchWord));
        }

        /// <summary>
        /// Implementation 1: Stream-based approach, StreamReader
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
        /// Implementation 2: Stream-based approach, FileStream
        /// </summary>
        public static async Task<int> GetLineNumberUsingByteStreamAsync(string file, string searchWord)
        {
            var searchBytes = Encoding.UTF8.GetBytes(searchWord);
            using var fileStream = File.OpenRead(file);

            int lineNumber = 1;
            int keyRun = 0;
            int keyRunMax = searchWord.Length;

            byte[] buffer = new byte[4096];
            while (await fileStream.ReadAsync(buffer, 0, buffer.Length) is int bytesRead && bytesRead > 0)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    byte b = buffer[i];
                    if (b == NewLine)
                    {
                        lineNumber++;
                    }
                    else if (b == searchBytes[keyRun])
                    {
                        keyRun++;

                        if (keyRun == keyRunMax) return lineNumber;

                        continue;
                    }

                    keyRun = 0;
                }
            }
            return -1;
        }

        /// <summary>
        /// Implementation 3: System.IO.Pipelines-based approach
        /// </summary>
        public static async Task<int> GetLineNumberUsingPipeAsync(string file, string searchWord)
        {
            var searchBytes = Encoding.UTF8.GetBytes(searchWord);
            using var fileStream = File.OpenRead(file);
            var pipe = PipeReader.Create(fileStream, new StreamPipeReaderOptions(bufferSize: 4096));

            var lineNumber = 1;
            while (true)
            {
                ReadResult readResult = await pipe.ReadAsync().ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                if (TryFindBytesInBuffer(buffer, searchBytes, ref lineNumber))
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
            while (bufferReader.TryReadTo(out ReadOnlySpan<byte> line, NewLine, advancePastDelimiter: true))
            {
                if (line.IndexOf(searchBytes) >= 0)
                    return true;

                lineNumber++;
            }
            return false;
        }
    }
}
