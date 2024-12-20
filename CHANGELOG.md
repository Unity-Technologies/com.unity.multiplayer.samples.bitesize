# Changelog

## [1.10.0] 2024-12-23

### Distributed Authority Social Hub

#### Added
- Textchat feature has been added. Chat is using Vivox, UI was integrated using UI Toolkit. (#258)
- Mobile and gamepad support, with associated UI for mobile controls using UIToolkit has been integrated to the sample (#256)
- Positional voicechat feature has been added. Chat is using Vivox, UI was integrated using UI Toolkit. See limitations in PR.(#261)
- Improve usability on Mobile, general UI improvements. (#266)
- In-editor tutorials have been added to the sample (#268) They walk you through:
  - Associating your project with a Unity Cloud Id
  - Creating a Virtual Player through Multiplayer Play Mode
  - Enabling Network Scene Visualization through Multiplayer Tools
  - A typical session owner promotion while connected to a session

#### Changed
- Player spawning has been deferred to be performed manually by an in-scene placed NetworkObject (#257) Player spawn has been moved to coincide with NetworkManager's OnNetworkSessionSynchronized callback, ensuring the player is spawned after synchronizing the game scene's NetworkObjects.
- Adjusted lighting and shadow settings and custom shaders to improve the overall look of the sample, also water transparency issue has been fixed (#265)

### 2D Space Shooter

#### Added
- Added a welcome dialog and the standard Table of Contents for Bitesize Samples to 2D Space Shooter Sample (#264)

#### Changed
- Update Project to Unity 6 (#263)
- Replaced 3rd party ParrelSync package with Unity's Multiplayer Play Mode (#263)
- Replaced old input system with new input system (#267)

### Client Driven

#### Added
- Added a welcome dialog and the standard Table of Contents for Bitesize Samples to the ClientDriven sample (#262)

#### Changed
- Updated project to Unity 6 (#259)
  - Replaced 3rd party ParrelSync package with Unity's Multiplayer Play Mode

## [1.9.0] 2024-10-31

### Distributed Authority Social Hub

#### Changed
- This sample is no longer "Experimental", and it has been moved into the "Basic" folder

## [1.8.0] 2024-10-23

### Multiplayer Use Cases

#### Added
- The last page of the "Data/Event Synchronization" tutorial redirects to the "Proximity Checks" tutorial
- Imported TextMeshPro essentials so you don't have to do it

#### Changed
- Updated project to Unity 6
- All inputs are handled through the new Unity Input System
- In-Game UI is now responsive and adapts to screen size
- Replaced 3rd party ParrelSync package with Unity's Multiplayer Play Mode
- Disabled compatibility mode for RenderGraph, as it's going to be deprecated and throws warnings
- Removed runtime network stats monitor from the scene to improve readability
- Removed deprecated visual studio code package
- Updated packages: Rider to 3.0.31, Input System to 1.11.1, Tutorial Framework to 4.0.2, Multiplayer Tools to 2.2.1, Netcode For GameObjects to 2.0.0, UGUI to 2.0.0, Test Framework to 1.4.5, Universal Render Pipeline to 17.0.3

#### Fixed
- Fixed "Build Profile" window not being clickable during the last page of each tutorial

## [1.7.0] 2024-08-31

### Bitesize Samples Repository

#### Added
- Added a "Deprecated" folder for samples that are no longer supported

### Invaders

#### Changed
- This sample is now "Deprecated"

### Multiplayer Use Cases

#### Changed
- This sample is no longer "Experimental", and it has been moved into the "Basic" folder
- In-game UI now uses UIToolkit instead of UGUI
- Updated README to provide a clear path to the onboarding resources in case a user closed the Tutorial window 

### Dedicated Game Server

#### Changed
- Upgraded project to Unity 6000.0.3f1
- Updated dedicated server packages to 1.1.0
- Updated Netcode For GameObjects version to 1.8.1, and replaced RPCs using the new workflow provided

## [1.6.0] 2024-05-30

### Bitesize Samples Repository

#### Cleanup
- Formatted .cs files inside the Bitesize Samples repository to adhere to coding standards (#156) Internal testing job definition files were added in order for internal processes to execute.

### 2D Space Shooter

#### Added
- Added local pooling for explosionParticles to optimize performance and showcase built-in pooling component (#167)

#### Changed
- Upgraded to IDE Rider v3.0.28 (#166)
- Upgraded to Unity 2022.3.27f1 (#170)
  - upgraded to com.unity.burst v1.8.13
  - added com.unity.modules.jsonserialize v1.0.0
  - upgraded to com.unity.render-pipelines.core v14.0.11
  - upgraded to com.unity.render-pipelines.universal-config v14.0.10
  - upgraded to com.unity.shadergraph v14.0.11
  - upgraded to com.unity.services.authentication v2.7.4
  - upgraded to com.unity.services.qos v1.3.0
  - upgraded to com.unity.transport v1.4.1
  - upgraded to com.unity.services.core v1.12.5
- Upgraded to Netcode for GameObjects v1.8.1 (#174)
  - Upgraded to the newer API for Rpcs, Universal Rpcs
  - Upgraded to newer API for Connection Events, OnConnectionEvent

#### Fixed
- Reset values and buffs after respawn of ship (#167)

### Client Driven

#### Changed
- Upgraded to Netcode for GameObjects v1.8.1 (#164)
  - Upgraded to the newer API for Rpcs, Universal Rpcs
  - The place of execution for a client's position was moved to ClientNetworkTransform child class, ClientDrivenNetworkTransform. This ensures no race condition issues on a client's first position sync. Server code now modifies a NetworkVariable that client-owned instances of ClientDrivenNetworkTransform use on OnNetworkSpawn to initially move a player
  - Upgraded to use NetworkObject.InstantiateAndSpawn() API where appropriate (#173)
- Upgraded to IDE Rider v3.0.28 (#166)
- Upgraded to Unity 2022.3.27f1 (#175)
  - com.unity.render-pipelines.core upgraded to v14.0.11
  - com.unity.services.authentication upgraded to v2.7.4
  - com.unity.services.core upgraded to v1.12.5
  - com.unity.services.qos upgraded to v1.3.0
  - com.unity.transport upgraded to v1.4.1

#### Fixed
- Added Spawner with event executed on Server Start to fix inconsistent ghost ingredients issue (#157)

### Dynamic Addressables Network Prefabs

#### Changed
- Upgraded to IDE Rider v3.0.28 (#166)
- Upgraded to Unity 2022.3.27f1 (#176)
  - com.unity.transport upgraded to v1.4.1

#### Fixed
- Releasing an Addressables handle on OnDestroy inside Preloading scene to prevent releasing loaded dynamic prefab from memory (#179)

### Invaders

#### Changed
- Upgraded to IDE Rider v3.0.28 (#166)
- Upgraded to Unity 2022.3.27f1 (#169)
- Upgraded to Netcode for GameObjects v1.8.1 (#172)
  - Upgraded to the newer API for Rpcs, Universal Rpcs
  - Upgraded to the newer API for NetworkObject spawning to use NetworkObject.InstantiateAndSpawn
  - Upgraded usage of NetworkManager.OnClientConnectedCallback to the new NetworkManager.OnConnectionEvent 

#### Fixed
- Optimized NetworkTransform on all networked prefabs so the Clients objects movements are closer to the Host ones (#168)

## [1.5.0] 2023-12-15

### Bitesize Samples Repository

#### Cleanup
- Removed the usage of System.Linq across the repository (#146)

### 2D Space Shooter

#### Changed
- Upgraded to Netcode for GameObjects v1.7.1 (#147)
- Upgraded sample to 2022.3.14f1 LTS (#147)
- Upgraded Samples Utilities package to v1.8.0 (#151)

#### Fixed
- Corrected the variables used for initialization of Health and Energy (#150)
- Converted unnecessary ship thrust NetworkVariable to a float (#149)
- Fixed non-host clients not hearing the Fire SFX (#148)

### Client Driven

#### Changed
- Upgraded to Netcode for GameObjects v1.7.1 (#147)
- Upgraded sample to 2022.3.14f1 LTS (#147)
- Upgraded Samples Utilities package to v1.8.0 (#151)

### Dynamic Addressables Network Prefabs

#### Changed
- Upgraded to Netcode for GameObjects v1.7.1 (#147)
- Upgraded sample to 2022.3.14f1 LTS (#147)

### Invaders

#### Changed
- Upgraded to Netcode for GameObjects v1.7.1 (#147)
- Upgraded sample to 2022.3.14f1 LTS (#147)
- Upgraded Samples Utilities package to v1.8.0 (#151)

## [1.4.0] - 2023-09-25

### 2D Space Shooter

#### Changed
- Upgraded to Netcode for GameObjects v1.6.0 (#134)
- Upgraded sample to 2022.3.9f1 LTS (#134)

#### Fixed
- Fixed warnings when spawning new bullets or asteroids by instantiating a new NetworkVariable pre-spawn (#134)

### Client Driven

#### Changed
- Upgraded to Netcode for GameObjects v1.6.0 (#134)
- Upgraded sample to 2022.3.9f1 LTS (#134)

#### Fixed
- Added a script to handle NetworkObject parent changes on Ingredients to address a bug where Ingredients would not get stuck on client disconnect events (#136)

### Dynamic Addressables Network Prefabs

#### Changed
- Upgraded to Netcode for GameObjects v1.6.0 (#134)
- Upgraded sample to 2022.3.9f1 LTS (#134)

#### Fixed
- Fixed loaded status displayed on UI for synchronous prefab spawns inside 05_API Playground Showcasing All Post-Connection Use-Cases scene (#132)

### Invaders

#### Changed
- Upgraded to Netcode for GameObjects v1.6.0 (#134)
- Upgraded sample to 2022.3.9f1 LTS (#134)

## [1.3.0] - 2023-07-07

### Dynamic Addressables Network Prefabs

#### Changed
- Upgrade to Netcode for GameObjects 1.4.0 (#118)
- Upgraded sample to 2021.3.24f1 LTS (#119)
- Upgraded sample to 2022.3.0f1 LTS (#124)

#### Fixed
- Resolved visual bug where the load status of dynamic prefabs on the host was not correct (#111)

### 2D Space Shooter

#### Changed
- Upgrade to Netcode for GameObjects 1.4.0 (#118)
- Upgraded sample to 2021.3.24f1 LTS (#119)
- Upgraded sample to 2022.3.0f1 LTS (#124)
- Upgraded Samples Utilities version to v2.2.0 (#129)

### Client Driven

#### Changed
- Upgrade to Netcode for GameObjects 1.4.0 (#118)
- Upgraded sample to 2021.3.24f1 LTS (#119)
- Upgraded sample to 2022.3.0f1 LTS (#124)
- Upgraded Samples Utilities version to v2.2.0 (#129)

### Invaders

#### Changed
- Upgrade to Netcode for GameObjects 1.4.0 (#118)
- Upgraded sample to 2021.3.24f1 LTS (#119)
- Upgraded sample to 2022.3.0f1 LTS (#124)
- Upgraded Samples Utilities version to v2.2.0 (#129)
  
#### Fixed
- IP address input field text value is now passed into UTP's ConnectionData, allowing for remote IP address hosting (#112)
- Enemy and Player bullet explosion FX are now replicated on clients via ClientRpcs (#113)
- Fixed an error produced when a client disconnected once the host disconnected after the game was complete (#124)

### Bitesize Samples Repository

#### Fixed
- Removed individual gitignore files for individual projects and added parrelsync clones to root gitignore file (#117)
- Fixed link to old tutorial and clarified supported platforms (#120)

## [1.2.1] - 2023-02-17

### Dynamic Addressables Network Prefabs

#### Changed
- Readme updated with link to the Dynamic Addressables Network Prefabs sample documentation, and broken links fixed (#106)

### 2D Space Shooter

#### Changed
- Readme updated with link to the 2D Space Shooter sample documentation, and broken links fixed (#106)

### Client Driven

#### Changed
- Readme updated with link to the Client Driven sample documentation, and broken links fixed (#106)

### Invaders

#### Changed
- Readme updated with link to the Invaders samples documentation, and broken links fixed (#106)

### Bitesize Samples Repository

#### Fixed
- Readme formatting adjustments and broken link fixes (#106) (#108)

## [1.2.0] - 2023-02-16

### Dynamic Addressables Network Prefabs Sample

Added the Dynamic Addressables Network Prefabs Sample. This sample showcases the available use-cases for the dynamic prefab system, which allows us to add new spawnable prefabs at runtime. This would be useful for games trying to use both [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@0.8/manual/index.html) and Netcode for GameObjects (#63) (#94) (#96) (#97) (#98) (#99) (#100) (#101) (#103)

### 2DSpaceShooter

#### Fixed
- Resolved issue where the colour of powerups was not displayed correctly (#91)
- Removing warning when spawning powerups (#90). Fixed the order in which powerups were spawned and when their NetworkVariable value was initialized. Now they are spawned beforehand.
- Fixing bullet explosion desync (#89). Bullet explosion vfx were happening too early on clients because of NetworkTransform's interpolation. Bullets are now no longer synchronised by NetworkTransforms and instead only have their velocity set through client rpcs when they are spawned. Since they are no longer interpolated, they are not lagging behind the server and are at the correct position when they receive the despawn message from the server.
- Fix: Broken Reference for Underline Character in Font Asset (#87)

#### Changed
- Upgraded sample to 2021.3.18f1 LTS (#91)
- Readme was updated (#96)
- Added readme banner image (#101)


### Client Driven

#### Changed
- Upgraded sample to 2021.3.18f1 LTS (#91)
- Readme was updated (#96)
- Added readme banner image (#101)

### Invaders

#### Changed
- Upgraded sample to 2021.3.18f1 LTS (#91)
- Readme was updated (#96)
- Added readme banner image (#101)

### Bitesize Samples Repository
#### Changed
- Readme was updated (#96)


## [1.1.0] - 2022-12-13

### Client Driven

#### Changed
- Changed connection UI to use UI buttons and events instead of OnGUI [MTT-4201] (#60)
- ThirdPersonController starter assets added (#62)
- Uniformize gitignore files (#65)
- Art Pass (#68)
- Anti Aliasing (#72)
- Third Person Character Controller integration with custom IP UI (#66) The sample has been refactored to feature a PlayerPrefab inside NetworkManager that is a networked variant of a prefab from [Unity's Starter Assets](https://assetstore.unity.com/packages/essentials/starter-assets-third-person-character-controller-196526). Other improvements include:
  - Input polling via the new Input System
  - Connection UI utilizes UI Toolkit
  - Updated to Unity 2021.3.15f1 LTS
- Change URP grading mode to HDR (#73)
- Player Colors (#75)
- UI Art (#69)
- Readme added (#76)
- Upgrade to Netcode for GameObjects v1.2.0 & cleaned up in-scene NetworkVariables (#78)
- Ingredient spawn position offset (#81)
- In-game UI backgrounds (#82)
- Initial position sync fix on owning clients (#85)

### 2DSpaceShooter

#### Changed
- UI Pass using UI Toolkit (#55)
- Uniformize gitignore files (#65)
- 2DSpaceShooter & Invaders 2021.3.15f1 LTS update & Readmes (#77)
- Updating Invaders and 2DSpaceShooter to Netcode for GameObjects v1.2.0 (#84)

### Invaders

#### Fixed
- Fixing NotServerException [MTT-4029] (#59)
- Despawning enemies instead of destroying them (#74)

#### Changed
- Updated to Unity 2021.3.15f1 LTS
- 2DSpaceShooter & Invaders 2021.3.15f1 LTS update & Readmes (#77)
- Updating Invaders and 2DSpaceShooter to Netcode for GameObjects v1.2.0 (#84)

## [1.0.0] - 2021-10-20

### Client Driven

A new sample was added named client driven. It focus on client driven movements, networked physics, spawning vs statically placed objects, object reparenting

### Invaders

#### Changed
- Updated to Netcode for GameObjects 1.0.0.
- Player now use ClientNetworkTransform for client driven movement
- Network Manager now uses Unity Transport instead of UNet
- SceneTransitionHandler : now uses the new Scene Manager and Scene Loading events
- InvadersGame : replicated time remaining now uses a RPC instead of a one time synchonized NetworkVariable

#### Fixed
- Network behaviour in OnDestroy was moved to OnNetworkDespawn

### 2DSpaceShooter

#### Changed
- Updated to Netcode for GameObjects 1.0.0.
- Player name is no longer static but based on the client id.

#### Fixed
- Fixed a bug where explosions from bullet impacts where only visible on the host.
- Fixed a bug where setting the hosts port in the UI wouldn't change the port on which the server was hosted.

## [0.2.0] - 2021-07-21

### Invaders

#### New Changes

- Game - rename all our alien prefabs to have a more generic name, the same principle was applied to our codebase, renamed variables/fields/classes to something more generic
- Game: Fix a crash in shipping build
- Enemies: Rename our main enemy class to EnemyAgent + minor clean-ups + implement a grace shoot timer period
- InvadersGame: Some big refactories here, the UpdateEnemies function not outputs a set of flags (bitmask) rather than having separated booleans to keep track off
- InvadersGame: Fix an edge case of the game loop where if the enemies would reach the bottom they would never respawn, now when they do reach that bottom boundary it will be game over
- InvadersGame: Introduce additional game over reasons
- LobbyControl: Introduce a minimum player count variable that could be tweaked in the inspector so that the users can start playing in the editor with just the host in the lobby
- PlayerControl: Unified the NotifyGameOver function with the InvadersGame one + added different texts for all the possible game over reasons to be displayed
- PlayerControl: Fix a minor issue where the Player graphics are not hidden on all connected instances upon "death".

#### Known Issues

- Upon import a MissingReferenceException is triggered from within MLAPI: **"MissingReferenceException: The object of type ‘GameObject' has been destroyed but you are still trying to access it.
  Your script should either check if it is null or you should not destroy the object."** in:
  - UnityEngine.GameObject.GetComponent[T] () (at /Users/bokken/buildslave/unity/build/Runtime/Export/Scripting/GameObject.bindings.cs:28)

### 2DSpaceShooter

#### New Changes
- Cleaned up project structure, removed duplicate art assets and moved remaining asset into sub folders.
- Removed empty MonoBehaviours which were used as tags and using the Unity tag system instead.
- Removed the dependency to the community contributions repository and copied `NetworkManagerHud` and `NetworkObjectPool` into the sample.
- Small QoL improvements to scripting. Using `TryGetComponent` and `CompareTag`.
- Fix audio issues by moving the audio listener onto the player ship object.

#### Known Issues

- Missing Reference Exception thrown when leaving playmode, can be ignored:<br>
*MissingReferenceException: The object of type 'NetworkObjectPool' has been destroyed but you are still trying to access it.
Your script should either check if it is null or you should not destroy the object.
MLAPI.Extensions.NetworkObjectPool.ReturnNetworkObject*

## [0.1.0] - 2021-04-07

Initial release of MLAPI Bitesize Samples repository. Samples support the following versions:

| Unity Version | Unity MLAPI Version |
| -- | -- |
| 2020.3 | 0.1.0 |

### New features

- Added Invaders sample.
- Added 2D Space Shooter sample.

### License

The Bitesize Samples repository and code projects are licensed under the Unity Companion License for Unity-dependent projects (see https://unity3d.com/legal/licenses/unity_companion_license). See [LICENSE](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/main/LICENSE.md) for full details.
