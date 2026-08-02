using System;
using TECS;
using TECS.Query; 
using Xunit;

namespace UnitTestsECS
{
        // 1. Define Components and Tags
        public struct Position { public float X; }
        public struct Velocity { public float Dx; }
        public struct Health { public int Hp; }
        public struct PlayerTag { }
        public struct Frozen { }

        // 2. Define the Query Struct with the required attribute!
        [Query]
        public ref struct MovementQuery
        {
            public ref Position Pos;
            public ref Velocity Vel;
            public ref Health Hp;
        }

        // 3. Define the Systems for testing
        public static class TestSystems
        {
            public static int ExecutionCount = 0;

            [System]
            public static void CountMovementSystem(Query<MovementQuery> query)
            {
                foreach (var item in query)
                {
                    ExecutionCount++;
                }
            }
        }
    public class QueryTests
    {

        private App SetupTestApp()
        {
            var app = new App();

            // We can now access app.ecs directly because it is 'internal'
            
            Entity e0 = app.ecs.CreateEntity();
            app.ecs.InsertComponent(e0, new Position());
            app.ecs.InsertComponent(e0, new Velocity());
            app.ecs.InsertComponent(e0, new Health());
            app.ecs.InsertComponent(e0, new PlayerTag());

            Entity e1 = app.ecs.CreateEntity();
            app.ecs.InsertComponent(e1, new Position());
            app.ecs.InsertComponent(e1, new Velocity());
            app.ecs.InsertComponent(e1, new Health());

            Entity e2 = app.ecs.CreateEntity();
            app.ecs.InsertComponent(e2, new Position());
            app.ecs.InsertComponent(e2, new Velocity());
            app.ecs.InsertComponent(e2, new Health());
            app.ecs.InsertComponent(e2, new Frozen());

            Entity e3 = app.ecs.CreateEntity();
            app.ecs.InsertComponent(e3, new Position());
            app.ecs.InsertComponent(e3, new Health());
            app.ecs.InsertComponent(e3, new PlayerTag());

            return app;
        }

        [Fact]
        public void System_ProcessesAllValidEntities()
        {
            TestSystems.ExecutionCount = 0;
            var app = SetupTestApp();
            
            // Register using the auto-generated method
            app.AddSystem(TestSystems.CountMovementSystem);


            // Run one frame[cite: 2]
            app.Run();

            // e0, e1, and e2 have all required components. e3 is missing Velocity.
            Assert.Equal(3, TestSystems.ExecutionCount);
        }
    }
}