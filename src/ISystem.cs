using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    public interface ISystem 
    {
        void Run(ECS ecs, ref CommandBuffer cmd);
    }
}