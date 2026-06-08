# Sharpfall
Sharpfall is a fast remake of the [Pianofall](https://github.com/ste-art/Pianofall) physics MIDI player by [ste-art](https://github.com/ste-art) for playing larger MIDIs with higher block counts.

## How to use
The current version of Sharpfall requires [OmniMIDI](https://github.com/KeppySoftware/OmniMIDI) to function, ensure it is installed.<br>
After that you should be able to download and run a version in [Releases](https://github.com/EmK530/Sharpfall/releases) just like normal!

### What version do I pick?
There are two versions of v3.0.0, a standard and a "nocuda" version. No CUDA is smaller in size but won't support CUDA acceleration for PhysX.<br>This does not matter if you intend to run prerenders as it is not recommended to use GPU acceleration with it.

## libSharpfall
Sharpfall v3.0.0 offloads MIDI playback and physics management onto a native library called libSharpfall in the Plugins folder.<br>
This library was compiled with Visual Studio 2022 and is managed under another repository:<br>
-> https://github.com/EmK530/libSharpfall <-
