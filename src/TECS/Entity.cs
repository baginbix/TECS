using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS;
public readonly struct Entity : IEquatable<Entity>
{
    public readonly int Id;
    public readonly int Version;
    public Entity(int id, int version){
        Id = id;
        Version = version;
    }


    public bool Equals(Entity other) => Id == other.Id && other.Version == Version;
    public override bool Equals(object obj) 
    {
        return obj is Entity other && Equals(other);
    }

    public override int GetHashCode() 
    {
        return HashCode.Combine(Id, Version);
    }

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}