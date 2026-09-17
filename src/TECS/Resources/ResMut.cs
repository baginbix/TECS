using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Resources;

namespace TECS.Resources;

public ref struct ResMut<T>
    where T : IResource
{
    private readonly ref T storage;
    public ref T Value => ref storage;
    private readonly bool isChanged;

    public bool IsChanged => isChanged;

    public ResMut(ResourceStorage<T> storage, uint lastRunSystemTick)
    {
        this.storage = ref storage.GetResource();
        this.isChanged = storage.LastChangedTick > lastRunSystemTick;
    }
}
