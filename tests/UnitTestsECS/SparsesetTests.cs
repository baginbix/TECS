using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using TECS;
using TECS.Query;
using TECS.Tests;
using Xunit;

namespace UnitTestsECS
{
    [Query]
    public ref struct QueryPos
    {
        public ref Position pos;
    }
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
            var query = new Query<QueryPos>(ecs);
            foreach (var item in query)
            {
                count++;
            }

            Assert.Equal(spawnCount, count);
        }
    }
}