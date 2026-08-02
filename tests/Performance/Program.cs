// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using PerformanceTests;

var summary = BenchmarkRunner.Run<EcsBenchmarks>();