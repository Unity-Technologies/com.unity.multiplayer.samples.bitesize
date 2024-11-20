using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using System;
using UnityEditor.Build;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class MultiNetworkObjectSpawner : NetworkBehaviour
    {
        public List<SpawnObjectLocations> SpawnObjectLocationList;

        private class ObjectSpawnInfo : INetworkSerializable, IEquatable<ObjectSpawnInfo>
        {
            public bool IsRespawning;
            public int TickToRespawn;
            public int ObjIndex;
            public int TabIndex;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref IsRespawning);
                serializer.SerializeValue(ref TickToRespawn);
                serializer.SerializeValue(ref ObjIndex);
                serializer.SerializeValue(ref TabIndex);
            }

            public bool Equals(ObjectSpawnInfo other)
            {
                return other.ObjIndex.Equals(ObjIndex) && other.TabIndex.Equals(TabIndex);
            }
        }

        NetworkVariable<Dictionary<int, List<ObjectSpawnInfo>>> m_SpawnedObjectsInfo = new NetworkVariable<Dictionary<int, List<ObjectSpawnInfo>>>(new Dictionary<int, List<ObjectSpawnInfo>>());


        protected override void OnNetworkSessionSynchronized()
        {
            if (IsSessionOwner)
            {
                var objectAndLocations = new Dictionary<int, List<ObjectSpawnInfo>>();
                var currentTick = NetworkManager.ServerTime.Tick;
                for (int i = 0; i < SpawnObjectLocationList.Count; i++)
                {
                    objectAndLocations.Add(i, new List<ObjectSpawnInfo>());
                    var spawnObjectAndLocations = SpawnObjectLocationList[i];
                    for (int j = 0; j < spawnObjectAndLocations.ObjectSpawnPoints.Count; j++)
                    {
                        objectAndLocations[i].Add(new ObjectSpawnInfo() { IsRespawning = true, TickToRespawn = currentTick, ObjIndex = j, TabIndex = objectAndLocations[i].Count });
                    }
                }
                m_SpawnedObjectsInfo.Value = objectAndLocations;
                NetworkManager.NetworkTickSystem.Tick += NetworkTick;
            }
            base.OnNetworkSessionSynchronized();
        }

        private void NetworkTick()
        {
            var currentTick = NetworkManager.ServerTime.Tick;
            bool shouldCheckDirtyState = false;
            foreach (var keyPair in m_SpawnedObjectsInfo.Value)
            {
                var objectsToSpawn = keyPair.Value.Where((c) => c.IsRespawning && c.TickToRespawn <= currentTick);
                var objectCount = objectsToSpawn.Count();
                if (objectCount == 0)
                {
                    continue;
                }
                else
                {
                    shouldCheckDirtyState = true;
                }

                var spawnObjectLocations = SpawnObjectLocationList[keyPair.Key];
                for (int i = 0; i < objectsToSpawn.Count(); i++)
                {
                    var objInfo = objectsToSpawn.ElementAt(i);
                    var spawnPointTransform = spawnObjectLocations.ObjectSpawnPoints[objInfo.ObjIndex].transform;
                    var spawnedNetworkObject = spawnObjectLocations.NetworkObjectToSpawn.InstantiateAndSpawn(NetworkManager, position: spawnPointTransform.position, rotation: spawnPointTransform.rotation);
                    var spawnable = spawnedNetworkObject.GetComponent<ISpawnable>();
                    spawnable.Init(this, keyPair.Key, objInfo.TabIndex);
                    objInfo.IsRespawning = false;
                    // Re-apply the struct with the change in its respawning state
                    m_SpawnedObjectsInfo.Value[keyPair.Key][objInfo.TabIndex] = objInfo;
                }
            }
            if (shouldCheckDirtyState)
            {
                m_SpawnedObjectsInfo.CheckDirtyState();
            }
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.NetworkTickSystem.Tick -= NetworkTick;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="respawnTime"> Network tick at which to respawn this NetworkObject prefab </param>
        [Rpc(SendTo.Authority)]
        public void RespawnRpc(int respawnTime, int Key, int Index)
        {
            var entry = m_SpawnedObjectsInfo.Value[Key][Index];
            entry.IsRespawning = true;
            entry.TickToRespawn = respawnTime;
            m_SpawnedObjectsInfo.Value[Key][Index] = entry;
            m_SpawnedObjectsInfo.CheckDirtyState();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (NetworkManager.LocalClientId == current)
            {
                NetworkManager.NetworkTickSystem.Tick += NetworkTick;
            }
            else
            {
                NetworkManager.NetworkTickSystem.Tick -= NetworkTick;
            }
        }
    }

    [Serializable]
    public class SpawnObjectLocations
    {
        public NetworkObject NetworkObjectToSpawn;
        public List<ObjectSpawnPoint> ObjectSpawnPoints;
    }
}
