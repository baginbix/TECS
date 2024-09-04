
using System;
using BenchmarkDotNet.Running;
using ECS.ECS;
using ECS.ECS.Tests.Performance;


ECS.ECS.ECS app = new();
var id1 = app.CreatEntity();
app.InsertComponent(id1, new Position{X = 10,Y = 10}); 
app.InsertComponent(id1, new Velocity{X = 1, Y = 0}); 

int id = app.CreatEntity();
app.InsertComponent(id, new Position{X = 1,Y = 10}); 
app.InsertComponent(id, new Velocity{X = 1, Y = 0}); 
app.RemoveComponent<Position>(id1);
app.AddSystem(app =>  {
    var positions = app.QueryComponent<Position>();
    foreach (var position in positions){
        Console.WriteLine(position.X);
    }
});
app.Run();
using var game = new ECS.Game1();
//game.Run();


//var summary = BenchmarkRunner.Run<PerformanceTest>();
struct Position{
    public int X;
    public int Y;
}

struct Velocity{
    public int X;
    public int Y;
}
