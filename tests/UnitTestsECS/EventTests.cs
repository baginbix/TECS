
using TECS;
using TECS.Commands;
using Xunit;

namespace UnitTestsECS
{
    public record struct MockEvent(int damage);
    public struct MockSystemSendEvent: ISystem
    {
        public void Run(ECS ecs, ref CommandBuffer cmd)
        {
            ecs.SendEvent<MockEvent>(new());
        }
    }
    public struct MockSystemReadEvent : ISystem
    {
         public void Run(ECS ecs, ref CommandBuffer cmd)
        {
            var allEvents = ecs.ReadEvents<MockEvent>();
            allEvents.Read();
        }
    }
    public class EventTests
    {
        private App SetupApp()
        {
           return new App(10)
           .AddSystem< MockSystemSendEvent>()
           .AddSystem<MockSystemReadEvent>();

        } 
        [Fact]
        public void Test1()
        {
            App app = SetupApp();

            app.Run();
            Assert.True(app.GetType)
        }
    }
}