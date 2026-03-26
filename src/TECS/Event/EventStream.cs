using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Event
{
    public interface IEventStream
    {
        public void Flush();
    }
    public class EventStream<T> : IEventStream where T : struct
    {
        private T[] events;
        private int activeCount;

        private int oldestEventId = 0;
        
        public int TotalEventsFired => activeCount + oldestEventId;

        private int eventsFromLastFrameCount = 0;

        public EventStream()
        {
            events = new T[16];
            activeCount = 0;
        }

        public void Send(in T eventData)
        {
            if (activeCount >= events.Length)
            {
                Array.Resize(ref events, events.Length * 2);
            }
            events[activeCount++] = eventData;
        }

        public ReadOnlySpan<T> Read()
        {
            return new ReadOnlySpan<T>(events);
        }

        public void Flush()
        {
            int newEventsThisFrame = activeCount - eventsFromLastFrameCount;
            
            if(eventsFromLastFrameCount == events.Length)
                activeCount = 0;
        }
    }
}