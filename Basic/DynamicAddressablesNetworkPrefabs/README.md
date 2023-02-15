# Dynamic Addressables Network Prefabs Sample

![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2021.3%20LTS-57b9d3.svg?logo=unity&color=2196F3)
![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-1.2.0-57b9d3.svg?logo=unity&color=2196F3)
[![LatestRelease](https://img.shields.io/badge/Latest%20%20Github%20Release:-v1.2.0-57b9d3.svg?logo=github&color=brightgreen)](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/releases/tag/v1.2.0)
<br><br>

The Dynamic Prefabs Sample showcases the available use-cases for the dynamic prefab system, which allows us to add new spawnable prefabs at runtime. This sample uses Addressables to load the dynamic prefab, however any GameObject with a NetworkObject component can be used, regardless of its source. This sample also uses in-game UI (created using UI Toolkit) to interface with the dynamic prefabs system with configurable options like artificial latency and network spawn timeout for easy testing.
<br><br>

# Sample Overview

In this sample, learn more about:
- The dynamic prefabs system
- Loading prefabs with addressables
<br><br>

## Exploring the Sample

Each scene in the project showcases a different, isolated feature of the API, allowing for easy extraction into other projects. We suggest exploring them in order to get a good understanding of the flow of dynamically loading and spawning network prefabs. The use-cases available in this sample are based around the current known limitations.
<br><br>

### Each Scene:

- Scene 00_Preloading Dynamic Prefabs
  - This is the simplest case of a dynamic prefab - we instruct all game instances to load a network prefab (it can be just one, it could also be a set of network prefabs) and inject them to NetworkManager's NetworkPrefabs list before starting the server.
  <br><br>

- Scene 01_Connection Approval Required For Late Joining
  - An optional use-case scenario that walks through what a server would need to validate from a client when dynamically loading network prefabs. Other use-cases don't allow for reconciliation after the server has loaded a prefab dynamically, whereas this one enables this functionality. 
  <br><br>

- Scene 02_Server Authoritative Preload All Prefabs Asynchronously
  - A simple use-case where the server notifies all clients to preload a collection of network prefabs. The server will not invoke a spawn in this use-case, and will incrementally load each dynamic prefab, one prefab at a time.
  <br><br>

- Scene 03_Server Authoritative Try Spawn Synchronously
  - A dynamic prefab loading use-case where the server instructs all clients to load a single network prefab, and will only invoke a spawn once all clients have successfully completed their respective loads of said prefab. The server will initially send a ClientRpc to all clients, begin loading the prefab on the server, will await acknowledgement of a load via ServerRpcs from each client, and will only spawn the prefab over the network once it has received an acknowledgement from every client, within `m_SynchronousSpawnTimeoutTimer` seconds.
  <br><br>

- Scene 04_Server Authoritative Spawn Dynamic Prefab Using Network Visibility
  - A dynamic prefab loading use-case where the server instructs all clients to load a single network prefab via a ClientRpc, will spawn said prefab as soon as it is loaded on the server, and will mark it as network-visible only to clients that have already loaded that same prefab. As soon as a client loads the prefab locally, it sends an acknowledgement ServerRpc, and the server will mark that spawned NetworkObject as network-visible to that client.
  <br><br>

- Scene 05_API Playground Showcasing All Post-Connection Use-Cases
  - This scene serves as an API playground to test how all of the use-cases in can work tandem.
<br><br><br>

## Known Limitations
- If you have NetworkConfig.ForceSamePrefabs enabled, you can only modify your prefab lists **before** starting
  networking, and the server and all connected clients must all have the same exact set of prefabs
  added via this method before connecting.

- Adding a prefab on the server **does not** automatically add it on the client - it's up to you
  to make sure the client and server are synchronized via whatever method makes sense for your game
  (RPCs, configs, deterministic loading, etc). This sample lists some of those possible methods with basic implementations you can adapt to your various use cases.

- If the server sends a Spawn message to a client that does not yet have the corresponding prefab loaded, the spawn message
  (and any other relevant messages) will be held for a configurable time before an error is logged (default 1 second, configured via
  NetworkConfig.SpawnTimeout). This is intented to enable the SDK to gracefully
  handle unexpected conditions that slow down asset loading (slow disks, slow network, etc). This timeout
  should not be relied upon and code shouldn't be written around it - your code should be written so that
  the asset is expected to be loaded before it's needed.
  
- It's currently impossible for clients to late join after a dynamic prefab has been spawned by the server - this is because the initial sync doesn't allow us any time to load prefabs that are aren't yet loaded on the client.
<br><br>


## Future Improvement Ideas
- Adding more advanced logic that would kick players that are consistently failing to load required Addressables.

- Compress Addressable GUID list before it is sent, thus reducing the amount of data being exchanged.

- Rather than exchanging Addressable GUIDS, the peers exchange a `short` index that would refer to Addressables
stored (in some sort of list) in a ScriptableObject, thus drastically reducing the amount of data being exchanged.
<br><br>


[![Documentation](https://img.shields.io/badge/Unity-bitesize--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-introduction)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)

