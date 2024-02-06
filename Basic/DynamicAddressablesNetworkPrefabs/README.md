![Banner](Resources/dynamicprefab_banner.png)
<br><br>

# Dynamic Addressables Network Prefabs Sample

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2022.3%20LTS-57b9d3.svg?logo=unity&color=2196F3)](https://unity.com/releases/editor/whats-new/2022.3.0)
[![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-1.7.1-57b9d3.svg?logo=unity&color=2196F3)](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/tag/ngo%2F1.7.1)
[![LatestRelease](https://img.shields.io/badge/Latest%20%20Github%20Release:-v1.5.0-57b9d3.svg?logo=github&color=brightgreen)](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/releases/tag/v1.5.0)
<br><br>

The Dynamic Prefabs Sample showcases the available use-cases for the dynamic prefab system, which allows us to add new spawnable prefabs at runtime. This sample uses Addressables to load the dynamic prefab, however any GameObject with a NetworkObject component can be used, regardless of its source. This sample also uses in-game UI (created using UI Toolkit) to interface with the dynamic prefabs system with configurable options like artificial latency and network spawn timeout for easy testing.

## Note on Addressables
Addressables are great for managing your memory, download times, adding DLCs.

Memory management with Addressables is great on platforms with restricted memory. Think mobile or webgl. In general on mobile, you won't load prefabs in memory directly on scene load, you'll want to load them only when you actually need them. You could load them from disk or from a CDN, that doesn't matter.
Another use case is for reducing the initial app download time. You could ship a slim app with only the essentials asset wise. And when your player is in menus or playing the tutorial, start downloading the remainder of the app through addressables.

There's a lot of optimizations potentials around asset bundles and addressables, we highly encourage you to look through them, they can be used on pretty much any platforms. 
Some cool ressources:
- Recent blog post on addressables: https://blog.unity.com/engine-platform/addressables-planning-and-best-practices
- An older one: https://blog.unity.com/technology/tales-from-the-optimization-trenches-saving-memory-with-addressables
<br><br>

# Sample Overview

In this sample, learn more about:
- The dynamic prefabs system
- Loading prefabs with addressables 
<br><br>

---
### 💡 Documentation
Check out the [Dynamic Addressables Network Prefabs sample documentation](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-dynamicPrefabs) for a more in-depth technical breakdown of our engineering decisions and why the sample works the way it does.

---
<br>

## Exploring the Sample

Each scene in the project showcases a different, isolated feature of the API, allowing for easy extraction into other projects. We suggest exploring them in order to get a good understanding of the flow of dynamically loading and spawning network prefabs. The use-cases available in this sample are based around the current known limitations.

**Important:** This sample assumes that Addressables will never fail to load, therefore the server does not account for the case in which a client fails to load an Addressable. In a realistic implementation, you would want to either have the offending client try to load the Addressable again, or disconnect it from the session. 
<br><br>

### Each Scene:

- Scene 00_Preloading Dynamic Prefabs
  - This is the simplest case of a dynamic prefab - we instruct all game instances to load a network prefab (it can be just one, it could also be a set of network prefabs) and inject them to NetworkManager's NetworkPrefabs list before starting the server.
  
  - This is the less intrusive option for your development, as you don't have any additional spawning and addressable management to do later in your game. 
  <br><br>

- Scene 01_Connection Approval Required For Late Joining
  - An optional use-case scenario that walks through what a server would need to validate from a connecting client when dynamically loading network prefabs. Other use-cases don't allow for reconciliation after the server has loaded a prefab dynamically and before a client joined, whereas this one enables this functionality. 
  
  - This is to support late join and should be used in combination with the other techniques described below.
  <br><br>

- Scene 02_Server Authoritative Preload All Prefabs Asynchronously
  - A simple use-case where the server notifies all clients to preload a collection of network prefabs. The server will not invoke a spawn directly after the addressable loading in this use-case, and will incrementally load each dynamic prefab, one prefab at a time.
  
  - This acts as a "warning" notification to clients that they'll soon need this prefab. This allows being less intrusive in the spawning process later as we can assume all clients have loaded the prefab already.
  
  - This is different from option 0, as here this is done when clients are connected and already in game. This allows for more flexibility around your gameplay and could load different prefabs depending on where your players are at in the game for example.
  <br><br>

- Scene 03_Server Authoritative Try Spawn Synchronously
  - This is the first technique that loads and spawns sequentially. This is a dynamic prefab loading use-case where the server instructs all clients to load a single network prefab, and will only invoke a spawn once all clients have successfully completed their respective loads of said prefab. The server will initially send a ClientRpc to all clients, begin loading the prefab on the server, will await acknowledgement of a load via ServerRpcs from each client, and will only Spawn() the instantiated prefab over the network once it has received an acknowledgement from every client, within `m_SynchronousSpawnTimeoutTimer` seconds.

  - This and the next technique allow for the most flexibility compared to the previous ones.

  - This technique makes sure all clients have loaded a prefab before spawning and starting gameplay on that prefab. This is useful for game changing objects, like a big boss that could kill everyone. In that case you want to make sure all clients have loaded that prefab before spawning that object. 
  <br><br>

- Scene 04_Server Authoritative Spawn Dynamic Prefab Using Network Visibility
  - A dynamic prefab loading use-case where the server instructs all clients to load a single network prefab via a ClientRpc, will spawn said prefab as soon as it is loaded on the server, and will mark it as network-visible only to clients that have already loaded that same prefab. As soon as a client loads the prefab locally, it sends an acknowledgement ServerRpc, and the server will mark that spawned NetworkObject as network-visible to that client.

  - This makes sure that no client can block the spawn of certain objects by making it visible as soon as specific clients have loaded that prefab. This is great for network reactivity, but might create inconsistent world views for each client depending on whether you've loaded that prefab or not. Using that technique for boss loading could create a situation where your player sees its health decreasing rapidly for no visible reason because the boss is loaded server side and hitting you, but you haven't loaded it yet and it's invisible on your client.
  <br><br>

- Scene 05_API Playground Showcasing All Post-Connection Use-Cases
  This scene serves as an API playground to test how all of the use-cases can work in tandem.
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
<br><br>

## Index of Resources

### Dynamic Prefabs System

- Preloading Dynamic Prefabs - [Assets/Scripts/00_Preloading/Preloading.cs](Assets/Scripts/00_Preloading/Preloading.cs)
- Connection Approval supporting late join - [Assets/Scripts/01_Connection Approval/ConnectionApproval.cs](Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs)
- Server Authoritative Preloading All Prefabs Asynchronously - [Assets/Scripts/02_Server Authoritative Load All Async/ServerAuthoritativeLoadAllAsync.cs](Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs)
- Server Authoritative Try Spawning Synchronously - [Assets/Scripts/03_Server Authoritative Synchronous Spawning/ServerAuthoritativeSynchronousSpawning.cs](Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs)
- Server Authoritative Spawn using Network-Visibility - [Assets/Scripts/04_Server Authoritative Network-Visibility Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs](Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs)

### UI
- In Game UI [Assets/Scripts/UI/InGameUI.cs](Assets/Scripts/UI/InGameUI.cs)
- IP Menu UI - [Assets/Scripts/UI/IPMenuUI.cs](Assets/Scripts/UI/IPMenuUI.cs)

### Addressables
- Package version - [Packages/manifest.json](Packages/manifest.json)
- Docs - https://docs.unity3d.com/Packages/com.unity.addressables@1.19/manual/index.html
<br><br>


## Future Improvement Ideas
This section describes some next steps a game developer could do to extend the sample code available here. This logic is left to the reader, this sample focuses on dynamic prefab management.
<br><br>
- Adding more advanced logic that would kick players that are consistently failing to load required Addressables.

- Compress Addressable GUID list before it is sent, thus reducing the amount of data being exchanged.

- Rather than exchanging Addressable GUIDS (underlying type `FixedString128Bytes`), the server and clients could instead exchange just a `short` index that would refer to an Addressable stored in some sort of list (potentially in a `ScriptableObject`). This would drastically reduce the amount of data being exchanged over the network. 

- This sample does not cover the use-case of using Addressables to load in custom User Generated Content (UGC) and replicating that to other clients.
<br><br>

---
### 💡 Bitesize Readme
Check out our main [Bitesize Samples GitHub Readme](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize#readme) for more documentation, resources, releases, contribution guidelines, and our feedback form.

---
<br>


[![Documentation](https://img.shields.io/badge/Unity-bitesize--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-introduction)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)

