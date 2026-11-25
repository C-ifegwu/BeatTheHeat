# Beat the Heat: Market Seller

A fast-paced VR market stall simulation developed for the Meta Quest 2 using Unity 6 (URP) and the XR Interaction Toolkit.

You play as a street vendor running a fruit and grocery stall under a blistering hot sun. Customers approach your stand with specific orders that you have to grab from the shelves, pack into paper grocery bags, and hand over before they run out of patience. Meanwhile, the temperature is constantly rising—if you don't periodically pull down the overhead umbrella rope to shade your stall, your goods spoil and heat levels spike.

Developed as a project vertical slice for the *AR/VR Development with Unity II* course at ALU.

---

## Gameplay & Controls

### Controls (Oculus Touch)
- **Grip (Hold):** Pick up fruit and wares from shelves, hold the paper bag, or pull down the umbrella rope.
- **Trigger:** Click buttons and navigate VR menus.
- **Left Menu Button / Y:** Pause game and toggle settings.

### Core Mechanics
- **Fulfill Orders:** Customers arrive at the counter with visual order bubbles. Grab the requested items from your shelves, place them inside the paper bag, and bring the bag up to the customer to complete the sale.
- **Manage the Heat:** Keep an eye on your heat gauge. Grabbing the umbrella rope and pulling it down opens the canopy, cooling the stall down and protecting produce from spoiling under direct sunlight.
- **Day Progression:** Clear daily coin quotas across three increasingly demanding days. Fulfilling orders quickly and without errors builds up combo streak bonuses.

---

## Setup & Running the Project

### Requirements
- **Unity Version:** `6000.4.6f1` (Universal Render Pipeline)
- **Build Module:** Android Build Support (including Android SDK & NDK tools + OpenJDK)
- **Target Device:** Meta Quest 2 (Standalone APK or Quest Link / Air Link)

### Deploying to Quest 2
1. Open the project folder in Unity Hub with version `6000.4.6f1`.
2. Load the main scene at `Assets/Scenes/Market.unity`.
3. Open **File > Build Profiles**, select **Android**, and ensure `Scenes/Market` is checked in the build list.
4. Enable Developer Mode on your headset and connect it via USB (allow USB debugging in the headset prompt).
5. Select your Quest from the **Run Device** dropdown and click **Build And Run**.

### Editor Testing
You can test directly in the Unity Editor using either:
- **Quest Link / Air Link** for full headset preview.
- **XR Interaction Simulator** to emulate controller poses with mouse and keyboard.
- **Desktop Debug Camera** (`Assets/Scripts/Core/DesktopTestCamera.cs`) for quick scene checks without entering VR.

---

## Project Structure & Architecture

```
Assets/
├── Scenes/
│   └── Market.unity          # Main playable VR scene
├── Scripts/
│   ├── Core/                 # Game loop, heat simulation, scoring, audio, settings
│   ├── Customers/            # Customer ordering behavior, queues, order handover
│   ├── Items/                # Shelf items, spoilage, bag packing, umbrella rope
│   ├── People/               # Pedestrian navigation, animation controllers, speech
│   ├── UI/                   # World-space order bubbles, follow-HUD, VR menus
│   └── Editor/               # Scene assembly tools and Mixamo import helpers
└── Resources/
    └── Voice/                # Customer dialogue and order audio clips
```

### Key Systems
- **Game State & Flow (`GameManager.cs`):** Coordinates progression across menus, tutorial steps, active gameplay, and end-of-day scoring screens.
- **Temperature & Shade (`HeatManager.cs`):** Simulates ambient temperature climb and checks whether the stall is protected by the pulled umbrella canopy.
- **Customer AI (`Customer.cs`, `CustomerSpawner.cs`):** Manages customer lifecycle, order generation, patience countdowns, voice feedback, and item verification upon bag handover.
- **Physical Interactions (`UmbrellaRope.cs`, `Bag.cs`, `StallItem.cs`):** Handles physics-driven rope pulling, multi-item bag packing triggers, and item heat vulnerability.
- **Dynamic Menus (`GameMenus.cs`, `FollowHud.cs`):** Renders clean world-space VR UI and a follow-HUD with heat and score metrics that stay comfortably in the player's field of view.

---

## Performance & Optimization (Quest 2 Target)

Designed to maintain a steady 72 FPS on standalone mobile VR hardware:
- **Scripting Backend:** IL2CPP targeting ARM64.
- **Rendering:** Universal Render Pipeline with Single Pass Instanced (Multiview) rendering to halve draw call overhead.
- **Character Meshes:** GPU skinning enabled for all animated pedestrian and customer models.
- **Texture Budgets:** Clamped to 1024x1024 with generated mipmaps to fit within Quest memory bandwidth.
- **UI & Canvases:** Lightweight unlit shaders used for world-space panels to avoid unnecessary dynamic lighting passes.

---

## Third-Party Credits & Acknowledgments

- **Props & Environment:** Low-poly city environment by *SimplePoly City*; market furniture and stall props from *WoodInterior*.
- **Items & Textures:** Produce 3D models and PBR surface textures from *Poly Haven* (CC0); market bottles and goods from *RPG Pack*.
- **Characters & Rigs:** 3D character models and humanoid animation clips sourced from *Mixamo*.
- **Voice Lines:** Synthesized customer dialogue audio generated with *Piper TTS*.
