using System;
using Unity.Netcode;

namespace Game
{
    public struct AddressableGUIDCollection : INetworkSerializable
    {
        public AddressableGUID[] GUIDs;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Length
            int length = 0;
            if (!serializer.IsReader)
            {
                length = GUIDs.Length;
            }

            serializer.SerializeValue(ref length);

            // Array
            if (serializer.IsReader)
            {
                GUIDs = new AddressableGUID[length];
            }

            for (int n = 0; n < length; ++n)
            {
                serializer.SerializeValue(ref GUIDs[n]);
            }
        }
        
        public override int GetHashCode()
        {
            int value = 0;
            for (var i = 0;i< this.GUIDs.Length; i++)
            {
                value = HashCode.Combine(this.GUIDs[i],value);
            }

            return value;
        }

        public unsafe int GetSizeInBytes()
        {
            return sizeof(AddressableGUID) * GUIDs.Length;
        }
    }
}
