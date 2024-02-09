# Anticipation Sample

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2021.3%20LTS-57b9d3.svg?logo=unity&color=2196F3)](https://unity.com/releases/editor/whats-new/2022.3.0)
[![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-1.9.0-57b9d3.svg?logo=unity&color=2196F3)](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/tag/ngo%2F1.7.1)
<br><br>

This sample provides examples for how the Client Anticipation feature of Netcode for GameObjects 1.9.0 can be used. It covers several use cases:

- **AnticipatedNetworkVariable:**
  - Moving network variables more responsive by anticipating server actions based on player interaction ("When the player clicks this button, they shouldn't have to wait for the server before they see it update.")
  - Handling incorrect anticipation ("If clicking the button fails, it changes back to its previous value")
  - Latency compensation for server-controlled values ("this progress bar value is from 100ms in the past due to latency, we can calculate where we expect it to be now")
  - Smoothing on incorrect anticipation ("If this progress bar moved to the wrong place, it moves smoothly to the correct one")
- **AnticipatedNetworkTransform:**
  - Responsive server-authoritative player movement:
    - By sending only input to the server, but also processing input locally to anticipate what we expect the server to do with that input, movement of a server-authoritative player object can be immediately responsive on the local client.
    - Storing input history over time allows replaying inputs to calculate a new anticipated position every time the server position updates
    - Smoothing can allow smooth interpolated movement between a previous anticipated state and a new one when small fluctuations and floating point errors result in slightly different outcomes
  - Smooth movement from other players:
    - Even without actually anticipating player movement based on latency, the OnReanticipate callback can call into Smooth on each update to create smooth player movement. (This replaces the normal Interpolate option on NetworkTransform when using AnticipatedNetworkTransform, and is shown in this sample.)

<br><br>

## Sample overview

**It is recommended that you build this sample using development builds.** The reason is that it uses the UTP network simulator to simulate 100ms of latency so that the latency compensation is easier to see, and the simulator is not available outside of the editor and development builds. Running on localhost without the simulator makes the latency very small, which makes the effectiveness of these techniques difficult to notice.

This sample shows NetworkVariables with a series of paired sliders. In each slider, the top value represents the current client local anticipated value and the bottom value represents the current authoritative value (which is to say, the most recent value we received from the server). This helps to show how anticipation hides latency - the top slider shows what AnticipatedNetworkVariable shows to the user, while the bottom value shows what a regular NetworkVariable would show if it were used instead. There are five different types of variables shown here:

- The first two (left common) are a common use case: a snap variable, where if the server for some reason does something different than we expect it to, the value will simply be updated and "snap" to the new authoritative value. The top one is the expected outcome (the server updates to the value we wanted it to) and shows the latency masking of the feature; the bottom one simulates an error condition on the server where the value changes to something other than what we wanted it to, in which case the top (anticipated) value snaps to the new value when it updates.
- The second two (middle column) match the first two, but add smoothing when the anticipated value is wrong. With only one client, there's no difference in behavior between the top left and top middle variables, but you can see the difference if you launch a second client: when one client changes the value, the other will smoothly interpolate to the new value. (This sample is set to always use smoothing for these variables; theoretically, though, a variable could conditionally smooth based on whether or not it had done an anticipation on the value.)
- The third one (right column) shows a value that slowly increments on the server, using reanticipation and smoothing to mask the latency and smooth out the jitter. You can see this easily if you place the server window over the client window: the authoritative value will be significantly behind what the server is rendering due to latency, but the anticipated value will more closely match the server value.

In addition to these NetworkVariables, there is also a player character (which moves using tank controls) to show AnticipatedNetworkTransform. There are a few things you can see here:

- On the client controlling the character, you can see the current value in the white character, and the authoritative value in the smaller gray character that follows behind it.
- On a second client, you can see the smoothing action of the other client's movement, contrasted with the jitter in the gray authoritative character.
- By pressing Q and E, you can simulate error cases where the player on the server ends up in a significantly different place than the player on the client. Q will jump the player to a random position, while the server will jump it to a different random position, leading to the client having to reconcile and update to the new correct position. E will jump the player to a random position near the center of the map, so the distance that it will have to travel to reconcile will be smaller.
- By pressing R, you can simulate a predicted teleport: the client object will jump to the center of the screen and the server object will quickly catch up with no reconciliation necessary.
- Additionally, there is a slider you can use to control the smoothing of the network transform: by default it is set to interpolate over 0.1 seconds, but this slider lets you see how changing that value affects the feel of the smoothing. Setting it to 0 will disable smoothing entirely, showing you what reanticipation without smoothing looks like (some jitter)

---
### 💡 Bitesize Readme
Check out our main [Bitesize Samples GitHub Readme](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize#readme) for more documentation, resources, releases, contribution guidelines, and our feedback form.

---
<br>

[![Documentation](https://img.shields.io/badge/Unity-bitesize--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bitesize/bitesize-introduction)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)