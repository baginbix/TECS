using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perfolizer.Mathematics.Multimodality;

namespace ECS.ECS
{
    public struct Bitset
    {
        public ulong bits;

        public Bitset(){
            bits = 0;
        }

        public static implicit operator long(Bitset bitset) => bitset;

        public void SetBit(int position){
            bits |= 1ul << position;
        }

        public void ClearBit(int position){
            bits &= ~(1ul << position);
        }

        public void ClearBits(){
            bits = 0;
        }
    }
}