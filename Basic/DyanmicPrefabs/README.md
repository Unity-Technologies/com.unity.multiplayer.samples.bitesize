# Dynamic Prefabs Sample
The Dynamic Prefabs Sample showcases the available use-cases for the dynamic prefab system, which allows us to add new spawnable prefabs at runtime. Each scene in the project showcases a different, isolated feature of the API, allowing for easy extraction into other projects. This sample also uses in-game UI (created using UI Toolkit) to interface with the dynamic prefabs system with configurable options like artificial latency and network spawn timeout for easy testing.
<br><br>

## Sample features
The dynamic prefabs system allows the developer to add a new prefab to the network prefab list at runtime.
This sample uses Addressables to load the dynamic prefab, however any GameObject with a NetworkObject component can be used, regardless of its source.
<br><br>

There are several limitations to this API:
- If you have NetworkConfig.ForceSamePrefabs enabled, you can only modify your prefab lists **before** starting
  networking, and the server and all connected clients must all have the same exact set of prefabs
  added via this method before connecting.

- Adding a prefab on the server **does not** automatically add it on the client - it's up to you
  to make sure the client and server are synchronized via whatever method makes sense for your game
  (RPCs, configs, deterministic loading, etc).

- If the server sends a Spawn message to a client that does not yet have the corresponding prefab loaded, the spawn message
  (and any other relevant messages) will be held for a configurable time before an error is logged (default 1 second, configured via
  NetworkConfig.SpawnTimeout). This is intented to enable the SDK to gracefully
  handle unexpected conditions that slow down asset loading (slow disks, slow network, etc). This timeout
  should not be relied upon and code shouldn't be written around it - your code should be written so that
  the asset is expected to be loaded before it's needed.
  
- It's currently impossible for clients to late join after a dynamic prefab has been spawned by the server - this is because the initial sync doesn't allow us any time to load prefabs that are aren't yet loaded on the client.
<br><br>

Therefore, based on these limitations, the following use-cases are covered in this sample:
- PreloadingSample.cs - this is the simplest case of a dynamic prefab - we just add it to the list of prefabs before we connect on all the peers.

- AppController.cs and DynamicPrefabManager.cs - this is a more complex case where the server spawns things that the client haven't yet loaded. There are two loading strategies - one is "synchronous" spawning, which ensures that all clients have acknowledged that they have loaded that prefab before the server actually spawns the NetworkObject. The other one is spawning the object immediately and using visibility system to hide that object from clients that haven't loaded the prefab yet (when they do acknowledge the prefab loading - the server will show the relevant hidden objects to the client). This sample also shows how we could handle the issue with initial synchronization breaking for late joiners by using some custom logic in connection approval delegate.
<br><br>

[![Documentation](https://img.shields.io/badge/Unity-bitesize--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-introduction)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)

