# FModel
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/total.svg?color=green&label=Total%20Downloads&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel/releases)
[![](https://img.shields.io/badge/License-GPL-blue.svg?logo=gnu)](https://github.com/iAmAsval/FModel/blob/master/LICENSE)
[![](https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter)](https://twitter.com/AsvalFN)
[![](https://img.shields.io/badge/Discord-Need%20Help%3F-7289da.svg?logo=discord)](https://discord.gg/JmWvXKb)

**A Fortnite .PAK files explorer built in C#**

## GETTING STARTED
### Prerequisites
[.NET Framework 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework/net471)
### Download
[![](https://img.shields.io/badge/Release-Executable-orange.svg?logo=github)](https://github.com/iAmAsval/FModel/releases/tag/2.4.0)
### How does it works
**1.** Once you start the executable, you'll be asked to set your path to your Fortnite .PAK files. Meanwhile a `FModel` subfolder will be created in your `Documents` folder.
![](https://i.imgur.com/O2Vg3Bx.gif)

**2.** Restart the executable, go to the AES Manager and add you AES Keys, click **Load** and select your .PAK file
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
    - Try to create an [Icon](https://i.imgur.com/pUVxUih.png) with all **Challenges' Description**, **Count** and the **Reward**
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
If issues occur when compiling the source code, make sure that the software is being built for x64.

If somehow FModel crash because of permissions, please either disable Windows Defender or add and exception for FModel.exe.
Also if you find this project useful, feel free to give it a :star: thank you :kissing_heart:
### Features
 1. Extract
 2. Serialize (CTRL+F/G/I support)
 3. Filter & Search
 4. Create Cosmetics Icon
 5. Create Challenges Icon
 6. Merge Icons
 7. Backup current .PAK files
 8. Load difference between current .PAK files and backup file
 9. Load, Extract and Save Assets automatically between current .PAK files and backup file
### What i'm using
  - [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) - *C# Bind*
  - [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)
  - [JSON Parser](https://app.quicktype.io/)
  - [ScintillaNET](https://www.nuget.org/packages/jacobslusser.ScintillaNET)
  - [Find & Replace for ScintillaNET](https://www.nuget.org/packages/snt.ScintillaNet.FindReplaceDialog/)
  - [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
### Contributors
| <a href="https://github.com/SirWaddles" target="_blank">**Waddlesworth**</a> | <a href="https://github.com/MaikyM" target="_blank">**Maiky M**</a> | <a href="https://github.com/AyeTSG" target="_blank">**AyeTSG**</a> | <a href="https://github.com/ItsFireMonkey" target="_blank">**FireMonkey**</a> |
| :---: | :---: | :---: | :---: |
| [![Waddlesworth](https://avatars1.githubusercontent.com/u/769399?s=200&v=4)](https://github.com/SirWaddles) | [![Maiky M](https://avatars3.githubusercontent.com/u/51415805?s=200&v=4)](https://github.com/MaikyM) | [![AyeTSG](https://avatars1.githubusercontent.com/u/49595354?s=200&v=4)](https://github.com/AyeTSG) | [![FireMonkey](https://avatars2.githubusercontent.com/u/38590471?s=200&v=4)](https://github.com/ItsFireMonkey) |
| <a href="https://github.com/SirWaddles" target="_blank">`https://github.com/SirWaddles`</a> | <a href="https://twitter.com/MaikyMOficial" target="_blank">`https://twitter.com/MaikyMOficial`</a> | <a href="https://twitter.com/AyeTSG" target="_blank">`https://twitter.com/AyeTSG`</a> | <a href="https://twitter.com/FireMonkeyFN" target="_blank">`https://twitter.com/FireMonkeyFN`</a> |
### Why FModel
This project is mainly based on what [UModel](https://github.com/gildor2/UModel) can do, in a personalized way, in case UModel doesn't work, as a temporary rescue solution.
I'd highly suggest you to use [UModel](https://github.com/gildor2/UModel) instead if you wanna use something made professionnaly.

## TODO
  - [ ] Take a look at memory usage
  - [ ] Code refactoring
  - [ ] 2FA support
  - [ ] Export Queue
  - [x] Translation support
  - [x] AES Manager
  - [x] Display support for .locres files
  - [x] Nintendo Switch PAKs
  - [x] Multithreading
  - [x] Stop button
  - [x] Auto update
  - [x] CTRL F, CTRL G, CTRL I for jsonTextBox
  - [x] STW Icons
  - [x] Update Mode
  - [x] Search through PAKs
  - [x] Improve speed
  - [x] Filter for the items ListBox
  - [x] Quest viewer or something
  - [x] Load all paks
  - [x] Load only difference between 2 paks version
  - [x] Custom watermark option on icons
