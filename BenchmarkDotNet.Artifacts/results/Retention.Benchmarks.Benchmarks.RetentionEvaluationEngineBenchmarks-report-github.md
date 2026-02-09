```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.7623)
11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-INNTTD : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=10  WarmupCount=3  

```
| Method                                            | Mean        | Error      | StdDev     | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------------------------- |------------:|-----------:|-----------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
| &#39;Small (~50 deployments, 5 projects, 3 envs)&#39;     |    24.24 μs |   6.056 μs |   4.005 μs | 0.008 |    0.00 |   6.0425 |   0.2441 |       - |   37.07 KB |        0.01 |
| &#39;Medium (~600 deployments, 20 projects, 5 envs)&#39;  |   311.16 μs |   3.391 μs |   2.018 μs | 0.106 |    0.00 |  73.2422 |  19.0430 |       - |  450.43 KB |        0.14 |
| &#39;Large (~6000 deployments, 50 projects, 10 envs)&#39; | 2,948.29 μs | 182.633 μs | 108.682 μs | 1.001 |    0.05 | 496.0938 | 351.5625 | 89.8438 | 3208.66 KB |        1.00 |
