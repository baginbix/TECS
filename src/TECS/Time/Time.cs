namespace TECS.Time;

public class Time : IResource
{
    public float DeltaTime { get; internal set; }
    public float TotalTime { get; internal set; }
}