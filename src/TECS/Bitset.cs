using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    public record struct Bitset
    {
        public ulong bits;

        public Bitset(){
            bits = 0;
        }

        public static implicit operator long(Bitset bitset) => (long)bitset.bits;

        public void SetBit(int position){
            bits |= 1ul << position;
        }

        public void ClearBit(int position){
            bits &= ~(1ul << position);
        }

        public void ClearBits(){
            bits = 0;
        }

        public bool HasBit(int position){
            return (bits & (1ul << position)) != 0;
        }
    }
}