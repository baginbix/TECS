
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

int id2 = app.CreatEntity();
app.InsertComponent(id2, new Position{X = 1,Y = 10}); 


int id3 = app.CreatEntity();
app.InsertComponent(id3, new Position{X = 10,Y = 10}); 
app.InsertComponent(id3, new Velocity{X = 1, Y = 0}); 
app.InsertComponent(id3, new Health{max = 10, current = 10}); 
var group = app.HasAll<Position,Velocity,Health>();
app.AddSystem(app =>  {
    var positions = app.HasAll<Position,Velocity>();

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

struct Health{
    public int max;
    public int current;
}



