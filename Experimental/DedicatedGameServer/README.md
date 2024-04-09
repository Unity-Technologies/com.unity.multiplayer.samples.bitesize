<br><br>

# Dedicated Game Server

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2023.3.017a%20-57b9d3.svg?logo=unity&color=2196F3)](https://unity.com/releases/editor/alpha/2023.3.0a17)
[![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-1.8.0-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/about)
<br><br>

The Dedicated Game Server sample project uses Netcode For GameObjects (NGO). It also demonstrates the structure of a project that uses the dedicated server and the tools that you can use to configure it.
<br><br>

# Sample Overview

The dedicated server sample scene contains a small maze with interactive doors and patrolling AI characters. Player characters can move around and open or close the doors using the switches nearby. You can use this sample scene to learn how to do the following:

- Integrate features from the [Dedicated Server](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/index.html) and [Multiplayer Play Mode](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@1.0/manual/index.html) packages with Netcode for GameObjects to with the dedicated server architecture.
* [Set default comand line arguments](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/cli-arguments.html)
* Use [multiplayer roles](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/multiplayer-roles.html).
* Use the dedicated server with [multiplayer play mode](https://docs-multiplayer.unity3d.com/mppm/current/about/).
* Strip GameObjects from the clients and server.
- Integrate a project that uses the dedicated server with the [Game Server Hosting](https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/welcome) and [Matchmaker](https://docs.unity.com/ugs/en-us/manual/matchmaker/manual/matchmaker-overview) services.
<br><br>

## Exploring the Sample

The dedicated server sample has a different workflows when you select the Client or Server multiplayer role. Both servers and clients start at the StartupScene, where Unity initializes the relevant systems. 

### The server multiplayer role
Unity moves the server to the game scene, where it waits for clients to connect. 

### The client multiplayer role
Unity moves clients to the Metagame scene. From here you can use the main menu to connect to a server. Select the **Find Match** to join a matchmaking queue to connect to a server. Select **Join with Direct IP** to connect to a server directly when you know its IP address. In both cases Unity moves clients to the game scene when the client connects.

#### Autoconnect mode
Use the autoconnect mode to automatically connect as a client to a local server without moving to the Metagame scene. You can use this mode to speed up testing.

To enable autoconnect mode on the main editor, do the following:

1. Set the editor [multiplayer role](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/multiplayer-roles.html) to **Client**.
2. Open the StartupScene
3. In the Hierarchy window, select the **ApplicationEntryPoint** GameObject.
4. In the **ApplicationEntryPoint** component, select the **Autoconnect if client** checkbox .

---
<br>

## Set up the dedicated game server sample

### Register the project with Unity Gaming Services (UGS)

This sample uses the following services from Unity Game Services (UGS) to help connect between players: 

- Game Server Hosting
- Matchmaker
 
To use these services inside your project, [create an organization](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-) inside the Unity Dashboard. 

#### Set up the Game Server Hosting service

To set up the Game Server Hosting (Multiplay) service, do the following: 
1. [Test the dedicated server sample in a build](#test-dedicated-server-sample). 
2. Generate the build that Unity uploads to the Game Server Hosting service. To learn how to do this, refer to [Create a build](https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/guides/create-a-build).

#### Set up the Matchmaker service

Use the Matchmaker service to connect clients to the servers hosted by Game Server Hosting and control when a server starts. To use this service, [install and set up Matchmaker](https://docs.unity.com/ugs/en-us/manual/matchmaker/manual/get-started).

 **Note**: Name your Queue "Queue01" for the sample to use it automatically.

---
<br><br>
 <a name="test-dedicated-server-sample"></a>
## Test the dedicated game server sample in multiplayer

To run the dedicated game server sample as a multiplayer game, you can use [Multiplayer Play Mode](https://docs-multiplayer.unity3d.com/mppm/current/about/) to run multiple instances of the game locally, or connect to a friend over the internet. For more information, refer to [Testing locally](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/testing/testing_locally).

To change the total number of players allowed in a single session, do the following:
1. Open the StartupScene
2. In the Hierarchy window, select the **ApplicationEntryPoint** GameObject.
3. In the **ApplicationEntryPoint** component, adjust the numbers in the **Min Players** and **Max Players** fields.
<br><br>

### Use Multiplayer Play Mode to test the dedicated game server sample

Multiplayer Play Mode allows quicker testing iterations without requiring to generate builds. To use this feature, you will first have to [enable one or more virtual players](https://docs-multiplayer.unity3d.com/mppm/current/virtual-players/). Each of those players can be a client or a server. To start testing, assign the server role to either the main editor or a player, then assign the client role to the rest. You can assign those roles using the same dropdown as the one on the main editor. You will then be able to have them connect to each other via direct connection, or have clients connect to a server hosted by Game Server Hosting via Matchmaker if both services are already setup in the dashboard (see the above sections about setting up Game Server Hosting and Matchmaker). However, make sure that the server build that is hosted is up to date.

### Test the dedicated server sample in a build

To generate a build to test the sample locally or over the internet do the following: 
1. Open the Build window (menu: **DedicatedGameServerSample** > **Builds**). 
2. Select the **Toggle...** option for the platform(s) you want to build for.
3. Select **Build Client(s**), **Build Server(s**), or **Build Client(s) and Server(s)** to generate the builds.

### Set up multiplayer in a local server

After you create a build to test the sample, you can run a server build to host the server and run client builds to join the server. To connect to a local server, select **Join with Direct IP** and use the IP address 127.0.0.1.

### Set up multiplayer over the internet

To test the dedicated server sample in multiplayer over the internet, every client needs to use the same build version. This means you need to share the build executables to each client. Once each client has the same build, you can connect using one of the following ways: 
* Use a direct IP address to connect to a server hosted by one of the playtesters. Note: the person hosting the server will need to open their ports and share their public IP address to allow clients to connect to the server. 
* Use the Matchmaker and Game Server Hosting services to connect to a server on the cloud.
Both of these options work in the editor and in Multiplayer Play Mode clones, but a build version stays the same acrross multiple clients. This reduces the risk of incompatibilities if you make any changes in the editor.

#### Use a direct IP address

To use a public IP address to connect players to the server in a client build, do the following: 
1. Run the server executable on a player's machine.
2. [Open the player's port](https://docs-multiplayer.unity3d.com/netcode/current/learn/faq/#what-are-recommendations-for-getting-a-game-connecting-over-the-internet) on the server to allow other players to connect to it.
3. On Clients, enter the opened port and public IP address of the server in the **Join with Direct IP** screen of the game.


---

### 💡 Bitesize Readme
Check out our main [Bitesize Samples GitHub Readme](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize#readme) for more documentation, resources, releases, contribution guidelines, and our feedback form.

---
<br>

[![Documentation](https://img.shields.io/badge/Unity-bitesize--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-introduction)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)