# FModel
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/total.svg?color=green&label=Total%20Downloads&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel/releases)
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/latest/total.svg?label=2.4.2.2&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel//releases/latest)
[![](https://img.shields.io/badge/License-GPL-blue.svg?logo=gnu)](https://github.com/iAmAsval/FModel/blob/master/LICENSE)
[![](https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter)](https://twitter.com/AsvalFN)
[![](https://img.shields.io/badge/Discord-Need%20Help%3F-7289da.svg?logo=discord)](https://discord.gg/JmWvXKb)

**A Fortnite .PAK files explorer built in C#**

## GETTING STARTED
### Prerequisites
[.NET Framework 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework/net471)
### Download
[![](https://img.shields.io/badge/Release-2.4.2.2-orange.svg?logo=github)](https://github.com/iAmAsval/FModel/releases/latest)
### How To Use
**1.** Once you start the executable, you'll be asked to set your path to your Fortnite .PAK files. Meanwhile a `FModel` subfolder will be created in your `Documents` folder.
![](https://i.imgur.com/9AUVUVU.gif)

**2.** Restart the executable, go to the AES Manager and add your AES Keys, click **Load** and select your .PAK file
- It will parse all Assets contained in the selected .PAK file with their respective path
  
**3.** Navigate through the tree to find the Asset you want

**4.** Clicking on **Extract** will extract the selected Asset to your `Documents` folder, it will also try to serialize it and will display information about it
- if the Asset is an **_Item Definition_**:
    - Try to create an [Icon](https://i.imgur.com/8hxXSsA.png) with **Name**, **Description**, **Rarity**, **Type**, **Cosmetic Source** and the **Cosmetic Set**
- if the Asset is a **_Bundle Of Challenges_**:
    - Try to create an [Icon](https://i.imgur.com/pUVxUih.png) with all **Challenges' Description**, **Count** and the **Reward**
- if the Asset is a **_Texture_**:
    - Try to display the Asset as PNG
- if the Asset is a **_Sound_**:
    - Try to convert the Asset to OGG and play the sound
- if the Asset is a **_Font_**:
    - Try to convert the Asset to OTF

### Difference Mode
**1.** Create a backup of your .PAK files before the update (**Load** -> **Backup PAKs**)

**2.** Enable Difference Mode

**3.** Click `Load Difference`

![](https://i.imgur.com/36icHam.gif)

### Update Mode
**1.** Enable Difference Mode, then Update Mode

**2.** Choose your Assets to extract

**3.** Click `Load And Extract Difference`

[Demonstration](https://streamable.com/234bg)

## DOCUMENTATION
### Important
If issues occur when compiling the source code, make sure that the software is being built for x64.

If somehow FModel crashed due to permissions, please either disable Windows Defender or add and exception for FModel.exe.
Also if you find this project useful, feel free to give it a :star: thank you :kissing_heart:
### Features
 1. Read, Search, Extract, Serialize
 2. Icon Creation for various BR/STW Cosmetics or Challenges with language support
 3. Icon Merger
 4. Automatic Key detection for Dynamic PAKs
 5. Twitter Api Authentication to send Tweets from within FModel
### What i'm using
  - [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) - *C# Bind*
  - [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)
  - [JSON Parser](https://app.quicktype.io/)
  - [ScintillaNET](https://www.nuget.org/packages/jacobslusser.ScintillaNET)
  - [Find & Replace for ScintillaNET](https://www.nuget.org/packages/snt.ScintillaNet.FindReplaceDialog/)
  - [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
### Contributors
<table><tr><td align="center"><a href="https://github.com/SirWaddles"><img src="https://avatars1.githubusercontent.com/u/769399?s=200&v=4" width="100px;" alt="Waddlesworth"/><br /><sub><b>Waddlesworth</b></sub></a><br><a href="https://github.com/SirWaddles" title="Github">🔧</a></td><td align="center"><a href="https://github.com/MaikyM"><img src="https://avatars3.githubusercontent.com/u/51415805?s=200&v=4" width="100px;" alt="Maiky M"/><br /><sub><b>Maiky M</b></sub></a><br /><a href="https://github.com/MaikyM" title="Github">🔧</a><a href="https://twitter.com/MaikyMOficial" title="Twitter">🐦</a></td><td align="center"><a href="https://github.com/AyeTSG"><img src="https://avatars1.githubusercontent.com/u/49595354?s=200&v=4" width="100px;" alt="AyeTSG"/><br><sub><b>AyeTSG</b></sub></a><br><a href="https://github.com/AyeTSG" title="Github">🔧</a><a href="https://twitter.com/AyeTSG" title="Twitter">🐦</a></td><td align="center"><a href="https://github.com/ItsFireMonkey"><img src="https://avatars2.githubusercontent.com/u/38590471?s=200&v=4" width="100px;" alt="FireMonkey"/><br /><sub><b>FireMonkey</b></sub></a><br><a href="https://github.com/ItsFireMonkey" title="Github">🔧</a><a href="https://twitter.com/iFireMonkey" title="Twitter">🐦</a></td></tr></table>


### The History
Basically i was bored and wanted to make something like [UModel](https://github.com/gildor2/UModel) but with a Fortnite Touch and more features.

I'd highly suggest you to use [UModel](https://github.com/gildor2/UModel) if you wanna use something made professionally.

## TODO
  - [ ] Code refactoring
  - [ ] Special Schematics icon design with weapon icon and ingredients
  - [ ] New Heroes icon design with perks and more
  - [ ] New Defenders icon design with useful infos
  - [x] Translation support
  - [x] AES Manager
  - [x] Display support for .locres files
  - [x] Stop button
  - [x] Auto update
  - [x] STW Icons
  - [x] Update Mode
  - [x] Search through PAKs
  - [x] Quest viewer or something
  - [x] Load all paks
  - [x] Load only difference between 2 paks version
  - [x] Custom watermark option on icons

## Removal
Contact me with an authorized, genuine email if you work for Epic Games and would like this removed.
asval.contactme@gmail.com
