using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace TECS
{
    public unsafe struct Bitset : IEquatable<Bitset>
    {

        /* Cache-line is 64 bytes so the array fits inside 2 chache-lines.
        *  This allows us to store up to 512 bits while still being very fast to copy and compare.
        */
        public fixed ulong parts[8];

        public Bitset()
        {
            for(int i = 0; i < 8; i++){
                parts[i] = 0;
            }
        }

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly bool Intersects(ref Bitset b) // Changed from 'in' to 'ref'
{
    // Grab raw references to the first element to bypass 'fixed' GC pinning
    ref ulong aRef = ref Unsafe.As<Bitset,ulong>(ref Unsafe.AsRef(in this));
    ref ulong bRef = ref Unsafe.As<Bitset,ulong>(ref b);

    if (Vector256.IsHardwareAccelerated)
    {
        ref Vector256<ulong> aVec = ref Unsafe.As<ulong, Vector256<ulong>>(ref aRef); 
        ref Vector256<ulong> bVec = ref Unsafe.As<ulong, Vector256<ulong>>(ref bRef);

        if((aVec & bVec) != Vector256<ulong>.Zero) return true;

        if((Unsafe.Add(ref aVec, 1) & Unsafe.Add(ref bVec, 1)) != Vector256<ulong>.Zero) return true;

        return false; 
    }
    for (int i = 0; i < 8; i++)
    {
        if ((Unsafe.Add(ref aRef, i) & Unsafe.Add(ref bRef, i)) != 0) 
            return true;
    }
    return false;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly bool ContainsAll(ref Bitset b) // Changed from 'in' to 'ref'
{
       // Grab raw references to the first element to bypass 'fixed' GC pinning
    ref ulong aRef = ref Unsafe.As<Bitset,ulong>(ref Unsafe.AsRef(in this));
    ref ulong bRef = ref Unsafe.As<Bitset,ulong>(ref b);

    if (Vector256.IsHardwareAccelerated)
    {
        ref Vector256<ulong> aVec = ref Unsafe.As<ulong, Vector256<ulong>>(ref aRef); 
        ref Vector256<ulong> bVec = ref Unsafe.As<ulong, Vector256<ulong>>(ref bRef);

        if((aVec & bVec) != bVec) return false;

        if((Unsafe.Add(ref aVec, 1) & Unsafe.Add(ref bVec, 1)) != Unsafe.Add(ref bVec, 1)) return false;

        return true; 
    }
    for (int i = 0; i < 8; i++)
    {
        ulong bVal = Unsafe.Add(ref bRef, i); // Micro-optimization: cache the B value
        if ((Unsafe.Add(ref aRef, i) & bVal) != bVal) 
            return false;
    }
    return true;
}

        public static bool operator ==(Bitset a, Bitset b)
        {
            for(int i = 0; i < 8; i++){
                if(a.parts[i] != b.parts[i]) return false;
            }
            return true;
        }

        public static bool operator !=(Bitset a, Bitset b) => !(a == b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int position)
        {
            // position >> 6 finds the ulong index.
            // 1ul << position automatically applies the modulo 64 at the hardware level!
            parts[position >> 6] |= 1ul << position; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBit(int position)
        {
            parts[position >> 6] &= ~(1ul << position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBit(int position)
        {
            return (parts[position >> 6] & (1ul << position)) != 0;
        }

        public void ClearBits(){
            for(int i = 0; i < 8; i++){
                parts[i] = 0;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsEmpty()
        {
            for(int i = 0; i < 8; i++){
                if(parts[i] != 0) return false;
            }
            return true;
        }

        public bool Equals(Bitset other) => this == other;

        public override bool Equals(object? obj) => obj is Bitset other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            for(int i = 0; i < 8; i++){
                hash.Add(parts[i]);
            }
            return hash.ToHashCode();
        }
        
    }
}