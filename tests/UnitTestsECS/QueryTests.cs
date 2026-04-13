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

        public struct PlayerTag { }
        public struct Frozen { }
        
        private ECS SetupTestEcs()
        {
            var ecs = new ECS();

            Entity e0 = ecs.CreateEntity();
            ecs.InsertComponent(e0, new Position());
            ecs.InsertComponent(e0, new Velocity());
            ecs.InsertComponent(e0, new Health());
            ecs.InsertComponent(e0, new PlayerTag());

            Entity e1 = ecs.CreateEntity();
            ecs.InsertComponent(e1, new Position());
            ecs.InsertComponent(e1, new Velocity());
            ecs.InsertComponent(e1, new Health());

            Entity e2 = ecs.CreateEntity();
            ecs.InsertComponent(e2, new Position());
            ecs.InsertComponent(e2, new Velocity());
            ecs.InsertComponent(e2, new Health());
            ecs.InsertComponent(e2, new Frozen());

            Entity e3 = ecs.CreateEntity();
            ecs.InsertComponent(e3, new Position());
            ecs.InsertComponent(e3, new Health());
            ecs.InsertComponent(e3, new PlayerTag());

            return ecs;
        }

        [Fact]
        public void Query_CombinedFilters_WorksCorrectly()
        {
            var ecs = SetupTestEcs();

            int executionCount = 0;

            // Using the Enumerator with filters!
            foreach (var item in ecs.Query<Position, Velocity, Health>().With<PlayerTag>().Without<Frozen>())
            {
                executionCount++;
            }

            Assert.Equal(1, executionCount);
        }

        [Fact]
        public void Query_NoFilters_ProcessesAllValidEntities()
        {
            var ecs = SetupTestEcs();
            int executionCount = 0;

            foreach (var item in ecs.Query<Position, Velocity, Health>())
            {
                executionCount++;
            }

            Assert.Equal(3, executionCount);
        }

        [Fact]
        public void Query_ChangeDetection_ShouldOnlyReturnModifiedEntities()
        {
            var ecs = new ECS();
            
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();

            ecs.InsertComponent(e1, new Position { X = 1 });
            ecs.InsertComponent(e2, new Position { X = 2 });

            // Assuming you have an API to advance the global tick or run a system frame
            ecs.NextTick(); // Or however your engine steps time forward

            // Modify ONLY e1
            // Assuming GetValue returns a ref, and updates the entity's tick
            ref var pos = ref ecs.GetOrCreateSet<Position>().GetValue(e1, ecs.GlobalTick).Unwrap();
            pos.X = 99;

            int modifiedCount = 0;
            
            // Assuming your Query API has a .Changed<T>() filter
            foreach (var item in ecs.Query<Position>().Changed())
            {
                modifiedCount++;
            }

            // Assert it ONLY grabbed e1, skipping e2
            Assert.Equal(1, modifiedCount);
        }
    }
}