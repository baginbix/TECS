using System.Data.Common;

namespace src.Event;

public class EventReader<T> where T: struct
{
    private int lastReadEventId = 0;

    public ReadOnlySpan<T> Read(EventManager manager)
    {
        var stream = manager.GetOrCreateEventStream<T>();
        var span = stream.Read(out int oldestId, out int totalFired);

        if(lastReadEventId < oldestId)
        {
            lastReadEventId = oldestId;
        }
        
        if(lastReadEventId == totalFired)
        {
            return ReadOnlySpan<T>.Empty;
        }

        int unreadCount = totalFired - lastReadEventId;
        int startIndex = span.Length - unreadCount;

        lastReadEventId = totalFired;

        return span[startIndex..unreadCount];
    }
}