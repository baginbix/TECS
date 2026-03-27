namespace src.Event
{
    public class EventManager
    {
        Dictionary<Type,IEventStream> eventStreams = new();
        public void SendEvent<T>(in T e) where T: struct
        {
            GetOrCreateEventStream<T>().Send(e);
        }
        internal EventStream<T> GetOrCreateEventStream<T>() where T: struct
        {
            IEventStream eventStream;
            if(!eventStreams.TryGetValue(typeof(T), out eventStream)){
                eventStream = new EventStream<T>();
                eventStreams.Add(typeof(T), eventStream);
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