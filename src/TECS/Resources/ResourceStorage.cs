using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Resources
{
    public class IResourceStorage;

    public class ResourceStorage<T> : IResourceStorage
    {
        private T _resource;
        private uint _changedTick = 0;

        public ResourceStorage(T resource)
        {
            _resource = resource;
        }

        public ref T GetResource()
        {
            return ref _resource;
        }

        public void UpdateLastTick(uint currentTick)
        {
            _changedTick = currentTick;
        }
    }
}
