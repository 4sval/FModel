# FModel
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/total.svg?color=green&label=Downloads&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel/releases)
[![](https://img.shields.io/badge/License-GPL-blue.svg?logo=gnu)](https://github.com/iAmAsval/FModel/blob/master/LICENSE)
[![](https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter)](https://twitter.com/AsvalFN)
[![](https://img.shields.io/badge/Discord-Need%20Help%3F-7289da.svg?logo=discord)](https://discord.gg/JmWvXKb)

**A Fortnite .PAK file explorer built in C#**



## GETTING STARTED
### Prerequisites
[.NET Framework 4.6.1](https://dotnet.microsoft.com/download/dotnet-framework-runtime/net461)
### Download
[![](https://img.shields.io/badge/Release-Executable-orange.svg?logo=github)](https://github.com/iAmAsval/FModel/releases/tag/1.3)
### How does it works
**1.** Once you start the executable, you'll be asked to set your path to your Fortnite .PAK files. Meanwhile a `FModel` subfolder will be created in your `Documents` folder and it'll automatically download the latest version of the modded Fortnite Asset Parser in this subfolder.
![](https://i.imgur.com/sO6G6Vy.gif)

**2.** Restart the executable, select your .PAK file, enter the AES key and click **Load**
  - It will parse all Assets contained in the selected .PAK file with their respective path
  
**3.** Navigate through the tree to find the Asset you want

**4.** Clicking on **Extract Asset** will extract the selected Asset to your `Documents` folder, try to serialize it and will display infos about it
  - Asset is an **_ID_**:
    - Try to create an [Icon](https://i.imgur.com/R0OhRpw.png) with **Name**, **Description**, **Rarity**, **Type** and the **Cosmetic Source**
  - Asset is a **_Texture_**:
    - Try to display the Asset as PNG
  - Asset is a **_Sound_**:
    - Try to convert the Asset to OGG and play the sound
  - Asset is a **_Bundle Of Challenges_**:
    - Try to create an [Icon](https://i.imgur.com/6AjoVVm.png) with all challenges' description & amount needed to complete them
  - Asset is a **_Font_**:
    - Try to convert the Asset to OTF

### Load Difference Between 2 Fortnite Version
**1.** Create a backup of your PAK files before the update
**2.** Enable PAKs Diff
**3.** Click `Load Difference`
![](https://i.imgur.com/5zFOXbY.gif)



## DOCUMENTATION
### Features
1. Extract
2. Serialize
3. Filter
4. Icon Creation
5. Save Icon
6. Icons Merger
7. Backup current PAK files
8. Load only difference between current PAK files and backup file
### What i'm using
- [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) - Modded Version With Output Control
- [JSON Parser](https://app.quicktype.io/)
- [ScintillaNET](https://www.nuget.org/packages/jacobslusser.ScintillaNET)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
### Why FModel
This project is mainly based on what [UModel](https://github.com/gildor2/UModel) can do, in a personalized way, in case UModel doesn't work, as a temporary rescue solution.
I'd highly suggest you to use [UModel](https://github.com/gildor2/UModel) instead if you wanna use something made professionnaly.

## TODO
- [x] Improve speed
- [x] Multithreading - Need improvements
- [x] Filter for the items ListBox
- [x] Quest viewer or something
- [x] Load all paks
- [ ] Shop loader ?
- [x] Load only difference between 2 paks version
- [ ] Custom watermark option on icons
- [x] Choose between extracted filename or displayName for icons file name
- [ ] Stop button while extracting
- [ ] Support for meshes
- [ ] Support for animations
