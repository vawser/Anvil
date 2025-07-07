# Anvil

A modding toolkit for the From Software catalogue.

[![GitHub release](https://img.shields.io/github/release/vawser/Anvil.svg)](https://github.com/vawser/Anvil/releases/latest)
[![Github All Releases](https://img.shields.io/github/downloads/vawser/DSMapStudio/total.svg)](https://github.com/vawser/Anvil/releases/latest)
[![Discord](https://img.shields.io/badge/Discord%20-%237289DA.svg?&logo=discord&logoColor=white)](https://discord.gg/5p9bRKkK4J)

## Links
Anvil is a fork of the [DSMapStudio repository](https://github.com/soulsmods/DSMapStudio)

## Requirements
* Windows 7/8/8.1/10/11 (64-bit only). Linux installations may function with wine 9+. You may need to configure for access to vulkan 1.3 and the correct gpu.
* [Visual C++ Redistributable x64](https://aka.ms/vs/16/release/vc_redist.x64.exe)
* For the Map and Model Editor: A Vulkan 1.3 compatible graphics card.

## Usage
* **Dark Souls Prepare to die Edition**: Game must be unpacked with UDSFM before usage with Map Studio (https://www.nexusmods.com/darksouls/mods/1304).
* **Dark Souls Remastered**: Game is unpacked by default and requires no other tools.
* **Dark Souls 2 SOTFS**: Use UXM (https://www.nexusmods.com/sekiro/mods/26) to unpack the game. Vanilla Dark Souls 2 is not supported.
* **Dark Souls 3 and Sekiro**: Use UXM to extract the game files.
* **Demon's Souls**: Make sure to disable the RPCS3 file cache to test changes if using an emulator.
* **Bloodborne**: Any valid full game dump should work out of the box. Note that some dumps will have the base game (1.0) and the patch as separate, so the patch should be merged on top of the base game before use with map studio. You're on your own for installing mods to console at the moment.
* **Sekiro**: Use UXM to extract game files.
* **Elden Ring**: Use UXM Selective Unpack (https://github.com/Nordgaren/UXM-Selective-Unpack) to extract the game files. It's recommended to unpack everything, but at least the `map`, `asset`, `chr`, and `msg` directories are needed for basic editor usage.
* **Armored Core 6**: Use UXM Selective Unpack (https://github.com/Nordgaren/UXM-Selective-Unpack) to extract the game files. It's recommended to unpack everything, but at least the `msg` directory is needed for basic editor usage, until map support is added.

## Credits (Smithbox)
* Vawser 
* ivi 
* nex3 
* gixxpunk 
* Strackeror 
* FireWolf700 
* GoogleBen 
* LordExelot 
* Pear0533 
* Metito 
* WarpZehpyr 
* twistedgwazi 
* FeeeeK 
* colaaaaaa123 
* alson041 
* gracenotes 

## Credits (DSMapStudio)
* Katalash
* philiquaz
* george
* thefifthmatt
* TKGP
* Nordgaren
* [Pav](https://github.com/JohrnaJohrna)
* [Meowmaritus](https://github.com/meowmaritus)
* [PredatorCZ](https://github.com/PredatorCZ)
* [Horkrux](https://github.com/horkrux)

# Libraries
* [SoulsFormats](https://github.com/JKAnderson/SoulsFormats) - Credit to TKGP
* [SoapstoneLib](https://github.com/soulsmods/SoapstoneLib) - Credit to gracenotes
* [HKLib](https://github.com/The12thAvenger/HKLib) - Credit to The12thAvenger
* [Veldrid](https://github.com/veldrid/veldrid)
