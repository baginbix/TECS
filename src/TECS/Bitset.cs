using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace TECS
{
    public unsafe struct Bitset : IEquatable<Bitset>
    {

        /* Cache-line is 64 bytes so the array fits inside 2 chache-lines.
        *  This allows us to store up to 1024 bits while still being very fast to copy and compare.
        */
        public fixed ulong parts[16];

        public Bitset()
        {
            for(int i = 0; i < 16; i++){
                parts[i] = 0;
            }
        }

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly bool Intersects(ref Bitset b) // Changed from 'in' to 'ref'
{
    // Grab raw references to the first element to bypass 'fixed' GC pinning
    ref ulong aRef = ref Unsafe.AsRef(in parts[0]);
    ref ulong bRef = ref Unsafe.AsRef(in b.parts[0]);

    for (int i = 0; i < 16; i++)
    {
        if ((Unsafe.Add(ref aRef, i) & Unsafe.Add(ref bRef, i)) != 0) 
            return true;
    }
    return false;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly bool ContainsAll(ref Bitset b) // Changed from 'in' to 'ref'
{
    ref ulong aRef = ref Unsafe.AsRef(in parts[0]);
    ref ulong bRef = ref Unsafe.AsRef(in b.parts[0]);

    for (int i = 0; i < 16; i++)
    {
        if ((Unsafe.Add(ref aRef, i) & Unsafe.Add(ref bRef, i)) != Unsafe.Add(ref bRef, i)) 
            return false;
    }
    return true;
}

        public static bool operator ==(Bitset a, Bitset b)
        {
            for(int i = 0; i < 16; i++){
                if(a.parts[i] != b.parts[i]) return false;
            }
            return true;
        }

        public static bool operator !=(Bitset a, Bitset b) => !(a == b);

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
        public readonly bool IsEmpty()
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
            HashCode hash = new HashCode();
            for(int i = 0; i < 16; i++){
                hash.Add(parts[i]);
            }
            return hash.ToHashCode();
        }
        
    }
}