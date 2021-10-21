# Change log

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

- Upon import a MissingReferenceException is triggered from within MLAPI: **"MissingReferenceException: The object of type ‘GameObject’ has been destroyed but you are still trying to access it.
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
