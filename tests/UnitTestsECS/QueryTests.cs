using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS;
using Xunit;

namespace UnitTestsECS
{
    public class QueryTests
    {
        public struct Position { public float X; }
        public struct Velocity { public float Dx; }
        public struct Health { public int Hp; }

        // "Zero-byte" tag components for filtering
        public struct PlayerTag { }
        public struct Frozen { }
        private ECS SetupTestEcs()
        {
            var ecs = new ECS();

            // Entity 0: The Player (Has Everything + PlayerTag)
            int e0 = ecs.CreateEntity();
            ecs.InsertComponent(e0, new Position());
            ecs.InsertComponent(e0, new Velocity());
            ecs.InsertComponent(e0, new Health());
            ecs.InsertComponent(e0, new PlayerTag());

            // Entity 1: Normal Enemy (Missing PlayerTag)
            int e1 = ecs.CreateEntity();
            ecs.InsertComponent(e1, new Position());
            ecs.InsertComponent(e1, new Velocity());
            ecs.InsertComponent(e1, new Health());

            // Entity 2: Frozen Enemy (Has Frozen tag)
            int e2 = ecs.CreateEntity();
            ecs.InsertComponent(e2, new Position());
            ecs.InsertComponent(e2, new Velocity());
            ecs.InsertComponent(e2, new Health());
            ecs.InsertComponent(e2, new Frozen());

            // Entity 3: A Rock (Missing Velocity entirely, but has PlayerTag just to trick it)
            int e3 = ecs.CreateEntity();
            ecs.InsertComponent(e3, new Position());
            ecs.InsertComponent(e3, new Health());
            ecs.InsertComponent(e3, new PlayerTag());

            return ecs;
        }

        [Fact]
        public void Query_WithFilter_OnlyProcessesEntitiesWithTag()
        {
            // Arrange
            var ecs = SetupTestEcs();
            int executionCount = 0;

            // Act
            ecs.Query<Position, Velocity, Health>()
               .With<PlayerTag>()
               .ForEach((ref Position p, ref Velocity v, ref Health h) =>
               {
                   executionCount++;
               });

            // Assert
            // Should ONLY hit Entity 0. 
            // Entity 1 lacks PlayerTag. Entity 3 lacks Velocity.
            Assert.Equal(1, executionCount);
        }

        [Fact]
        public void Query_WithoutFilter_SkipsExcludedEntities()
        {
            // Arrange
            var ecs = SetupTestEcs();
            int executionCount = 0;

            // Act
            ecs.Query<Position, Velocity, Health>()
               .Without<Frozen>()
               .ForEach((ref Position p, ref Velocity v, ref Health h) =>
               {
                   executionCount++;
               });

            // Assert
            // Should hit Entity 0 and Entity 1. 
            // Entity 2 has Frozen. Entity 3 is missing Velocity.
            Assert.Equal(2, executionCount);
        }

        [Fact]
        public void Query_CombinedFilters_WorksCorrectly()
        {
            // Arrange
            var ecs = SetupTestEcs();

            // Let's add PlayerTag to the Frozen enemy to test complex overlap
            ecs.InsertComponent(2, new PlayerTag());

            int executionCount = 0;

            // Act
            ecs.Query<Position, Velocity, Health>()
               .With<PlayerTag>()
               .Without<Frozen>()
               .ForEach((ref Position p, ref Velocity v, ref Health h) =>
               {
                   executionCount++;
               });

            // Assert
            // Entity 0: Has PlayerTag, NOT Frozen -> (Pass)
            // Entity 1: Missing PlayerTag -> (Fail)
            // Entity 2: Has PlayerTag, BUT is Frozen -> (Fail)
            // Entity 3: Missing Velocity -> (Fail)
            Assert.Equal(1, executionCount);
        }

        [Fact]
        public void Query_NoFilters_ProcessesAllValidEntities()
        {
            // Arrange
            var ecs = SetupTestEcs();
            int executionCount = 0;

            // Act
            ecs.Query<Position, Velocity, Health>()
               .ForEach((ref Position p, ref Velocity v, ref Health h) =>
               {
                   executionCount++;
               });

            // Assert
            // Should hit Entities 0, 1, and 2. 
            // Entity 3 is skipped purely because it lacks a Velocity component.
            Assert.Equal(3, executionCount);
        }
    }
}