# Beat the Heat: Market Seller

A virtual reality market-stall game for the Meta Quest 2. You run a fruit stall on a hot street. Customers walk up with an order, you pick the items off the shelf, pack them in a paper bag and hand the bag over before the customer loses patience. The heat keeps climbing, so you also have to pull the umbrella rope to keep the stall shaded.

Built for the AR/VR Project Vertical Slice (ALU, AR/VR Development with Unity II).

## Tech

- Unity 6000.4.6f1 (Universal Render Pipeline)
- XR Interaction Toolkit 3.4.1, OpenXR
- Target: Meta Quest 2, Android, IL2CPP, ARM64

## How to run

### On a Quest 2

1. Install Unity 6000.4.6f1 with the Android Build Support module (OpenJDK and Android SDK/NDK included).
2. Clone this repository and open the folder in Unity Hub.
3. Open `Assets/Scenes/Market.unity`.
4. In `File > Build Profiles`, select Android. The scene list should contain `Scenes/Market`.
5. Put the headset in developer mode, connect it by USB and accept the USB debugging prompt.
6. Choose your headset as the Run Device and click **Build And Run**.

### In the Unity editor

Press Play with the XR Interaction Simulator or a Link-connected headset. A desktop test camera is included for quick checks without a headset (`Assets/Scripts/Core/DesktopTestCamera.cs`).

## How to play

- **Menu:** point at a button and press the trigger. Press the left Menu button (or Y) at any time to pause.
- **Grab:** hold the grip on a fruit on the shelf behind you and drop it into the paper bag on the counter.
- **Serve:** pick up the bag and hold it against the customer's body to hand over the order. Wrong or missing items cost you; a complete order earns coins.
- **Shade:** grab the umbrella rope and pull it down to open the umbrella. The stall cools while it is open.
- **Goal:** reach the coin target for the day before time runs out. Perfect orders in a row build a streak bonus. Three days, getting harder each time.

## Project structure

```
Assets/
  Scenes/Market.unity        main scene
  Scripts/
    Core/                    game state, score, heat, settings, audio helpers
    Customers/               customer behaviour, spawning, order handling
    Items/                   shelf items, bag, umbrella rope
    People/                  pedestrians, animation, dialogue playback
    UI/                      HUD, order bubbles, tutorial, menus
    Editor/                  scene and material setup tools
```

## Design notes

- **State machine:** `GameManager` owns the game state (menu, tutorial, playing, results) and other systems react to its events.
- **Singletons for services:** score, heat, feedback and the bag are single instances used across the scene.
- **Event driven UI:** the HUD and menus subscribe to score and state events instead of polling.
- **Settings:** volume, player height and difficulty are saved with PlayerPrefs.
- **Menus** are built at runtime from one layout helper, so every panel shares the same sizes, spacing and text scale.

## Performance

Settings used for the Quest 2 target: IL2CPP scripting backend, ARM64 only, GPU skinning for the animated characters, engine code stripping, URP, mipmaps on imported textures and lightweight unlit shaders for the menus. Benchmark figures and profiler screenshots are in the Game Design Document.

## Assets

Fruit, bread and ground materials are from Poly Haven (CC0). City buildings are from SimplePoly City. Characters and animations are from Mixamo. Customer voices were generated with Piper text-to-speech. The same credits appear in the in-game Credits screen.
