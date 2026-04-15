using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS;
using TECS.Tests;
using Xunit;

namespace UnitTestsECS
{
    public class EntityTests
    {
        [Fact]
        public void Engine_ShouldRejectStaleEntities()
        {
            var ecs = new ECS();
            
            // 1. Create an entity and save it
            Entity target = ecs.CreateEntity();
            
            // 2. Destroy it (this recycles the ID but bumps the Version)
            ecs.DestroyEntity(target);
            
            // 3. Create a new entity (this will reuse the ID but have Version 1)
            Entity newEntity = ecs.CreateEntity();
            
            // 4. Try to add a component to the OLD 'target' variable
            // Your engine should either throw an exception, or safely ignore this!
            // (Depending on how you wrote InsertComponent, you might need to assert an exception here)
            ecs.InsertComponent(target, new Position { X = 50 });

            // Assert the NEW entity didn't accidentally get the Position!
            Assert.False(ecs.GetComponentList<Position>().Count == 0);
        }
    }
}