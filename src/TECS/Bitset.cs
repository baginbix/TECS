using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TECS
{
    public unsafe struct Bitset : IEquatable<Bitset>
    {
        public ulong bits;

        /* Cache-line is 64 bytes so the array fits inside 2 chache-lines.
        *  This allows us to store up to 1024 bits while still being very fast to copy and compare.
        */
        public fixed ulong parts[16];

        public Bitset()
        {
            bits = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitset operator &(Bitset a, Bitset b)
        {
            Bitset result = new();
            for(int i = 0; i < 16; i++){
                result.parts[i] = a.parts[i] & b.parts[i];
            }
            return result;
        }

        public static bool operator ==(Bitset a, Bitset b)
        {
            for(int i = 0; i < 16; i++){
                if(a.parts[i] != b.parts[i]) return false;
            }
            return true;
        }

        public static bool operator !=(Bitset a, Bitset b) => !(a == b);

        public static implicit operator long(Bitset bitset) => (long)bitset.bits;

        public void SetBit(int position){
            int part = position >> 6; // Divide by 64 to find the part index
            int bitPosition = position & 63; // Modulo 64 to find the bit position within the part

            parts[part] |= 1ul << bitPosition; // Set the specific bit in
        }

        public void ClearBit(int position){
            int part = position >> 6; // Divide by 64 to find the part index
            int bitPosition = position & 63; // Modulo 64 to find the bit position within the part

            parts[part] &= ~(1ul << bitPosition); // Clear the specific bit in
        }

        public void ClearBits(){
            for(int i = 0; i < 16; i++){
                parts[i] = 0;
            }
        }

        public bool HasBit(int position){
            int part = position >> 6; // Divide by 64 to find the part index
            int bitPosition = position & 63; // Modulo 64 to find the bit position within the part

            return (parts[part] & (1ul << bitPosition)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty()
        {
            for(int i = 0; i < 16; i++){
                if(parts[i] != 0) return false;
            }
            return true;
        }

        public bool Equals(Bitset other) => this == other;

        public override bool Equals(object? obj) => obj is Bitset other && Equals(other);

        public override int GetHashCode()
        {
            int hash = bits.GetHashCode();
            for(int i = 0; i < 16; i++){
                hash = HashCode.Combine(hash, parts[i].GetHashCode());
            }
            return hash;
        }
        
    }
}