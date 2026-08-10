using System;
using TECS;
using TECS.Event;
using TECS.Query;
using Xunit;

namespace UnitTestsECS
{
    public record struct MockEvent(int damage);

    // 1. Create a dummy query so the Roslyn Generator picks up the systems
    [Query]
    public ref struct EventQuery
    {
        public ref int Dummy; // This is just a placeholder to satisfy the generator
    }

    // 2. Define your systems using the new static attribute architecture
    public static class TestEventSystems
    {
        public static bool EventSent = false;
        public static bool EventRead = false;

        [System]
        public static void MockSystemSendEvent(Query<EventQuery> query, EventWriter<MockEvent> writer)
        {
            var e = new MockEvent(5);
            
            // TODO: Hook up your new Event Writer logic here!
            // Depending on your new design, it might look like this:
            writer.Send(e);

            EventSent = true; // Mark as run for the test
        }

        [System]
        public static void MockSystemReadEvent(Query<EventQuery> query, EventReader<MockEvent> reader)
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