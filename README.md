# FModel
[![](https://img.shields.io/badge/Releases-Executable-orange.svg?logo=github)](https://github.com/iAmAsval/FModel/releases)
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/1.1/total.svg?color=green&label=Downloads&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel/releases/tag/1.1)
[![](https://img.shields.io/badge/License-GPL-blue.svg?logo=gnu)](https://github.com/iAmAsval/FModel/blob/master/LICENSE)
[![](https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter)](https://twitter.com/AsvalFN)
[![](https://img.shields.io/badge/Discord-Need%20Help%3F-7289da.svg?logo=discord)](https://discord.gg/JmWvXKb)

**A Fortnite .PAK file explorer built in C#**



## GETTING STARTED
### Prerequisites
[.NET Framework 4.6.1](https://dotnet.microsoft.com/download/dotnet-framework-runtime/net461)
### How does it works
**1.** Once you start the executable, you'll be asked to set your path to your Fortnite .PAK files. Meanwhile a `FModel` subfolder will be created in your `Documents` folder and it'll automatically download the latest version of the modded Fortnite Asset Parser in this subfolder.
![](https://i.imgur.com/oaceS8K.gif)

**2.** Restart the executable, select your .PAK file, enter the AES key and click **Load**
  - It will parse all Assets contained in the selected .PAK file with their respective path
  
**3.** Navigate through the tree to find the Asset you want

**4.** Clicking on **Extract Asset** will extract the selected Asset to your `Documents` folder, try to serialize it and will display infos about it
  - Asset is an **_ID_**:
    - Try to create an [Icon](https://i.imgur.com/PStlmUV.png) with **Name**, **Description**, **Rarity**, **Type** and the **Cosmetic Source**
  - Asset is a **_Texture_**:
    - Try to display the Asset as PNG
  - Asset is a **_Sound_**:
    - Try to convert the Asset to OGG and play the sound
  - Asset is a **_Font_**:
    - Try to convert the Asset to OTF



## DOCUMENTATION
### What i'm using
- [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) - Modded Version With Output Control
- [JSON Parser](https://app.quicktype.io/)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
### Why FModel
This project is mainly based on what [UModel](https://github.com/gildor2/UModel) can do, in a personalized way, in case UModel doesn't work, as a temporary rescue solution.
I'd highly suggest you to use [UModel](https://github.com/gildor2/UModel) instead if you wanna use something made professionnaly.

## TODO
- [ ] Improve speed
- [x] Multithreading
- [x] Filter for the items ListBox
- [ ] More settings
- [ ] Stop button while extracting
- [ ] Support for meshes
- [ ] Support for animations
