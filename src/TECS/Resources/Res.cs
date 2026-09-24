using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Resources;

public readonly ref struct Res<T>
    where T : IResource
{
    private readonly ref T storage;
    public ref readonly T Value => ref storage;
    private readonly bool isChanged;

    public bool IsChanged => isChanged;

    public Res(ResourceStorage<T> storage, Tick lastRunSystemTick)
    {
        this.storage = ref storage.GetResource();
        isChanged = storage.LastChangedTick > lastRunSystemTick;
    }
}
