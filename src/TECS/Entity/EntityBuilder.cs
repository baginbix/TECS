using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Commands;

namespace TECS.Entities
{
    public readonly ref struct EntityBuilder
    {
        private readonly CommandBuffer cmd;

        public Entity Id { get; init; }

        public EntityBuilder(CommandBuffer cmd, Entity entityId)
        {
            this.cmd = cmd;
            Id = entityId;
        }

        public EntityBuilder With<T>(T component) where T : struct
        {
            cmd.InsertComponent(component, Id);
            return this;
        }

        /// <summary>
        /// Implicitly converts the EntityBuilder to an Entity, allowing you to directly assign it to an Entity variable without calling a build method.
        /// </summary>
        /// <param name="builder"></param>
        public static implicit operator Entity(EntityBuilder builder) => builder.Id;

    }
}