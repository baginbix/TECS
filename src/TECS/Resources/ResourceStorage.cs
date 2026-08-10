using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Resources
{
    public class ResourceStorage
    {
        private IResource _resource;
        private uint _changedTick = 0;

        public ResourceStorage(IResource resource)
        {
            _resource = resource;
        }

        public ref IResource GetResource()
        {
            return ref _resource;
        }

        public void UpdateLastTick(uint currentTick)
        {
            _changedTick = currentTick;
        }
        
    }
}