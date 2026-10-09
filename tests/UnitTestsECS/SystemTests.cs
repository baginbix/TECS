using TECS;
using TECS.Resources;
using TECS.Scheduler.Labels;
using TECS.Systems;
using Xunit;

namespace UnitTestsECS
{
    public class SystemTests
    {
        public class Test : IResource
        {
            public int value;
        }

        [System]
        public static void ResSystem(Res<Test> testRes) { }

        [Fact]
        public void Test1()
        {
            App app = new App();
            app.AddSystem<Update>(ResSystem);
            Assert.True(true);
        }
    }
}
