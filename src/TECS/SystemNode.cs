using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    public class SystemNode
    {
        public SystemBinding System;
        public List<SystemNode> Dependents;
        public int InitialDependencyCount;
        public int CurrentDependencyCount;

        public SystemNode(SystemBinding system)
        {
            System = system;
            Dependents = new();
            InitialDependencyCount = 0;
        }
    }
}