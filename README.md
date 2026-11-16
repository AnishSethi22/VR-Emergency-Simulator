# VR Emergency Rescue Simulator – Firefighter Training Experience

A complete, physics-driven 3D & Virtual Reality emergency rescue simulator built with **Unity 6 LTS (`6000.0.85f1`)**, **Universal Render Pipeline (URP)**, **XR Interaction Toolkit 3.7.0**, **OpenXR 1.19.0**, **Unity Input System**, **AI Navigation (NavMesh)**, and **TextMeshPro**.

---

## 1. Dual Playable Modes

The project supports two full gameplay modes without requiring VR hardware for testing:
1. **Desktop Mode** (First-Person Keyboard & Mouse)
2. **VR Mode** (OpenXR Headset Tracking, VR Controllers, Direct Grabbing, Ray Interaction, Locomotion)

You can switch modes directly from the **Main Menu** (`LAUNCH IN DESKTOP MODE` or `LAUNCH IN VR MODE (OpenXR)`).

---

## 2. Controls Documentation

### Desktop Mode
| Input | Action |
|---|---|
| `W A S D` | Move |
| `Left Shift` | Sprint |
| `Mouse` | Look around |
| `E` | Interact (Pick up Fire Extinguisher / Crate, Open/Close Door, Rescue Civilian, Refill Extinguisher) |
| `Left Mouse Button` (Hold) | Spray Fire Extinguisher |
| `G` or `Q` | Drop / Toss held object |
| `Esc` | Pause / Resume |

### VR Mode (OpenXR Controllers)
| Input | Action |
|---|---|
| Left Thumbstick | Continuous Movement |
| Right Thumbstick | Snap Turn (45° default, configurable in Settings) or Smooth Turn |
| Grip Button | Grab Fire Extinguisher / Physics Objects (`XRDirectInteractor` / `XRRayInteractor`) |
| Trigger Button | Spray Fire Extinguisher / Interact |

---

## 3. Mission Structure

1. **MISSION 1 – FIRE CONTROL**:
   - Enter the burning office building through the Main Entrance.
   - Pick up the wall/lobby **Fire Extinguisher**.
   - Extinguish the **Lobby Fire** using the physical spray stream.
   - Advance to the **Office Hallway Checkpoint**.
2. **MISSION 2 – HUMAN RESCUE**:
   - Locate the two trapped civilians (**Alex** in Office A, **Maya** in Office B).
   - Interact (`E` / VR Trigger) to rescue them.
   - Civilians automatically navigate via **NavMesh** around furniture and doorways to the exterior **Safe Evacuation Zone**.
3. **MISSION 3 – FINAL ESCAPE**:
   - Navigate through the **Smoke-Filled Corridor** (watch your Oxygen meter!).
   - Avoid remaining fire hazards and push/move debris crates if needed.
   - Reach the **Emergency Exit** before oxygen or the 5-minute countdown runs out.

---

## 4. Setup & Running in Unity

1. Open **Unity Hub** and open the project folder (`Arvr project`) using **Unity 6000.0.85f1 (Unity 6 LTS)**.
2. All required packages—including `com.unity.xr.interaction.toolkit`, `com.unity.xr.openxr`, and `com.unity.xr.management`—are pre-configured and bundled locally in `Packages/LocalTarballs/` so the project opens reliably even offline.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play** in the Unity Editor.
4. To rebuild or regenerate scenes/prefabs at any time, use the Unity Editor menu:
   `RescueSim -> Build Complete Project`

---

## 5. Connecting a VR Headset (OpenXR)

1. Ensure your VR headset runtime is installed and active:
   - **Meta Quest (Link / Air Link)**: Open the Meta Quest Link PC app and connect your headset; set Meta Quest Link as the active OpenXR runtime.
   - **SteamVR / Valve Index / HTC Vive**: Launch SteamVR and ensure SteamVR is set as the active OpenXR runtime in SteamVR Developer Settings.
2. In Unity Editor, open **Edit -> Project Settings -> XR Plug-in Management** and enable **OpenXR** under the **Windows, Mac, Linux** tab (and add your controller interaction profile, e.g., *Oculus Touch Controller Profile* or *Valve Index Controller Profile*).
3. Press **Play** and select **LAUNCH IN VR MODE (OpenXR)** in the Main Menu.

---

## 6. Packages & External Dependencies

No paid assets or Asset Store downloads are required. All 3D props, materials, particle effects, and sound effects are built natively or synthesized procedurally.

| Package | Version | Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | `17.0.4` | URP Lighting, Fog, Shadows, Particles |
| `com.unity.xr.interaction.toolkit` | `3.7.0` | VR Grabbing, Direct/Ray Interactors, Locomotion |
| `com.unity.xr.openxr` | `1.19.0` | Cross-vendor VR Headset & Controller Support |
| `com.unity.xr.management` | `4.6.1` | XR Loader Management |
| `com.unity.ai.navigation` | `2.0.15` | NavMesh Baking & NPC Pathfinding |
| `com.unity.inputsystem` | `1.20.0` | Desktop & VR Input Handling |
| `com.unity.ugui` (TextMeshPro) | `2.0.0` | Modern HUD, Menus, & World-Space Labels |
