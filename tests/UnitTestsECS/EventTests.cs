using System;
using TECS;
using TECS.Event;
using TECS.Query;
using Xunit;

namespace UnitTestsECS
{
    public record struct MockEvent(int damage);

    public static class TestEventSystems
    {
        public static bool EventSent = false;
        public static bool EventRead = false;

        [System]
        public static void MockSystemSendEvent(EventWriter<MockEvent> writer)
        {
            var e = new MockEvent(5);
        
            writer.Send(e);

            EventSent = true; // Mark as run for the test
        }

        [System]
        public static void MockSystemReadEvent(EventReader<MockEvent> reader)
        {
            // TODO: Hook up your new Event Reader logic here!
            reader.Read();

            EventRead = true; // Mark as run for the test
        }
    }

    public class EventTests
    {
        private App SetupApp()
        {
            var app = new App();

            // Register systems using your new auto-generated methods!
            app.AddSystem(TestEventSystems.MockSystemSendEvent);
            app.AddSystem(TestEventSystems.MockSystemReadEvent);

            return app;
        } 

        [Fact]
        public void EventSystems_ShouldRunSuccessfully()
        {
            // Reset static counters for the test
            TestEventSystems.EventSent = false;
            TestEventSystems.EventRead = false;

            App app = SetupApp();

            // Run one frame
            app.Run();

            // Verify both systems executed
            Assert.True(TestEventSystems.EventSent);
            Assert.True(TestEventSystems.EventRead);
        }
    }
}