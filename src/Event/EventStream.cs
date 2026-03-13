using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Event
{
    public struct EventStream<T>
    {
        private T[] events;
        private int count;

        public EventStream()
        {
            events = new T[10];
            count = 0;
        }

        public void Push(in T eventData)
        {
            if(count >= events.Length)
            {
                Array.Resize(ref events, events.Length * 2);
            }
            events[count++] = eventData;
        }

        public ReadOnlySpan<T> Read()
        {
            return new ReadOnlySpan<T>(events);
        }

        public void Clear()
        {
            count = 0;
        }
    }
}