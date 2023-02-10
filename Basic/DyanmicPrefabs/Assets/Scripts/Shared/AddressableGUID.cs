using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace Game
{
    public struct AddressableGUID : INetworkSerializable
    {
        // could use a byte or a short if client and server has the same list in the same order (use the index in the list instead of the actualy 128 bits for a full GUID)
        public FixedString128Bytes Value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
    
    public class AddressableGUIDEqualityComparer : IEqualityComparer<AddressableGUID>
    {
        public int GetHashCode(AddressableGUID addressableGUID)
        {
            return addressableGUID.GetHashCode();
        }

        public bool Equals(AddressableGUID x, AddressableGUID y)
        {
            return x.GetHashCode().Equals(y.GetHashCode());
        }
    }
}
