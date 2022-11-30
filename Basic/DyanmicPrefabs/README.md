# Dynamic Prefabs

This sample showcases the available use-cases for the dynamic prefab system, which allows us to add new spawnable prefabs in runtime.

## Sample feature

Dynamic prefabs feature allows the developer to add a new prefab to the network prefab list in runtime.
For simplicity, this sample uses Addressables to load the dynamic prefab, however any GameObject with a NetworkObject component can be used, regardless of it's source.

There are several limitations to this API:
- If you have NetworkConfig.ForceSamePrefabs enabled, you can only modify your prefab lists before starting
  networking, and the server and all connected clients must all have the same exact set of prefabs
  added via this method before connecting
- Adding a prefab on the server does not automatically add it on the client - it's up to you
  to make sure the client and server are synchronized via whatever method makes sense for your game
  (RPCs, configs, deterministic loading, etc)
- If the server sends a Spawn message to a client that has not yet added a prefab for, the spawn message
  and any other relevant messages will be held for a configurable time (default 1 second, configured via
  NetworkConfig.SpawnTimeout) before an error is logged. This is intented to enable the SDK to gracefully
  handle unexpected conditions (slow disks, slow network, etc) that slow down asset loading. This timeout
  should not be relied on and code shouldn't be written around it - your code should be written so that
  the asset is expected to be loaded before it's needed.
- Currently it's impossible to latejoin for clients after a dynamic prefab has been spawned by the server. The reason for this is that the initial sync doesn't allow us any time to load prefabs that are yet absent on the client.

Based on these limitations the following use cases are covered in the sample:
 - PreloadingSample.cs - this is the simplest case of a dynamic prefab - we just add it to the list of prefabs before we connect on all the peers
 - AppController.cs and DynamicPrefabManager.cs - this is a more complex case where the server spawns things that the client haven't yet loaded. There are two loading strategies - one is "synchronous" spawning, which ensures that all clients have acknowledged that they have loaded that prefab before the server actually spawns the NetworkObject. The other one is spawning the object immediately and using visibility system to hide that object from clients that haven't loaded the prefab yet (when they do acknowledge the prefab loading - the server will show the relevant hidden objects to the client). This sample also shows how we could handle the issue with initial synchronization breaking for late joiners by using some custom logic in connection approval delegate.
