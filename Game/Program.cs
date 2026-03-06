
using System;
using System.Collections;
using System.Collections.Generic;
using BenchmarkDotNet.Running;

using var game = new TestGame.Game1();
game.Run();

//var summary = BenchmarkRunner.Run<PerformanceTest>();