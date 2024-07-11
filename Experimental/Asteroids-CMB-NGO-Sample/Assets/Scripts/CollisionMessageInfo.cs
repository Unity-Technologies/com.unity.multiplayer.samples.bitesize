using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Only 15 types are allowed.
/// Applied to the first byte of <see cref="CollisionMessageInfo.Flags"/>
/// </summary>
public enum CollisionTypes
{
    Laser = 0x01,
    Missile = 0x02,
    Mine = 0x03,
    Asteroid = 0x04,
    Ship = 0x05,
    DebugCollision = 0x0F,
    // Maximum value for collision types is 15 - 0x0E
}

/// <summary>
/// Flags to be used with <see cref="CollisionMessageInfo.Flags"/>
/// 16, 32, 64, and 128 are used for flags
/// </summary>
public enum CollisionCategoryFlags
{
    // Four flags are reserved
    Standard = 0x10,
    CollisionForce = 0x20,
    CollisionPoint = 0x40,
}

public struct CollisionMessageInfo : INetworkSerializable
{
    /// <summary>
    /// Flags are serialized and determine the collision type
    /// </summary>
    public byte Flags;

    /// <summary>
    /// Damage is serialized and is set based on the type
    /// </summary>
    public ushort Damage;

    /// <summary>
    /// Collision force is serialized only if the flags contains the
    /// <see cref="CollisionCategoryFlags.CollisionForce"/> flag.
    /// </summary>
    public Vector3 CollisionForce;

    /// <summary>
    /// Collision force is serialized only if the flags contains the
    /// <see cref="CollisionCategoryFlags.CollisionPoint"/> flag.
    /// </summary>
    public Vector3 CollisionPoint;

    /// <summary>
    /// Never serialized, only used locally to determine if 
    /// we are 
    /// </summary>
    public bool DebugCollisionEnabled;

    public int CollisionId;
    public float Time;
    public ulong Source;
    public ulong Destination;
    public ulong DestNetworkObjId;
    public ushort DestBehaviourId;

    /// <summary>
    /// Never serialized, only used locally
    /// </summary>
    public ulong SourceId;

    /// <summary>
    /// Never serialized, only used locally
    /// </summary>
    public ulong SourceOwner;

    /// <summary>
    /// Never serialized, only used locally
    /// </summary>
    public ulong TargetOwner;

    public CollisionTypes GetCollisionType()
    {
        return (CollisionTypes)(Flags & 0x0F);
    }

    public bool HasCollisionForce()
    {
        return (Flags & (uint)CollisionCategoryFlags.CollisionForce) == (byte)CollisionCategoryFlags.CollisionForce;
    }

    private bool DebugCollisionsEnabled
    {
        get
        {
            return GetFlag((uint)CollisionTypes.DebugCollision);
        }

        set
        {
            SetFlag(value, (uint)CollisionTypes.DebugCollision);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(bool set, uint flag)
    {
        var flags = (uint)Flags;
        if (set) { flags = flags | flag; }
        else { flags = flags & ~flag; }
        Flags = (byte)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetFlag(uint flag)
    {
        var flags = (uint)Flags;
        return (flags & flag) != 0;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Flags);
        serializer.SerializeValue(ref Damage);
        // only if we have a collision force should we serialize it (read and write)
        if (GetFlag((uint)CollisionCategoryFlags.CollisionForce))
        {
            serializer.SerializeValue(ref CollisionForce);
        }

        if (GetFlag((uint)CollisionCategoryFlags.CollisionPoint))
        {
            serializer.SerializeValue(ref CollisionForce);
        }

        if (DebugCollisionsEnabled)
        {
            serializer.SerializeValue(ref CollisionId);
            serializer.SerializeValue(ref Time);
            serializer.SerializeValue(ref Source);
            serializer.SerializeValue(ref Destination);
            serializer.SerializeValue(ref DestNetworkObjId);
            serializer.SerializeValue(ref DestBehaviourId);
        }
    }
}
