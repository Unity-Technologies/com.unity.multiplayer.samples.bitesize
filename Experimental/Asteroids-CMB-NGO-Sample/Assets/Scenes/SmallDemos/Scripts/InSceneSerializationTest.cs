using System;
using System.Text;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class InSceneSerializationTest : NetworkBehaviour
{
    private struct SerializableTypesData : INetworkSerializable, IEquatable<SerializableTypesData>
    {
        public bool SomeBool;
        public byte SomeByte;
        public char SomeChar;
        public short SomeShort;
        public ushort SomeUShort;
        public int SomeInt;
        public uint SomeUInt;
        public long SomeLong;
        public ulong SomeULong;
        public float SomeFloat;
        public double SomeDouble;
        public string SomeString;
        private byte[] StringData;

         public void PopulateSerializableTypes()
        {
            SomeBool = Random.Range(0, 1) == 1;
            SomeByte = (byte)Random.Range(byte.MinValue, byte.MaxValue);
            SomeChar = (char)Random.Range(byte.MinValue, byte.MaxValue);
            SomeShort = (short)Random.Range(short.MinValue, short.MaxValue);
            SomeUShort = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
            SomeInt = Random.Range(int.MinValue, int.MaxValue);
            SomeUInt = (uint)Random.Range(uint.MinValue, uint.MaxValue);
            SomeLong = Random.Range(int.MinValue, int.MaxValue);
            SomeULong = (ulong)Random.Range(uint.MinValue, uint.MaxValue);
            SomeFloat = Random.Range(uint.MinValue, uint.MaxValue);
            SomeDouble = Random.Range(uint.MinValue, uint.MaxValue);
            SomeString = NetworkManagerHelper.GetRandomString(Random.Range(0, 128));
        }

        private void SerializeString<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var stringDataSize = 0;
            if (serializer.IsWriter)
            {
                if (!string.IsNullOrEmpty(SomeString))
                {
                    StringData = Encoding.UTF8.GetBytes(SomeString);
                    stringDataSize = StringData.Length;
                }
                serializer.SerializeValue(ref stringDataSize);
                if (stringDataSize > 0)
                {
                    serializer.SerializeValue(ref StringData);
                }
            }
            else
            {
                serializer.SerializeValue(ref stringDataSize);
                if (stringDataSize > 0)
                {
                    StringData = new byte[stringDataSize];
                    serializer.SerializeValue(ref StringData);
                    SomeString = Encoding.UTF8.GetString(StringData);
                }
                else
                {
                    SomeString = string.Empty;
                }
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SomeBool);
            serializer.SerializeValue(ref SomeByte);
            serializer.SerializeValue(ref SomeChar);
            serializer.SerializeValue(ref SomeShort);
            serializer.SerializeValue(ref SomeUShort);
            serializer.SerializeValue(ref SomeInt);
            serializer.SerializeValue(ref SomeUInt);
            serializer.SerializeValue(ref SomeLong);
            serializer.SerializeValue(ref SomeULong);
            serializer.SerializeValue(ref SomeFloat);
            serializer.SerializeValue(ref SomeDouble);
            SerializeString(serializer);
        }

        public bool Equals(SerializableTypesData other)
        {
            var success = other.SomeBool.Equals(SomeBool);
            success = success && other.SomeByte.Equals(SomeByte);
            success = success && other.SomeChar.Equals(SomeChar);
            success = success && other.SomeShort.Equals(SomeShort);
            success = success && other.SomeUShort.Equals(SomeUShort);
            success = success && other.SomeInt.Equals(SomeInt);
            success = success && other.SomeUInt.Equals(SomeUInt);
            success = success && other.SomeLong.Equals(SomeLong);
            success = success && other.SomeULong.Equals(SomeULong);
            success = success && other.SomeFloat.Equals(SomeFloat);
            success = success && other.SomeDouble.Equals(SomeDouble);
            // Check and handle null or empty string
            if (!string.IsNullOrEmpty(SomeString))
            {
                success = success && other.SomeString.Equals(SomeString);
            }
            else
            {
                success = success && string.IsNullOrEmpty(other.SomeString);
            }
            return success;
        }
    }

    private NetworkVariable<SerializableTypesData> m_SerializableTypesData = new();

    public override void OnNetworkSpawn()
    {
        if (IsSessionOwner)
        {
            var newData = new SerializableTypesData();
            newData.PopulateSerializableTypes();
            m_SerializableTypesData.Value = newData;
        }
        base.OnNetworkSpawn();
    }

    protected override void OnNetworkPostSpawn()
    {
        if (!IsSessionOwner)
        {
            ValidateDataRpc(m_SerializableTypesData.Value);
        }
        base.OnNetworkPostSpawn();
    }

    [Rpc(SendTo.Authority)]
    private void ValidateDataRpc(SerializableTypesData data, RpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        if (!data.Equals(m_SerializableTypesData.Value))
        {
            NetworkManagerHelper.Instance.LogMessage($"[Client-{senderId}][Validate - {nameof(SerializableTypesData)}][Failed]");
        }
        else
        {
            NetworkManagerHelper.Instance.LogMessage($"[Client-{senderId}][Validate - {nameof(SerializableTypesData)}][Success]");
        }
    }
}
