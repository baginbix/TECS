using System;
using System.Collections.Generic;
using Xunit;
using TECS;
using TECS.Commands;

namespace TECS.Tests
{
    public struct Position { public float X, Y; }
    public struct Velocity { public float X, Y; }
    public struct Health { public int Value; }
    public class TimeResource : IResource { public float DeltaTime; }

    class MoveSystem : ISystem
    {
        public void Run(IEngine ecs, CommandBuffer cmd)
        {
            foreach (var item in ecs.Query<Position>())
            {
                // You can mutate items directly here now!
            }
        }
    }

    public class ECSTests
    {
        [Fact]
        public void CreateEntity_ShouldReturnUniqueEntities()
        {
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();
            Assert.NotEqual(e1.Id, e2.Id);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveComponentFromDenseArray()
        {
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();

            ecs.InsertComponent<Position>(e1, new Position { X = 10, Y = 10 });
            ecs.InsertComponent<Position>(e2, new Position { X = 20, Y = 20 });

            // Act
            ecs.DestroyEntity(e1);

            // Assert
            var positions = ecs.GetComponentList<Position>();
            Assert.Single(positions);
            Assert.Equal(20, positions[0].X);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveEntityFromQueries()
        {
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();
            Entity e3 = ecs.CreateEntity();

            ecs.InsertComponent<Position>(e1, new Position());
            ecs.InsertComponent<Velocity>(e1, new Velocity());

            ecs.InsertComponent<Position>(e2, new Position());
            ecs.InsertComponent<Velocity>(e2, new Velocity());

            ecs.InsertComponent<Position>(e3, new Position());
            ecs.InsertComponent<Velocity>(e3, new Velocity());

            // Act
            ecs.DestroyEntity(e2); // Destroy the middle one

            // Assert
            int loopCount = 0;
            
            // Replaced ForEach with the blazing fast Enumerator!
            foreach (var item in ecs.Query<Position, Velocity>())
            {
                loopCount++;
            }

            Assert.Equal(2, loopCount);
        }
    }
}