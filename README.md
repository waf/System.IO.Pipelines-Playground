# System.IO.Pipelines Experimentation

A repo where I play around with System.IO.Pipelines and benchmark it with other approaches, namely streams.

This benchmark is solving the following problem: Given a 100MB text file, `BigFileReader/BigFile.txt`, find the word "giraffe" inside it and return its line number.

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i5-9400F CPU 2.90GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=3.1.402
  [Host]     : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
  DefaultJob : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
```

## Results

|                           Method |     Mean |   Error |  StdDev |      Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |---------:|--------:|--------:|-----------:|------:|------:|----------:|
|     GetLineNumberWithStreamAsync | 443.7 ms | 7.23 ms | 6.76 ms | 81000.0000 |     - |     - | 366.19 MB |
| GetLineNumberWithByteStreamAsync | 266.3 ms | 2.56 ms | 2.39 ms |   500.0000 |     - |     - |   4.38 MB |
|       GetLineNumberWithPipeAsync | 172.3 ms | 3.43 ms | 4.34 ms |  2000.0000 |     - |     - |   9.28 MB |

## Analysis

- GetLineNumberWithStreamAsync is the naive approach. It leads to high allocation and relatively slow execution time. I think this is because there is both a memory and cpu cost to the UTF8 encoding process (UTF8 is the default encoding in StreamReader).
- GetLineNumberWithByteStreamAsync is similar to the above, but tries to get rid of the UTF8 encoding. It's faster than the previous approach and uses the least amount of memory of all the algorithms. However, it's manually managing its byte array, so this will get complex and inefficient as the application grows, and adds more layers.
- GetLineNumberWithPipeAsync is the fastest of the bunch -- no surprise as it's born from the Kestrel project. I think the improvement in execution speed is mostly due to the `ReadOnlySpan<T>` use -- this struct has intelligent `IndexOf` algorithms that are SIMD-accelerated. Surprisingly, it allocated a bit more memory than the ByteStream approach. I think this may be due to the ArrayPool<T> usage internal to System.IO.Pipelines; if so then this should be constant and I'd expect memory use actually become more efficient as the application grows in complexity.
