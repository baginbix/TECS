using System;
using System.Collections.Generic;
using Xunit;
using TECS;

namespace TECS.Tests
{
    // 1. Dummy Components & Resources for testing
    public struct Position
    {
        public float X, Y;
    }

    public struct Velocity
    {
        public float X, Y;
    }

    public struct Health
    {
        public int Value;
    }

    public class TimeResource
    {
        public float DeltaTime;
    }

    struct MoveAction : IQueryAction<Position>
    {
        public void Execute(ref Position pos)
        {
            pos.X += 1;
            pos.Y += 1;
        }
    }
    class MoveSystem : ISystem
    {
        public void Run(ECS ecs)
        {
            var query = ecs.Query<Position>();
            query.ForEach(new MoveAction());
        }
    }

    public class ECSTests
    {
        [Fact]
        public void CreateEntity_ShouldReturnUniqueEntities()
        {
            // Arrange
            var ecs = new ECS();

            // Act
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();

            // Assert
            Assert.NotEqual(e1.Id, e2.Id); // Assuming Entity has an Id property
        }

        [Fact]
        public void InsertAndGetResource_ShouldStoreAndRetrieveData()
        {
            // Arrange
            var ecs = new ECS();
            var time = new TimeResource { DeltaTime = 0.016f };

            // Act
            ecs.InsertResource(time);
            var retrievedTime = ecs.GetResource<TimeResource>();

            // Assert
            Assert.NotNull(retrievedTime);
            Assert.Equal(0.016f, retrievedTime.DeltaTime);
        }

        [Fact]
        public void InsertComponent_ShouldAddComponentToDenseList()
        {
            // Arrange
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();

            // Act
            ecs.InsertComponent(e1, new Position { X = 10, Y = 20 });
            List<Position> positions = ecs.QueryComponent<Position>();

            // Assert
            Assert.Single(positions);
            Assert.Equal(10, positions[0].X);
            Assert.Equal(20, positions[0].Y);
        }

        [Fact]
        public void RemoveComponent_ShouldRemoveFromSparseSet()
        {
            // Arrange
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            ecs.InsertComponent(e1, new Position { X = 5, Y = 5 });

            // Act
            ecs.RemoveComponent<Position>(e1);

            // Wait, your QueryComponent<T> throws KeyNotFoundException if the set doesn't exist yet,
            // but since we inserted it above, the set exists.
            List<Position> positions = ecs.QueryComponent<Position>();

            // Assert
            Assert.Empty(positions);
        }

        [Fact]
        public void HasAll_TwoComponents_ShouldReturnOnlyEntitiesWithBoth()
        {
            // Arrange
            var ecs = new ECS();

            Entity e1 = ecs.CreateEntity(); // Only Position
            ecs.InsertComponent(e1, new Position());

            Entity e2 = ecs.CreateEntity(); // Only Velocity
            ecs.InsertComponent(e2, new Velocity());

            Entity e3 = ecs.CreateEntity(); // Both (The intersection!)
            ecs.InsertComponent(e3, new Position());
            ecs.InsertComponent(e3, new Velocity());

            // Act
            List<Entity> intersection = ecs.HasAll<Position, Velocity>();

            // Assert
            Assert.Single(intersection);
            Assert.Equal(e3.Id, intersection[0].Id);
        }

        [Fact]
        public void Query_RefStruct_ShouldMutateDataCorrectly()
        {
            // Arrange
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();

            ecs.InsertComponent(e1, new Position { X = 0, Y = 0 });
            ecs.InsertComponent(e1, new Velocity { X = 5, Y = 5 });

            // Act
            var query = ecs.Query<Position, Velocity>();

            // Run the ForEach we built earlier!
            query.ForEach((ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.X;
                pos.Y += vel.Y;
            });

            // Assert: Check if the actual memory was modified
            var updatedPositions = ecs.QueryComponent<Position>();
            Assert.Equal(5, updatedPositions[0].X);
            Assert.Equal(5, updatedPositions[0].Y);
        }

        [Fact]
        public void Query_ISystem_ShouldMutateDataCorrectly()
        {

            //Arrange
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            ecs.InsertComponent(e1, new Position { X = 0, Y = 0 });
            ecs.AddSystem(new MoveSystem());

            //Act
            ecs.Run();

            // Assert: Check if the actual memory was modified
            var updatedPositions = ecs.QueryComponent<Position>();
            Assert.Equal(1, updatedPositions[0].X);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveComponentsFromDenseArray()
        {
            // Arrange
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();

            ecs.InsertComponent(e1, new Position { X = 10, Y = 10 });
            ecs.InsertComponent(e2, new Position { X = 20, Y = 20 });

            // Act
            ecs.DestroyEntity(e1.Id);

            // Assert
            // e1 is destroyed, so e2 should be the only component left, 
            // and because of Swap-and-Pop, e2 should now be at index 0!
            var positions = ecs.QueryComponent<Position>();

            Assert.Single(positions);
            Assert.Equal(20, positions[0].X);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveEntityFromQueries()
        {
            // Arrange
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();
            Entity e3 = ecs.CreateEntity();

            // Give all three entities both components
            ecs.InsertComponent(e1, new Position());
            ecs.InsertComponent(e1, new Velocity());

            ecs.InsertComponent(e2, new Position());
            ecs.InsertComponent(e2, new Velocity());

            ecs.InsertComponent(e3, new Position());
            ecs.InsertComponent(e3, new Velocity());

            // Act
            ecs.DestroyEntity(e2.Id); // Destroy the middle one

            // Assert
            int loopCount = 0;
            var query = ecs.Query<Position, Velocity>();

            query.ForEach((ref Position pos, ref Velocity vel) =>
            {
                loopCount++;
            });

            // The query should only run for e1 and e3
            Assert.Equal(2, loopCount);
        }
    }
}