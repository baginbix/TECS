using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Event;

namespace TECS.Event
{
    public class EventWriter<T>(EventManager manager)  where T: struct
    {
        private EventStream<T> stream;

        public void Send (ref T data)
        {
            if(stream is null)
            {
                stream = manager.GetOrCreateEventStream<T>();
            }

            stream.Send(data);
        }
    }
}