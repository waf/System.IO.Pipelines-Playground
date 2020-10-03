# System.IO.Pipelines Experimentation

A repo where I play around with System.IO.Pipelines and benchmark it with other approaches, namely streams.

**Current status**: Using System.IO.Pipelines leads to very low memory usage. Currently trying to figure out why it's slower than streams.

This benchmark is solving the following problem: Given a 100MB text file, `BigFileReader/BigFile.txt`, find the word "giraffe" inside it and return its line number.

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.329 (2004/?/20H1)
Intel Core i5-9400 CPU 2.90GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=5.0.100-preview.6.20318.15
  [Host]     : .NET Core 3.1.7 (CoreCLR 4.700.20.36602, CoreFX 4.700.20.37001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.7 (CoreCLR 4.700.20.36602, CoreFX 4.700.20.37001), X64 RyuJIT


```
|                       Method |     Mean |   Error |  StdDev |      Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |---------:|--------:|--------:|-----------:|------:|------:|----------:|
| GetLineNumberWithStreamAsync | 435.6 ms | 5.37 ms | 4.48 ms | 82000.0000 |     - |     - | 366.19 MB |
|   GetLineNumberWithPipeAsync | 619.8 ms | 4.01 ms | 3.75 ms |  2000.0000 |     - |     - |   9.28 MB |
