namespace src.Event
{
    public class EventManager
    {
        Dictionary<Type,IEventStream> eventStreams = new();
        public void SendEvent<T>(in T e) where T: struct
        {
            GetOrCreateEventStream<T>().Send(e);
        }

        public ReadOnlySpan<IEvent> ReadEvents<IEvent>() where IEvent:struct
        {
            var eventStream = GetOrCreateEventStream<IEvent>();
            return eventStream.Read();
        }
        private EventStream<T> GetOrCreateEventStream<T>() where T: struct
        {
            IEventStream eventStream;
            if(!eventStreams.TryGetValue(typeof(T), out eventStream)){
                eventStreams.Add(typeof(T), new EventStream<T>());
                eventStream = eventStreams[typeof(T)];
            }

            return (EventStream<T>)eventStream;
        }

        public void Flush()
        {
            foreach(var eventStream in eventStreams)
            {
                eventStream.Value.Flush();
            }
        }
    }
}