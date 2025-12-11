# Godot Steam Audio .Net

[[Donate](ko-fi.com/johndaniel256)] [[Example project repo](https://github.com/JamDoggie/godot-steamaudio-dotnet-example)] [[Steam Audio repo](https://github.com/ValveSoftware/steam-audio)] [[utopia-rise FMOD Fork repo](https://github.com/JamDoggie/fmod-gdextension-steamaudio)] [[Original utopia-rise FMOD repo](https://github.com/utopia-rise/fmod-gdextension)]

This is a rough implemention of Valve's Steam Audio library using P/Invoke in C#. **This addon is currently only intended to serve as a bridge between Steam Audio and FMOD. If you are not using FMOD, this will be useless to you.** 

With that being said, you will need to clone and compile this repository to work: https://github.com/JamDoggie/fmod-gdextension-steamaudio

Unfortunately, a small modification was necessary in the utopia-rise extension in order to pass along FMOD event pointers to C# land. I would definitely like to find a better way to do this in the future.

## Example Project

This repository contains an example project with everything you need to figure out how this works. Here's a demo video of what it looks like:

[https://www.youtube.com/watch?v=u51iJlPK1KE](https://www.youtube.com/watch?v=u51iJlPK1KE)
[![Demonstration of the plugin](https://i.gyazo.com/880fc48094e9d0812eec7ee32acd803b.png)](https://www.youtube.com/watch?v=u51iJlPK1KE)

## Usage
You'll need to grab both the Steam Audio library itself, and the Steam Audio fmod plugin from [Valve's website](https://valvesoftware.github.io/steam-audio/downloads.html).
Put the following files in the following directories:

- phonon.dll & its sibling files (GpuUtilities.dll, etc.) in the root of your godot project
- The steam audio fmod files from the zip into your fmod project's Plugins folder, so it looks like the following: Plugins/steamaudio_fmod/lib/(platform, ex windows-x64)/phonon_fmod.dll
- You'll need to also point the FMOD integration in Godot to the plugin file. In the example project, it expects it at fmodproject/plugins/windows/phonon_fmod.dll

## Implementation
Below is a list of all the Steam Audio features that this plugin implements:
- Reflections
- Occlusion
- Pathing (requires a SteamAudioBaker, otherwise you should leave this disabled)
- Radeon Rays
- True Audio Next (Theoretically, although I realized that this is an AMD GPU only feature so I literally can't test it)
- Baking (with some limitations, will explain further below)
- Materials
- Static Geometry

## Limitations
- **There is currently no support for Godot's built in audio system.** I simply did not build this implementation for this as I didn't need it.
 If you are interested in this and don't care about FMOD, I recommend checking out this repository: https://github.com/stechyo/godot-steam-audio
- The only types of baked data that are really supported are static audio sources, and pathing. Reverb technically is baked, although there isn't really a way to use it right now.
  Steam audio also supports baking static listeners, so that's another thing that isn't implemented here.
- Dynamic geometry is not implemented yet. I plan on implementing this soon as I need it for my own project.

## Pain points
Because I built this not knowing if I could feasibly get a working implementation at all, there are a few things I'd like to refactor. There's also some stuff that just kinda sucks.

### Initialization
You're going to need to add the fmod plugin to both a place where FMOD Studio can see it, and also to the Godot FMOD plugin. 
There is documentation on adding FMOD plugins in the utopia-rise plugin repository. You can find the Steam Audio FMOD plugin (phonon_fmod.dll) here: [https://valvesoftware.github.io/steam-audio/downloads.html](https://valvesoftware.github.io/steam-audio/downloads.html)

### Main nodes
It's a little awkward, but you need to place two Nodes in your main scene. FmodSteamAudioBridge, which is the main "server" for Steam Audio, then another node that uses the fmod_gdscript_bridge.gd script. 
This is because C# needs to be able to access some methods from the utopia-rise FMOD plugin. 

### Polish
This plugin is developed primarily for the game I am currently working on. Because of this, some nodes don't have icons, and the ones that do are pretty much all placeholders. If you are interested in polishing the User Experience of 
this plugin, feel free to make a pull request.

### Materials
The material system actually isn't horrible. You simply attach a SteamAudioMaterialScript to any StandardMaterial3D or other Material node, and you can create Steam Audio material definitions that will be loaded by SteamAudioStaticGeometry.
 That being said though, I still feel this system could still be improved and working with it still isn't 100% amazing. There are some example material definitions on [this page](https://valvesoftware.github.io/steam-audio/doc/capi/scene.html#_CPPv411IPLMaterial) of the Steam Audio docs.

### Probes
To bake audio data, you first need to generate probes with the "Gen Probes" button. There are a few options to choose from for automatic probe generation in Steam Audio by default, but the only really useful one is Uniform Floor. 
In the future, I would like to implement a way to manually place probes in the editor.

## Static geometry
To send your level geometry to Steam Audio, you'll need a SteamAudioStaticGeometry node. In the "Mesh Instances" array, you can either directly link MeshInstance3Ds, or you can link to a node that has MeshInstance3D children. 
This is useful if you just want to point it to an imported GLTF. Just make sure your geometry is relatively simple, as polygons increase CPU load and baking times.

## Baking
To bake static audio sources, you first need to add a SteamAudioStaticSourceMarker node as a child of any FmodEventEmitter you want to be static. It's important to note that these marker nodes need to be in the exact same position that your sound is going to play, 
or the sounds will simply fall back to realtime calculation.

In the end, your node tree may look something like this:

![An example node tree while using this plugin](https://i.imgur.com/IDr7AnD.png)
