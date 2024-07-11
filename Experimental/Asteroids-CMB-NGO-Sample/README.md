# Getting Started
This project is built to demonstrate Netcode for GameObjects (NGO) running in distributed authority mode. 

## There are several distributed authority NGO mode demos:
- **Asteroids:** A more "full feature" usage example of distributed authority.
- **Parenting:** A parenting example in ditributed authority where everything is owner authoritative.
- **Ownership:** Demonstrates various ownership permissions along with how "showing" and "hiding" objects works.
- **Deferred Despawn:** A simpler sub-set from the Asteroids demo assets to demonstrate the deferred despawn feature.
- **Scene Loading:** Demonstrates how NGO's integrated scene management works in distributed authority mode and how session owners control scene events.
- **Stress Test:** A very simple spawning stress test.

## Running Demos
**Required Editor: Unity 6 (6000.0)**
After cloning the repository, add the project to your Unity Hub and open it like any other project. Make a stand alone build and run.

### Connect Mode Options:
When you start any of the demos/samples you will always be presented with a "Mode" button in the top left corner and a "Main Menu" button in the top right corner. If you click the "Main Menu" button it will take you back to the demo scene selection screen. If you click the "Mode" button then you will be presented with several options of what mode you want to test:

![image](https://media.github.cds.internal.unity3d.com/user/3057/files/87fd2ec4-bdb6-435f-ba72-b6300d367e5b)

- **DAHost:** Primarily used for development. This mode will provide you with the option to start a "DAHost" (a host that mocks the cloud state service) or a "DAClient" (a standard client that runs in distributed authority session mode). 
- **DAHostRelay:** Primarily used for development and network performance comparison. This acts just like the DAHost mode with the option to connect to a relay service. This provides a reasonable context when comparing network performance relative to when connected to a live service (the UGS backend cloud state service for distributed authority sessions).
- **ServiceLocal:** Primarily for development when testing a locally running service (contact @noel.stephens if you are interested in setting this up).
- **ServiceLive:** Connect to or start a cloud state service network session. This will provide you with a session name text field and a connect button. If you want to start a new session, enter in the name of the session and hit "connect". If you want to join an existing session perform the same steps (enter the session name to connect to and click "connect").

![image](https://media.github.cds.internal.unity3d.com/user/3057/files/29bccbd7-2c90-463f-80f7-1cf52e5c954f)

   
# Project Overview
The most complex full featured testing grounds/sandbox scene is the `Asteroids` scene asset located in the Assets\Scenes folder:

## Bootstrap scene
Most of the time the `Asteroids` scene is the best way to test all of the POC feature functionality. However, there are other scenes that are used for testing purposes (or you might want to create your own for a new feature or to simplify testing). The `BootStrap` scene's camera contains the `BootstrapHandler` component that defines what scenes are presented in the initial first screen GUI when running a stand alone build (and the `BootStrap` scene is still in the index 0 slot of the scenes in build list):

## Test Scenes and Scripts
The test scenes are located in the Assets\Scenes\Tests folder along with test specific scripts in the Assets\Scenes\Tests\Scripts. If you want to create a test scene to validate a specific new feature (or the like), then this would be the area to add it. It might be easier to move the `BootstrapScene` the 0 index of the scenes in build list and add your newly created test scene to that (for rapid access/development if you are using stand alone builds).

## Asteroids Scene
For the most part, the content is developed like a standard NGO enabled game. Due to some of the differences in distributed authority vs cient-server NGO operation modes, there are a few things to take into consideration/be aware of:

### In-Scene Placed Spawners
When you open the `Asteroids` scene, you will notice several "Spawner" managers that have the `InSceneObjectSpawner` script attached to them:

The `InSceneObjectSpawner` script is designed (for the POC core features) to handle the job of instantiating an in-scene placed NetworkObject when a session is first started. Once the session is started these objects will no longer attempt to create an instance of their targeted object to spawn. _When running NGO in distributed authority mode with CMB services enabled (`NetworkManager.CMBServiceConnection`), in-scene placed NetworkObjets are actually dynamically spawned to avoid complexities with scene synchronization. When running in DA-Host mode, scene management is enabled and in-scene placed NetworkObjects are spawned normally. To avoid issues until CMB services mode can handle scene synchronization, use the `InSceneObjectSpawner` to handle the spawning of in-scene placed NetworkObjects._

### Object Pools:
In order to obtain the most performant response time in spawning objects (especially with things like POC interest management), almost everything that can be spawned has an associated object pool. Each pool system contains an `ObjectPoolSystem` component that is derived from a `MonoBehaviour` and implements the `INetworkPrefabInstanceHandler` interface. Pool systems only require that an existing `NetworkManager` instance be available in order to function properly. `ObjectPoolSystem` also provides you with the ability to persist the pool between network sessions (i.e. hitting the `X` and reloading the `Asteroids` scene) in order to reduce development iteration time when using large pools that can take awhile to instantiate. Check the `DontDestroyOnSceneUnload` property to enable this on a per `ObjectPoolSystem` basis.

You can easily create additioanl pools by duplicating an existing prefab, renaming it, changing its assigned netowrk prefab to pool, and updating how many instances the pool should start with. The `ObjectPoolSystem` component will dynamically increase its pool size if the demand goes beyond the initial pool size settings.

### Ship Controls:
The ship controls are as follows:

**WASD or Arrow Keys:** Activates ship thrusters. Momentum is gained over time.

**Space Bar:** Fires lasers.

**{G}rab-Tractor:** The `(G)` key activates the tractor beam that lets a ship grap a mine. This feature was added to demonstrate the client-driven ownership transfer. If a mine is not owned by the client grabbing it, then the client will claim ownership over the mine as it pulls it in to be locked into place.

### Display toggles

**{O}wnership Overlay:** The `(O)` key toggles ownership overlays on all objects. The color corresponds to the owning client for each object.

**Runtime {N}etwork Stats Monitor:** The `(N)` or `(TAB)` key toggles the RNSM tool.

**{M}ap Interest HUD:** The `(M)` key activates an interest heads up display that will pop up in the bottom right corner of the render view. 
Hitting `(M)` for the first time enables the transparent background view:

Hitting `(M)` for the second time enables the semi-transparent background view:

Hitting `(M)` a third time disables the view.

**{ [ } & { ] } Zoom Keys:** The `([)` key zooms in and the `(])` key zooms out when the overlay interest map is enabled.

**{`~`} Runtime Console Log:** Hitting the back quote/`(~)` key  will toggle the runtime log output for the console log.
