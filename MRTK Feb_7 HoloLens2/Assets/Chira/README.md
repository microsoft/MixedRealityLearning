# Introduction
Welcome to the Unity sample for fully articulated hands, codenamed "Chira" (kai - ra).

This sample currently uses a temporary internal API called PixieLib, which it loads as a plugin. Eventually this will be replaced by a public-facing Windows API. 

The sample here allows you to test hand tracking in editor without needing build for HoloLens. Press space to simulate the hand. Please see section below for more.

# Building a new scene
To add Chira hand tracking in your scene, do the following:

1. Make sure your Unity version is at least 2018.9.1f1
1. Drag "Chira Hand API With Debugging Info" prefab into your scene. This will add the data provider for running on HoloLens, as well as hand data visualizers and debug statistics text
2. If you don't want the hand visualizations, drag in "Chira Hand API"
3. Compile your UWP as normal, (Build->Player Settings, make sure platform is UWP, you can use either .NET or IL2CPP backend)
4. When the app compiles to UWP, ignore errors starting with "Reference rewriter", these are the errors generated as part of the Unity bug tracked at MSFT:16730174.

### Controls for mouse/keyboard simulation of Chira Data in Unity
Press spacebar to turn right hand on/off
Press ctrl + spacebar to turn left hand on/off
Press Q/E to rotate hand about Y axis
Press R/F to rotate hand about X axis
Preff Z/X to rotate hand about Z axis
Left mouse button brings index and thumb together
Mouse moves left and right hand together.
Scroll to move hands in Z.

### Other keybindings
H - Move between different hand views (joints, hand mesh, nothing)
