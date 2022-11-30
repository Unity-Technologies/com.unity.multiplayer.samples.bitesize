# Dynamic Prefabs

This sample showcases the available use-cases for the dynamic prefab system, which allows us to add new spawnable prefabs in runtime.

## Sample feature

Dynamic prefabs feature allows the developer to add a new prefab to the network prefab list in runtime.
For simplicity, this sample uses Addressables to load the dynamic prefab, however any GameObject with a NetworkObject component can be used, regardless of it's source (think User Generated Content (UGC)).

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
 - SparseLoadingNoLatejoinSample.cs - this is a more complex case where the server spawns things that the client haven't yet loaded, but with the ability to latejoin or reconnect disabled after the "game session" has started.
