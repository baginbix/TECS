using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace TECS.Commands
{
    public interface ICommandQueue
    {
        void Flush(ECS ecs, Span<Entity> mappedIDs);

    }
}