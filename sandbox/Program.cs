// See https://aka.ms/new-console-template for more information
using TECS;

ECS ecs = new();
var e = ecs.CreateEntity();
ecs.InsertComponent(e, new A());
var q = ecs.Query<A>();
q.ForEach((Entity e, ref A a) =>
{
    
});


struct A{}