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
        private T[] events = new T[16];
        private int activeCount = 0;

        private int oldestEventId = 0;
        
        public int TotalEventsFired => activeCount + oldestEventId;

        private int eventsFromLastFrameCount = 0;

        public void Send(in T eventData)
        {
            if (activeCount >= events.Length)
            {
                Array.Resize(ref events, events.Length * 2);
            }
            events[activeCount++] = eventData;
        }
        
        public ReadOnlySpan<T> Read(out int oldestId, out int totalFired)
        {
            oldestId = oldestEventId;
            totalFired = TotalEventsFired;
            return new ReadOnlySpan<T>(events,0,activeCount);
        }

        public void Flush()
        {
            int newEventsThisFrame = activeCount - eventsFromLastFrameCount;
            
            if(eventsFromLastFrameCount == events.Length)
                activeCount = 0;
        }
    }
}