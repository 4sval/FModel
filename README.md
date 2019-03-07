# FModel
[![](https://img.shields.io/badge/Releases-Executable-orange.svg?logo=github)](https://github.com/iAmAsval/FModel/releases)
[![](https://img.shields.io/github/downloads/iAmAsval/FModel/0.11/total.svg?color=green&label=Downloads&logo=buzzfeed&logoColor=white)](https://github.com/iAmAsval/FModel/releases/tag/0.11)
[![](https://img.shields.io/badge/License-GPL-blue.svg?logo=gnu)](https://github.com/iAmAsval/FModel/blob/master/LICENSE)
[![](https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter)](https://twitter.com/AsvalFN)
[![](https://img.shields.io/badge/Discord-Need%20Help%3F-7289da.svg?logo=discord)](https://discord.gg/JmWvXKb)

**A Fortnite .PAK file explorer built in C#**



## GETTING STARTED
### Prerequisites
[.NET Framework 4.6.1](https://dotnet.microsoft.com/download/dotnet-framework-runtime/net461)
### How does it works
**1.** Once you start the executable, a `FModel` subfolder will be created in your `Documents` folder as well as a `config.json` file and it'll automatically download the latest version of the modded Fortnite Asset Parser.

**2.** Open the config file and fill `pathToFortnitePAKs` with the path to your Fortnite .PAK files
```json
{
  "pathToFortnitePAKs": "C:\\Program Files\\Epic Games\\Fortnite\\FortniteGame\\Content\\Paks"
}
```

**3.** Restart the executable, select your .PAK file, enter the AES key and click **Load**
  - It will parse all Assets contained in the selected .PAK file with their respective path
  
**4.** Navigate through the tree to find the Asset you want

**5.** Clicking on **Extract** will extract the selected Asset to your `Documents` folder, try to serialize it and will display infos about it
  - Asset is an **_ID_**:
    - Try to create an [Icon](https://i.imgur.com/CkiU3p5.png) with **Name**, **Description**, **Rarity**, **Type** and the **Cosmetic Source**
  - Asset is a **_Texture_**:
    - Try to display the Asset as PNG
  - Asset is a **_Sound_**:
    - Try to convert the Asset to OGG and play the sound



## DOCUMENTATION
### What i'm using
- [Fortnite Asset Parser](https://github.com/SirWaddles/JohnWickParse) - Modded Version With Output Control
- [JSON Parser](https://app.quicktype.io/)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)

## TODO
- [ ] Improve speed
- [ ] Detect the pak file of an image if default isn't working
- [x] Filter for the items ListBox
- [ ] Support for meshes
- [ ] Support for animations
- [ ] Readable code
