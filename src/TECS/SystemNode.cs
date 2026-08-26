using TECS.Systems;

namespace TECS
{
    public class SystemNode
    {
        public SystemItem System;
        public List<SystemNode> Dependents;
        public int InitialDependencyCount;
        public int CurrentDependencyCount;

        public SystemNode(SystemItem system)
        {
            System = system;
            Dependents = new();
            InitialDependencyCount = 0;
        }
    }
}
