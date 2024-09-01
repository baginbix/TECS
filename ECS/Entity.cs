using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public struct Entity
    {
        public int Id{get;}
        public Entity(int id){
            Id = id;
        }


        public bool Equals(Entity other) => Id == other.Id;
        public bool Equals(int other) => Id == other;
        public override int GetHashCode() => Id.GetHashCode();

        // Implicit conversion to int
        public static implicit operator int(Entity entity) => entity.Id;
        public static implicit operator Entity(int id) => new Entity(id);
    }
}