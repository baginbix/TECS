using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Event;

namespace TECS.Event
{
    public interface IEventWriter
    {

    }
    public class EventWriter<T>(EventManager manager):IEventWriter  where T: struct
    {
        private EventStream<T> stream;

        public void Send (T data)
        {
            if(stream is null)
            {
                stream = manager.GetOrCreateEventStream<T>();
            }

            stream.Send(data);
        }
    }
}