using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Queries.Components;
public ref struct ComponentItem<T> where T: struct
{
    private readonly ref T component;
    private readonly ref uint tick;
    private readonly ulong globalTick;
    public ComponentItem(ref T component, ref uint tick, ulong globalTick)
    {
        this.component = ref component;
        this.tick = ref tick;
        this.globalTick = globalTick;
    }

    public ref readonly T Read => ref component;

    public ref T Write
    {
        get
        {
            tick = (uint)globalTick;
            return ref component;
        }
    }

}
