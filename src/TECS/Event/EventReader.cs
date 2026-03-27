using System.Data.Common;

namespace src.Event;

public class EventReader<T> where T: struct
{
    private int lastReadEventId = 0;

    public ReadOnlySpan<T> Read(EventManager manager)
    {
        var stream = manager.GetOrCreateEventStream<T>();
        var readData = stream.Read();

        if(lastReadEventId < readData.OldestID)
        {
            lastReadEventId = readData.OldestID;
        }
        
        if(lastReadEventId == readData.TotalFired)
        {
            return ReadOnlySpan<T>.Empty;
        }

        int unreadCount = readData.TotalFired - lastReadEventId;
        int startIndex = readData.Data.Length - unreadCount;

        lastReadEventId = readData.TotalFired;

        return readData.Data[startIndex..unreadCount];
    }
}