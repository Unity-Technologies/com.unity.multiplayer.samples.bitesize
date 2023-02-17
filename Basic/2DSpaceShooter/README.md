![Banner](Resources/spaceshooter_banner.png)
<br><br>

# 2D Space Shooter

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2021.3%20LTS-57b9d3.svg?logo=unity&color=2196F3)](https://unity.com/releases/editor/qa/lts-releases#:~:text=February%2014%2C%202023-,LTS%20Release,2021.3.18f1,-Released%3A%20February)
[![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-1.2.0-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/about)
[![LatestRelease](https://img.shields.io/badge/Latest%20%20Github%20Release:-v1.2.1-57b9d3.svg?logo=github&color=brightgreen)](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/releases/tag/v1.2.1)
<br><br>

This is a UNet sample project converted to Netcode for GameObjects. The 2DSpaceShooter sample is a bitesize sample designed to demonstrate networked 2D and physics-based character movement. 
<br><br>

# Sample Overview

In this sample, learn more about:

- Server authorative physics based movement using Netcode for GameObject's `NetworkRigidbody2D` component
- Managing health and a list of buffs for your players in a multiplayer game with a `NetworkVariable`
- How to pool network objects such as bullets and asteroids to improve performance
<br><br>
---
### 💡 Documentation
Check out the [2D Space Shooter sample documentation](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-spaceshooter) for a more in-depth technical breakdown of our engineering decisions and why the sample works the way it does.

---
<br>

## Exploring the Sample

The entry scene for this game is the network scene. From there a game can be hosted or an existing game can be joined. Control the ship using WASD and shoot asteroids (or other players!) using the spacebar. Fly over pickups in the scene to get different temporary buffs for your ship (like increased fly speed or shooting extra bullets).
<br><br>


---
### 💡 Bitesize Readme
Check out our main [Bitesize Samples GitHub Readme](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize#readme) for more documentation, resources, releases, contribution guidelines, and our feedback form.

---
<br>

[![Documentation](https://img.shields.io/badge/Unity-bitesize--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-introduction)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)