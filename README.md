# FModel
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/total.svg?color=green&label=Downloads&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel/releases)
[![](https://img.shields.io/badge/License-GPL-blue.svg?logo=gnu)](https://github.com/iAmAsval/FModel/blob/master/LICENSE)
[![](https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter)](https://twitter.com/AsvalFN)
[![](https://img.shields.io/badge/Discord-Need%20Help%3F-7289da.svg?logo=discord)](https://discord.gg/JmWvXKb)

**A Fortnite .PAK files explorer built in C#**



## GETTING STARTED
### Prerequisites
[.NET Framework 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework/net471)
### Download
[![](https://img.shields.io/badge/Release-Executable-orange.svg?logo=github)](https://github.com/iAmAsval/FModel/releases/tag/2.2)
### How does it works
**1.** Once you start the executable, you'll be asked to set your path to your Fortnite .PAK files. Meanwhile a `FModel` subfolder will be created in your `Documents` folder and it'll automatically download the latest version of the custom [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) in this subfolder.

![](https://i.imgur.com/NQWSBc2.gif)

**2.** Restart the executable, enter the AES key, click **Load** and select your .PAK file
  - It will parse all Assets contained in the selected .PAK file with their respective path
  
**3.** Navigate through the tree to find the Asset you want

**4.** Clicking on **Extract** will extract the selected Asset to your `Documents` folder, try to serialize it and will display infos about it
  - Asset is an **_ID_**:
    - Try to create an [Icon](https://i.imgur.com/etUcOEj.png) with **Name**, **Description**, **Rarity**, **Type** and the **Cosmetic Source**
  - Asset is a **_Texture_**:
    - Try to display the Asset as PNG
  - Asset is a **_Sound_**:
    - Try to convert the Asset to OGG and play the sound
  - Asset is a **_Bundle Of Challenges_**:
    - Try to create an [Icon](https://i.imgur.com/1Uzrlb0.png) with all **Challenges' Description**, **Count** and the **Reward**
  - Asset is a **_Font_**:
    - Try to convert the Asset to OTF

### Difference Mode
**1.** Create a backup of your .PAK files before the update (**Load** -> **Backup PAKs**)

**2.** Enable Difference Mode

**3.** Click `Load Difference`

![](https://i.imgur.com/YvGn91l.gif)

### Update Mode
**1.** Enable Difference Mode, then Update Mode

**2.** Choose your Assets to extract

**3.** Click `Load And Extract Difference`

[Demonstration](https://streamable.com/234bg)



## DOCUMENTATION
### Important
If you find this repository useful, feel free to give it a :star: thank you :kissing_heart:

If somehow FModel crash because of permissions, please either disable Windows Defender or add and exception for FModel.exe
### Features
1. Extract
2. Serialize (CTRL+F/G/I support)
3. Filter
4. Create Cosmetics Icon
5. Merge Icons
6. Backup current .PAK files
7. Load difference between current .PAK files and backup file
8. Load, Extract and Save Assets automatically between current .PAK files and backup file
### What i'm using
- [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) - Custom Version With Output Control And No `key.txt`
- [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)
- [JSON Parser](https://app.quicktype.io/)
- [ScintillaNET](https://www.nuget.org/packages/jacobslusser.ScintillaNET)
- [Find & Replace for ScintillaNET](https://www.nuget.org/packages/snt.ScintillaNet.FindReplaceDialog/)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
### Why FModel
This project is mainly based on what [UModel](https://github.com/gildor2/UModel) can do, in a personalized way, in case UModel doesn't work, as a temporary rescue solution.
I'd highly suggest you to use [UModel](https://github.com/gildor2/UModel) instead if you wanna use something made professionnaly.

## TODO
- [ ] Support for meshes
- [ ] Support for animations
- [ ] Display support for .locres files
- [x] Code refactoring
- [x] Multithreading
- [x] Stop button
- [x] Auto update
- [x] CTRL F, CTRL G, CTRL I for jsonTextBox
- [x] Update Mode
- [x] Search through PAKs
- [x] Improve speed
- [x] Filter for the items ListBox
- [x] Quest viewer or something
- [x] Load all paks
- [x] Load only difference between 2 paks version
- [x] Custom watermark option on icons
