<!-- NOTE: PLEASE TRY TO MATCH THE OVERALL FORMATTING AND SPACING OF THE BOSS ROOM README HERE: https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop#readme -->

![Banner Image](image.filetype)
<br><br>

# Distributed Authority Social Hub Sample
###  Sub Title
<br>

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-6000.0.21%20LTS-57b9d3.svg?logo=unity&color=2196F3)](Link-to-editor-version)
[![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-2.1.1-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/1.4.0/about)
[![LatestRelease](https://img.shields.io/badge/Latest%20Github%20Release:-vX.X.X-57b9d3.svg?logo=github&color=brightgreen)](link-to-github-release)
<br><br>

*Provide a brief description of the project, including its purpose, features, and target audience in one to two lines. Provide links to external features if necessary.*

*i.e.*
> The Distributed Authority networking topology aims to simplify the development of large-scale multiplayer games by offering a flexible authority model and an out-of-the-box backend solution, targeting developers who require scalable, performant, and cost-effective networking options.
>
> Social Hub is a beginner-friendly sample project aimed to demonstrate Distributed Authority's implementation and benefits, helping users seamlessly integrate it into their own game projects.
> 
<br>

# Distributed Authority Social Hub Overview

*Provide a more detailed description of the sample here. Explain what unique or valuable concepts or features it convers.*

*i.e.*
> Boss Room is designed to be used in its entirety to help you explore the concepts and patterns behind a multiplayer game flow; such as character abilities, casting animations to hide latency, replicated objects, RPCs, and integration with the [Relay](https://unity.com/products/relay), [Lobby](https://unity.com/products/lobby), and [Authentication](https://unity.com/products/authentication) services.
>
>You can use the project as a reference starting point for your own Unity game or use elements individually.

<br>

------
## Readme Contents and Quick Links

<!-- add or remove sections as necessary -->

<details open>
<summary> <b>Click to expand/collapse contents</b> </summary>

- ### [Getting the project](#getting-the-project-1)
  - [Direct download](#direct-download)
  - [Installing Git LFS to clone locally](#installing-git-lfs-to-clone-locally)
- ### [Requirements](#requirements-1)
  - [Min Spec Devices](#boss-rooms-min-spec-devices-are)
- ### [Opening the project for the first time](#opening-the-project-for-the-first-time-1) 
- ### [Exploring the project](#exploring-the-project-1)
  - [Registering with Unity Gaming Services (UGS)](#registering-the-project-with-unity-gaming-services-ugs)
- ### [Testing multiplayer](#testing-multiplayer-1) 
  - [Local Multiplayer Setup](#local-multiplayer-setup)
  - [Multiplayer over Internet](#multiplayer-over-internet)
  - [Relay Setup](#relay-setup) 
- ### [Index of resources in this project](#index-of-resources-in-this-project-1)
  - [Gameplay](#gameplay)
  - [Game Flow](#game-flow)
  - [Connectivity](#connectivity)
  - [Services (Lobby, Relay, etc)](#services-lobby-relay-etc)
  - [Tools and Utilities](#tools-and-utilities)
- ### [Troubleshooting](#troubleshooting-1)
  - [Bugs](#bugs)
  - [Documentation](#documentation)
- ### [License](#license-1)
- ### [Contributing](#contributing-1)
- ### [Community](#community-1)
- ### [Feedback Form](#feedback-form-1)
- ### [Other samples](#other-samples-1)
  - [Bite-size Samples](#bite-size-samples)
</details>

------
<br>

## Getting the project

*Explain how users can directly download the latest version of the project. Provide a link and any specific instructions or considerations for the download process.*

*i.e.*
> ### Direct download
> - You can download the latest version of `Sample Name` from our [Releases](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases) page. <!-- make sure this is the correct version linked -->
> - **Alternatively:** click the green `Code` button and then select the 'Download Zip' option.  Please note that this will download the branch that you are currently viewing on Github.  
> - **Windows users:** Using Windows' built-in extraction tool may generate an "Error 0x80010135: Path too long" error window which can invalidate the extraction process. A workaround for this is to shorten the zip file to a single character (eg. "c.zip") and move it to the shortest path on your computer (most often right at C:\\) and retry. If that solution fails, another workaround is to extract the downloaded zip file using [7zip](https://www.7-zip.org/).


<br>

## Requirements

*List the software and hardware requirements for running the project. Include any specific versions or dependencies required.*

*i.e.*
> `Sample Name` is compatible with the latest Unity Long Term Support (LTS) editor version, currently [202X LTS](https://unity.com/releases/editor/qa/lts-releases?version=2022.3). Please include standalone support for Windows/Mac in your installation. <!-- make sure this is the correct version linked -->
> 
> **PLEASE NOTE:** You will also need Netcode for Game Objects to use these samples. See the [Installation Documentation](https://docs-multiplayer.unity3d.com/netcode/current/installation) to prepare your environment. You can also complete the [Get Started With NGO](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/get-started-ngo) tutorial to familiarize yourself with Netcode For Game Objects.
> <br><br>
> 
> `Sample Name` has been developed and tested on the following platforms:
> -  Windows
> - Mac
> - iOS
> - Android
>
> `Sample Name`'s min spec devices are:
> - iPhone 6S
> - Samsung Galaxy J2 Core

<!-- ADD IF NECESSARY

### Installing Git LFS to clone locally

`Sample Name` uses Git Large Files Support (LFS) to handle all large assets required locally. See [Git LFS installation options](https://github.com/git-lfs/git-lfs/wiki/Installation) for Windows and Mac instructions. This step is only needed if cloning locally. You can also just download the project which will already include large files.
<br><br>

-->

<br><br>

## Opening the project for the First Time

*Provide instructions on how to open the project in the development environment. This may include installing the necessary software, setting up the project configuration, or importing any dependencies.*

*i.e.*
> Once you have downloaded the project, follow the steps below to get up and running:
> - Check that you have installed the most recent [LTS editor version](https://unity.com/releases/2021-lts). <!-- make sure this is up to date -->
>   - Include standalone support for Windows/Mac in your installation. 
> - Add the project to the _Unity Hub_ by clicking on the **Add** button and pointing it to the root folder of the downloaded project.
>   - __Please note :__ the first time you open the project Unity will import all assets, which will take longer than usual.
> - Hit the **Play** button. You can then host a new game or join an existing one using the in-game UI.

<br>

## Exploring the project

*Provide an overview of the project structure and its main components. Explain the purpose and functionality of important files or directories. You can mention any architectural patterns or design principles used in the project.*

*i.e.*
> BossRoom is an eight-player co-op RPG game experience, where players collaborate to fight imps, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by a mouse button or hotkey.
> 
> One of the eight clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.
> 
> - The game is server-authoritative, with latency-masking animations. 
> - Position updates are carried out through NetworkTransform that sync position and rotation. 
> 
> Code is organized in domain-based assemblies. See the [Boss Room architecture documentation](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/bossroom-architecture) file for more details.

<br>

### Registering the project with Unity Gaming Services (UGS)

If the project requires registration or integration with Unity Gaming Services, provide instructions on how to register the project and set up the *necessary services. Include any specific steps or configurations required.*

*i.e.*
> Boss Room leverages several services from UGS to facilitate connectivity between players. To use these services inside your project, you must [create an organization](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-) inside the Unity Dashboard, and enable the [Relay](https://docs.unity.com/relay/get-started.html) and [Lobby](https://docs.unity.com/lobby/game-lobby-sample.html) services. Otherwise, you can still use Boss Room without UGS.

<br>

## Testing multiplayer

Explain how to test the multiplayer functionality of the project. Include instructions for local multiplayer setup and multiplayer over the internet. Provide any specific steps or configurations required for successful multiplayer testing.

i.e
> In order to see the multiplayer functionality in action we can either run multiple instances of the game locally on your computer - using either ParrelSync or builds - or choose to connect to a friend over the internet. See [how to test](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/testing/testing_locally) for more info.

<br>

### Local multiplayer setup

*Explain the process of setting up and testing local multiplayer. Include any instructions or considerations for running multiple instances of the game locally.*

### Multiplayer over Internet

*Explain the process of setting up and testing multiplayer over the internet. If the project requires specific configurations or services (e.g., relay servers), provide instructions on how to set them up.*

*i.e.*
> To play over internet, first build an executable that is shared between all players - as above.
>
> It is possible to connect between multiple instances of the same executable OR between executables and the editor that produced it.
> 
> Running the game over internet currently requires setting up a relay.

### Relay Setup

*If the project requires the use of a relay server, provide instructions on how to set up and configure the relay server. Include any necessary steps or configurations.*

*i.e*
> - Boss Room provides an integration with [Unity Relay](https://docs-multiplayer.unity3d.com/netcode/current/relay/relay). You can find our Unity Relay setup guide [here](https://docs-multiplayer.unity3d.com/netcode/current/relay/relay)
> 
> - Alternatively you can use Port Forwarding. The https://portforward.com/ site has guides on how to enable port forwarding on a huge number of routers.
> - Boss Room uses `UDP` and needs a `9998` external port to be open. 
> - Make sure your host's address listens on 0.0.0.0 (127.0.0.1 is for local development only).

<br>

-----

## Index of resources in this project

<details open>
<summary> <b>Click to expand/collapse contents</b> </summary>
<!--- Add or Remove different category headings as you need -->

### Gameplay
* Feature here - [link in project](url)

### Game Flow
* Feature here - [link in project](url)

### Connectivity
* Feature here - [link in project](url)

### Services (Lobby, Relay, etc)
* Feature here - [link in project](url)

### Tools and Utilities
* Feature here - [link in project](url)
</details>

-----

<br>

## Troubleshooting

### Bugs

*Instruct users on how to report bugs or issues related to the project. Provide clear instructions on how to submit bug reports, including any required information or steps to reproduce the issue.*

*i.e.*
> - Report bugs in `Sample Name` using Github [issues](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/issues)
>- Report NGO bugs using NGO Github [issues](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
>- Report Unity bugs using the [Unity bug submission process](https://unity3d.com/unity/qa/bug-reporting).

### Documentation

*If there is additional documentation or resources available for the project, provide links or references to help users find more information. This can include official documentation, tutorials, forums, or any external resources that may be useful.*

*i.e.*
> For a deep dive into Unity Netcode and `Sample Name`, visit our [documentation site](https://docs-multiplayer.unity3d.com/).


## License

*Specify the license under which the project is released. Include any specific terms or conditions associated with the license.*

*i.e.*
> `Sample Name` is licensed under the Unity Companion License. See [LICENSE.md](LICENSE.md) for more legal information.
>
> For a deep dive in Unity Netcode and `Sample Name`, visit our [docs site](https://docs-multiplayer.unity3d.com/).

## Contributing

*Explain how others can contribute to the project. Include guidelines for submitting pull requests, contributing code, reporting issues, or providing feedback. Mention any specific processes or workflows for contributing to the project.*

*i.e.*
> We welcome your contributions to this sample code and objects. See our [contribution guidelines](CONTRIBUTING.md) for details.
>   
> Our projects use the `git-flow` branching strategy:
>  - our **`develop`** branch contains all active development
>  - our **`main`** branch contains release versions
> 
> To get the project on your machine you need to clone the repository from GitHub using the following command-line command:
> ```
> git clone https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git
> ```
>
>**PLEASE NOTE:** You will need to have [Git LFS](https://git-lfs.github.com/) installed on your local machine in order to clone our repo.

## Community

*Provide links to the project's community channels, such as forums, Discord servers, mailing lists, or social media platforms. Encourage users to join the community, ask questions, and engage with other users or contributors.*

*i.e.*
> For help, questions, networking advice, or discussions about Netcode for GameObjects and its samples, please join our [Discord Community](https://discord.gg/FM8SE9E) or create a post in the [Unity Multiplayer Forum](https://forum.unity.com/forums/netcode-for-gameobjects.661/).

## Feedback Form

*Encourage users to provide feedback about the project. Include a link or instructions for submitting feedback. This can be in the form of surveys, feedback forms, or any other preferred method for collecting user feedback. Express appreciation for user feedback and mention how it can help improve the project.*

*i.e.*
> Thank you for cloning `Sample Name` and taking a look at the project. To help us improve and build better samples in the future, please consider submitting feedback about your experiences with `Sample Name` and let us know if you were able to learn everything you needed to today. It'll only take a couple of minutes. Thanks!
>
> [`Sample Name` Feedback Form](https://unitytech.typeform.com/bossroom)

## Other samples

*If there are other related samples or projects available, provide links or references to these samples. Explain how they are related to the main project and how users can benefit from exploring them.*

*i.e.*
> ### Bite-size Samples
> - The [Bitesize Samples](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize)  repository is currently being expanded and contains a collection of smaller samples and games, showcasing sub-features of NGO. You can review these samples with documentation to understand our APIs and features better.

<br>

[![Documentation](https://img.shields.io/badge/Unity-boss--room--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/bossroom)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)
