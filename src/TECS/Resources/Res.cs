using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Resources;

public readonly ref struct Res<T>
    where T : IResource
{
    private readonly ResourceStorage<T> storage;
    public ref readonly T Value => ref storage.GetResource();
    private readonly bool isChanged;

    public bool IsChanged => isChanged;

    public Res(ResourceStorage<T> storage, uint lastRunSystemTick)
    {
        this.storage = storage;
        isChanged = storage.LastChangedTick > lastRunSystemTick;
    }
}
