namespace src.Event;

public class EventReader<T> where T: struct
{
    private int cursor;

    public ReadOnlySpan<T> Read(EventManager manager)
    {
        var allEvents = manager.GetAllEvents<T>();
        int eventAmount = allEvents.Length;
        if (cursor > eventAmount)
        {
            cursor = 0;
        }

        if (cursor == eventAmount)
        {
            return ReadOnlySpan<T>.Empty;
        }

        var newEvents = allEvents[cursor..];
        cursor = eventAmount;
        return newEvents;
    }
}