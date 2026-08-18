using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TECS;
using TECS.Tests;
using Xunit;

namespace UnitTestsECS
{
    public class EntityTests
    {

        [Fact]
        public void Engine_CreateValidEntities_GenerateUniqueEntites()
        {
            var targetCount = 100_000;
            var ecs = new ECS();
            HashSet<Entity> entites = new(targetCount);
            for (int i = 0; i < targetCount; i++)
            {
                var entity = ecs.CreateEntity();
                var unique = entites.Add(entity);
                Assert.True(unique, $"Duplicate entity '{entity}' detected at iteration {i}.");
            }

            Assert.Equal(targetCount, entites.Count);
        }

        [Fact]
        public void DestroyEntity_RecycleId_AddBumpsVersion()
        {
            var ecs = new ECS();
            
            // Create first one adn destroy it
            var e1 = ecs.CreateEntity();

            ecs.DestroyEntity(e1);
            // Now that one has been destroyed it should be reused, 
            // but with a new generation
            var e2 = ecs.CreateEntity();

            Assert.Equal(e1.Id, e2.Id);
            Assert.NotEqual(e1.Version, e2.Version);
            Assert.False(ecs.IsEntityAlive(e1));
            Assert.True(ecs.IsEntityAlive(e2));
        }

        [Fact]
        public void DestroyEntity_RemovesAllAttachedComponents()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position{X=10});

            ecs.DestroyEntity(entity);

            Assert.True(ecs.QueryComponent<Position>(entity).IsNone);
            Assert.Empty(ecs.GetComponentList<Position>());
        }

        [Fact]
        public void DestroyEntity_CannotAccessNewEntityData()
        {
            var ecs = new ECS();
            var staleHandle = ecs.CreateEntity();

            ecs.DestroyEntity(staleHandle);
            var freshEntity = ecs.CreateEntity();
            ecs.InsertComponent(freshEntity, new Position{X = 42});
            Assert.True(ecs.QueryComponent<Position>(staleHandle).IsNone);
        }

        [Fact]
        public void BulkCreateAndDestroy_MaintainsCorrectActiveCount()
        {
            var ecs = new ECS();
            var activeEntities = new List<Entity>();

            for(int i = 0; i < 1_000; i++)
            {
                activeEntities.Add(ecs.CreateEntity());
            }

            for(int i = activeEntities.Count-1; i >= 0; i -= 2)
            {
                ecs.DestroyEntity(activeEntities[i]);
                activeEntities.RemoveAt(i);
            }

            Assert.All(activeEntities, entity => Assert.True(ecs.IsEntityAlive(entity)));
        }
    }
}