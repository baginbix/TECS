using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TECS;
using TECS.Resources;
using Xunit;

namespace UnitTestsECS
{
    public class ResourceTests
    {
        private sealed class TestResource : IResource
        {
            public int Value { get; set; }
        }

        private static ResourceStorage<TestResource> GetResourceStorage(ECS ecs) { 
            var resourcesField = typeof(ECS).GetField("resources", BindingFlags.Instance | BindingFlags.NonPublic); 
            var resources = (Dictionary<Type, IResourceStorage>)resourcesField!.GetValue(ecs)!; 
            return (ResourceStorage<TestResource>)resources[typeof(TestResource)]; 
        }

        [Fact]
        public void Res_IsNotChanged_WhenResourceWasNotChangedSinceLastRun()
        {
            var ecs = new ECS();
            ecs.InsertResource(new TestResource { Value = 10 });

            var storage = GetResourceStorage(ecs);

            storage.UpdateLastTick(0);

            var res = new Res<TestResource>(storage, 0);

            Assert.False(res.IsChanged);
            Assert.Equal(10, res.Value.Value);
        }

        [Fact]
        public void Res_IsChanged_WhenResourceChangedAfterLastRun()
        {
            var ecs = new ECS();
            ecs.InsertResource(new TestResource { Value = 10 });

            var storage = GetResourceStorage(ecs);

            storage.UpdateLastTick(5);

            var res = new Res<TestResource>(storage, 3);

            Assert.True(res.IsChanged);
        }

        [Fact]
        public void Res_IsNotChanged_WhenLastChangedTickEqualsLastRunTick()
        {
            var ecs = new ECS();
            ecs.InsertResource(new TestResource { Value = 10 });

            var storage = GetResourceStorage(ecs);

            storage.UpdateLastTick(5);

            var res = new Res<TestResource>(storage, 5);
            Assert.False(res.IsChanged);
        }

        [Fact]
        public void ResMut_CanModifyResource()
        {
            var ecs = new ECS();
            ecs.InsertResource(new TestResource { Value = 10 });

            var storage = GetResourceStorage(ecs);

            var resMut = new ResMut<TestResource>(storage, 0);
            resMut.Value.Value = 42;

            Assert.Equal(42, ecs.GetResource<TestResource>().Value);
        }
    }
}