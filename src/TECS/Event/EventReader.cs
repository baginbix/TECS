using System.Data.Common;

namespace TECS.Event;

interface IEventReader{}
public class EventReader<T>(EventManager manager) : IEventReader where T: struct
{
    private int lastReadEventId = 0;
 

    public ReadOnlySpan<T> Read(  )
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