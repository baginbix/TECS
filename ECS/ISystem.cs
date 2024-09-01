using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public interface ISystem
    {
        void Run(ECS app);
    }
}