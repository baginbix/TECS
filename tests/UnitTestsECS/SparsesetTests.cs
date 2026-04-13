using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS;
using TECS.Tests;
using Xunit;

namespace UnitTestsECS
{
    public class SparsesetTests
    {
        [Fact]
        public void SparseSet_ShouldHandlePageBoundaries()
        {
            var ecs = new ECS();
            
            // PAGE_SIZE is 4096. Let's spawn 5000 to force a page allocation!
            int spawnCount = 5000; 
            
            for (int i = 0; i < spawnCount; i++)
            {
                Entity e = ecs.CreateEntity();
                ecs.InsertComponent(e, new Position { X = i });
            }

            // Assert all 5000 entities can be iterated successfully
            int count = 0;
            foreach (var item in ecs.Query<Position>())
            {
                count++;
            }

            Assert.Equal(spawnCount, count);
        }
    }
}