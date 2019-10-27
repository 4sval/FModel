<h1 align="center">
  <br>
  <a href="https://github.com/iAmAsval/FModel"><img src="https://github.com/iAmAsval/FModel/blob/master/Images/Logo.png" alt="FModel" width="200"></a>
  <br>
  FModel
  <br>
</h1>

<h4 align="center">A powerful .PAK file explorer fully dedicated to Fortnite.</h4>

<p align="center">
  <a href="https://github.com/iAmAsval/FModel/releases/latest">
    <img src="https://img.shields.io/github/v/release/iamasval/fmodel"
         alt="Releases">
  </a>
  <a href="https://github.com/iAmAsval/FModel/releases/latest">
    <img src="https://img.shields.io/github/downloads/iAmAsval/FModel/latest/total.svg?label=v3.0%20Downloads"
         alt="Downloads">
  </a>
  <a href="https://twitter.com/AsvalFN"><img src="https://img.shields.io/badge/Twitter-@AsvalFN-1da1f2.svg?logo=twitter"></a>
  <a href="https://discord.gg/fdkNYYQ">
      <img src="https://img.shields.io/badge/Discord-Need%20Help%3F-778cd4.svg?logo=discord">
  </a>
  <a href="https://www.paypal.me/FModel">
    <img src="https://img.shields.io/badge/Paypal-Donate-00457C.svg?logo=paypal">
  </a>
</p>

------

<p align="center">
  <a href="#key-features">Key Features</a> •
  <a href="#how-to-use">How To Use</a> •
  <a href="#download">Download</a> •
  <a href="#contributors">Contributors</a> •
  <a href="#credits">Credits</a> •
  <a href="#license">License</a>
</p>

![demo](https://github.com/iAmAsval/FModel/blob/master/Images/FModel_Demo.gif)

## Key Features

* .PAK Files
  - Load one or all of them
  - Load the difference between 2 Fortnite versions
  - Load difference and auto generate & save icons for you
  - Backup your current files
* Assets
  - Extract and show the deserialized json string
  - Save JSON string
  - Export RAW data
  - Copy asset path
  - Get asset properties
* Filter & Search assets by their name
* Icon Generation
  - Battle Royale Cosmetics
  - Battle Royale Challenges
  - Save The World Heroes
  - Save The World Defenders
  - Weapons
  - Schematics
  - Other various assets
  - Supports for 15 different languages
* Save images
* Merge images
* Hex Viewer
* Sound Player
* Night Mode

## How To Use

To run this program, you'll need [.NET Framework 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework/net471) or newer installed on your computer. Once it's done, you can download the latest release [here](https://github.com/iAmAsval/FModel/releases/latest/download/FModel.zip).

1. Extract `FModel.exe` and `libSkiaSharp.dll` somewhere - __double click on FModel.exe__
2. You're now asked to set the path to your Fortnite .PAK files - __click on Load and then on Settings__
3. You'll need the AES key in order to read the files - __click on AES Manager and enter the key under Static Key__
4. You can now load one or all .PAK files
5. Navigate through the tree to find the Asset you want

### Difference Mode

The difference mode can check new files between 2 different Fortnite versions. You'll need at least one backup file to be able to compare your current .PAK files and the backup file.

1. You can target file size check on the settings window if you want - __click on Load, Settings, and then check Diff w/ File Size__
2. Enable the difference mode - __click on Load and then on Difference Mode__
3. Compare files - __click on Load and then on Load Difference__
4. It's gonna check you current .PAK files and then ask you for the backup file - __choose the backup file__
5. You can now see the difference between your backup file and you current files

## Download

You can download the latest version of FModel for Windows x64 [here](https://github.com/iAmAsval/FModel/releases/latest/download/FModel.zip).
For x32 users (i wonder how you play the game), you just have to clone or download the repository and build FModel on Visual Studio, make sure to target x32 Platforms in the solution properties.

## Contributors

<table><tr><td align="center"><a href="https://github.com/SirWaddles"><img src="https://avatars1.githubusercontent.com/u/769399?s=200&v=4" width="100px;" alt="Waddlesworth"/><br /><sub><b>Waddlesworth</b></sub></a><br><a href="https://github.com/SirWaddles" title="Github">🔧</a></td><td align="center"><a href="https://github.com/MaikyM"><img src="https://avatars3.githubusercontent.com/u/51415805?s=200&v=4" width="100px;" alt="Maiky"/><br /><sub><b>Maiky</b></sub></a><br /><a href="https://github.com/MaikyM" title="Github">🔧</a><a href="https://twitter.com/MaikyMOficial" title="Twitter">🐦</a></td><td align="center"><a href="https://github.com/WorkingRobot"><img src="https://avatars1.githubusercontent.com/u/16126912?s=200&v=4" width="100px;" alt="Asriel"/><br><sub><b>Asriel</b></sub></a><br><a href="https://github.com/WorkingRobot" title="Github">🔧</a><a href="https://twitter.com/Asriel_Dev" title="Twitter">🐦</a></td><td align="center"><a href="https://github.com/ItsFireMonkey"><img src="https://avatars2.githubusercontent.com/u/38590471?s=200&v=4" width="100px;" alt="FireMonkey"/><br /><sub><b>FireMonkey</b></sub></a><br><a href="https://github.com/ItsFireMonkey" title="Github">🔧</a><a href="https://twitter.com/iFireMonkey" title="Twitter">🐦</a></td></tr></table>

## Credits

This software uses the following open source packages:

- [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)
- [Avalon Edit](http://avalonedit.net/)
- [Color Picker](https://github.com/drogoganor/ColorPickerWPF)
- [Hex Editor](https://github.com/abbaye/WpfHexEditorControl)
- [Html Agility Pack (HAP)](https://html-agility-pack.net/)
- [Newtonsoft Json.NET](https://www.newtonsoft.com/json)
- [Ookii Dialogs](https://github.com/caioproiete/ookii-dialogs-wpf)
- [RestSharp](http://http://restsharp.org//)
- [SkiaSharp](https://github.com/mono/SkiaSharp)
- [WPFThemes DarkBlend](https://github.com/DanPristupov/WpfExpressionBlendTheme)
- [Writeable Bitmap Extensions](https://github.com/reneschulte/WriteableBitmapEx)

## Support

<a href="https://www.paypal.me/FModel">
  <img src="https://img.shields.io/badge/Paypal-Donate-00457C.svg?logo=paypal">
</a>

## You may also like...

- [UModel](https://github.com/gildor2/UEViewer) - **THE** Unreal Engine Viewer

## License

MIT
