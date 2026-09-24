using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS;
public struct Tick : IEquatable<Tick>, IEquatable<uint>
{
    private uint _tick;
    public uint CurrentTick
    {
        get => _tick;
        private set
        {
            _tick = value;
        }
    }
    public Tick(uint startTick = 0) => _tick = startTick;

    public bool Equals(Tick other) => _tick == other.CurrentTick ;
    public bool Equals(uint other) => _tick == other ;
    public override bool Equals(object? obj) 
    {
        return obj is Tick other && Equals(other);
    }

    public override int GetHashCode() 
    {
        return HashCode.Combine(_tick);
    }

    /// <summary>
    /// Implicit conversion from uint to Tick
    /// <code>
    /// Tick t = 5;
    /// </code>
    /// </summary>
    /// <param name="tick"></param>
    public static implicit operator Tick(uint tick) => new Tick(tick);
    public static implicit operator uint(Tick tick) => tick._tick;

    
    public static bool operator >(Tick left, Tick right) => left._tick > right._tick;
    public static bool operator <(Tick left, Tick right) => left._tick < right._tick;

    public static bool operator >(Tick left, uint right) => left._tick > right;
    public static bool operator <(Tick left, uint right) => left._tick < right;
    
    public static bool operator >(uint left, Tick right) => left > right._tick;
    public static bool operator <(uint left, Tick right) => left < right._tick;
    
    public static bool operator >=(Tick left, Tick right) => left._tick >= right._tick;
    public static bool operator <=(Tick left, Tick right) => left._tick <= right._tick;

    public static bool operator >=(Tick left, uint right) => left._tick >= right;
    public static bool operator <=(Tick left, uint right) => left._tick <= right;

    public static bool operator >=(uint left, Tick right) => left >= right._tick;
    public static bool operator <=(uint left, Tick right) => left <= right._tick;

    public static Tick operator +(Tick t, uint value) => t._tick + value;
    public static Tick operator -(Tick t, uint value) => t._tick - value;

    public static Tick operator ++(Tick t) => t._tick + 1;
    public static Tick operator --(Tick t) => t._tick -1;
    
    public static bool operator ==(Tick left, Tick right) => left.Equals(right);
    public static bool operator !=(Tick left, Tick right) => !left.Equals(right);

    public static bool operator ==(Tick left, uint right) => left.Equals(right);
    public static bool operator !=(Tick left, uint right) => !left.Equals(right);

    public static bool operator ==(uint left, Tick right) => right.Equals(left);
    public static bool operator !=(uint left, Tick right) => !right.Equals(left);

    public override string ToString()
    {
        return $"Current tick: {_tick}";
    }
}
