# VR Emergency Rescue Simulator – Comprehensive Defense & Viva Guide

A university-grade presentation and project documentation dossier for **VR Emergency Rescue Simulator – Firefighter Training Experience**, engineered in **Unity 6 LTS (6000.0.85f1)**.

---

## 1. Executive Summary & Objective

### 1.1 Problem Statement
Real-world firefighter and hazard rescue training is high-cost, logistically hazardous, and difficult to repeat safely. Traditional drills expose trainees to toxic combustion byproducts (CO, cyanides), flashover dangers, and structural disorientation, while standard 2D instructional video lacks spatial awareness, adrenaline management, and physical interaction realism.

### 1.2 Project Objective
To engineer an interactive, physics-driven, university-standard **3D & VR Emergency Rescue Simulator** that trains users in:
- Fire behavior recognition and extinguisher operation.
- Hazard mitigation under depleting vital supplies (Health and Oxygen depletion curves).
- Search and rescue traversal for trapped civilian NPCs.
- Safe zone evacuation and architectural escape routing under smoke occlusion.
- Dual hardware capability: Native support for **OpenXR VR headsets** and **Desktop First-Person (Keyboard & Mouse)** so it can be demonstrated without VR hardware dependencies.

---

## 2. Technical Stack & Architecture

| Layer | Technology | Version | Purpose |
|---|---|---|---|
| **Core Game Engine** | Unity 3D (C# .NET) | `6000.0.85f1` (Unity 6 LTS) | Real-time simulation, physics, scenegraph |
| **Render Pipeline** | Universal Render Pipeline (URP) | `17.0.4` | Optimized forward rendering, dynamic point lights, exponential smoke fog, GPU particle unlit shaders |
| **VR Framework** | OpenXR Plugin + XR Interaction Toolkit (XIT) | `1.19.0` / `3.7.0` | Open standard cross-platform VR runtime, `XROrigin`, `XRDirectInteractor`, `XRRayInteractor`, `XRGrabInteractable` |
| **Input Architecture** | Unity Input System | `1.20.0` | Action-based input mapping for mouse delta clamping, keyboard smoothing, and VR controller axis/buttons |
| **Pathfinding & AI** | Unity AI Navigation | `2.0.15` | Runtime/Bake `NavMeshSurface`, dynamic polygonal traversal, `NavMeshAgent` obstacle avoidance |
| **Audio Simulation** | Unity Audio + Procedural Synthesizer | Built-in Engine | Procedural runtime wave generation (no external audio assets needed), 3D spatialized logarithmic attenuation |
| **User Interface** | TextMeshPro (uGUI) | `2.0.0` | SDF vector typography, dynamic color-coded objectives, live room floor minimap, damage vignettes |

---

## 3. Core Systems & Technical Implementation

### 3.1 Physics & Fire Extinguishing Mechanics
Unlike basic games where clicking a button simply destroys the fire GameObject, this project implements a **physically verified extinguishing cone**:
- **Raycasting & SphereCasting (`FireExtinguisher.cs`)**:
  When the player sprays, the extinguisher triggers a `Physics.SphereCastAll` over a 6.5 meter forward vector with a 0.55 meter radius.
- **Line-of-Sight Occlusion Check**:
  A secondary `Physics.Raycast` verifies that no solid architectural collider (wall, closed door) obstructs the trajectory between the nozzle and the fire source.
- **Particle World Collision**:
  Spray foam particles are configured with `ParticleSystemCollisionType.World` and send trigger messages via `OnParticleCollision` into `FireController`.
- **Dynamic Fire Attenuation (`FireController.cs`)**:
  Fire intensity decreases dynamically (42 units/sec scaled by distance attenuation):
  $$\text{Intensity}_{\text{new}} = \max\left(0, \text{Intensity} - \text{ExtinguishRate} \times \text{DistanceFactor} \times \Delta t\right)$$
  As intensity drops, particle emission rates, particle sizes, light radius, and spatial audio crackle volume scale down down to 0, at which point the light and audio extinguish.

### 3.2 Fire Propagation Model (`FireSpreadSystem.cs`)
- Fires periodically evaluate spatial neighbors within a `spreadRadius` (4.5 meters).
- If an active fire has an intensity greater than 45% and an unlit fire node lies within line of sight (unobstructed by walls), a stochastic check triggers an ignition event (`Ignite(55f)`), demonstrating real-world thermal propagation.

### 3.3 Player Survival: Health & Oxygen Dynamics
The player's vital systems calculate differential decay equations:
- **Oxygen Consumption (`OxygenSystem.cs`)**:
  - Baseline metabolic consumption: 0.45% / sec.
  - Inside smoke hazard volumes (`SmokeZone.cs` trigger): 4.50% / sec (10x acceleration).
  - Inside Exterior Safe Zone: Regenerates at +12.0% / sec.
  - When Oxygen <= 0%, the player begins taking 8.0 HP / sec suffocation damage.
- **Thermal Heat Damage (`HealthSystem.cs`)**:
  Within the thermal sphere collider of active fires:
  $$\text{DPS} = \text{DamageValue} \times \left(\frac{\text{Intensity}}{\text{MaxIntensity}}\right) \times \max\left(0.25, 1 - \frac{\text{Distance}}{\text{Radius}}\right)$$

### 3.4 Autonomous Civilian AI & NavMesh Pathfinding
- Trapped civilians (**Alex** in Office A, **Maya** in Office B) use a finite state machine:
  $$\text{TrappedInjured} \longrightarrow \text{Evacuating} \longrightarrow \text{EvacuatedSafe}$$
- While trapped, procedural skeletal kinematics simulate an injured coughing motion and a waving arm for help.
- Upon player rescue interaction (`IInteractable.Interact()`), the NPC switches to `NavMeshAgent`, sets its destination to the exterior `SafeEvacuationZone`, and autonomously routes around desks, chairs, and doors.
- When reaching the safe zone perimeter, they enter the `EvacuatedSafe` state and update the mission counter without vanishing.

### 3.5 Dual Input & Camera Smoothing (`PlayerController.cs`)
To resolve common first-person camera stutter and jarring mouse movement:
- **Frame Spike Clamping**: Raw mouse delta is clamped to prevent massive teleport turns during OS focus shifts.
- **Exponential Angle Smoothing**:
  $$\text{CurrentAngle} = \text{Lerp}\left(\text{CurrentAngle}, \text{TargetAngle}, 1 - e^{-\text{SmoothSpeed} \times \Delta t}\right)$$
- **Automatic Cursor Management**: The mouse locks into `CursorLockMode.Locked` automatically upon game start, and clicking anywhere in the viewport re-locks it seamlessly.

---

## 4. How the Environment was Engineered

```
+-------------------------------------------------------------------------+
|                         EXTERIOR PLAZA (Z = -10 to -3)                  |
|   [ SAFE EVACUATION ZONE ]                       [ PLAYER SPAWN ]       |
|    (Green Ground + Fresh Air)                     (Facing Entrance)     |
+------------------------------[ MAIN ENTRANCE ]--------------------------+
|                                                                         |
|                          MAIN LOBBY (Z = -3 to 5)                       |
|   [Reception Desk & Chair]                      [Wall Extinguisher]     |
|   [Refill Station]                              [LOBBY FIRE - Mission 1]|
+---------------------------------[ LOBBY DOOR ]--------------------------+
|                                                                         |
|                     OFFICE HALLWAY (Checkpoint 1, Z = 5)                |
|           /                                               \             |
|   [ OFFICE A (West) ]                             [ OFFICE B (East) ]   |
|   - Trapped NPC: Alex                             - Trapped NPC: Maya   |
|   - Office Desks, Chairs                          - Office Desks, Chairs|
|   - Office Fire Hazard                            - Office Fire Hazard  |
+-------------------------------------------------------------------------+
|                  SMOKE CORRIDOR (Z = 15 to 23, Smoke Hazards)           |
|   - Debris Crate Obstacles (Move / Push)                                |
|   - Spreading Fire Nodes & Heavy Smoke VFX                              |
|   - Continuous Oxygen Depletion Area                                    |
+-----------------------------[ EMERGENCY EXIT ]--------------------------+
|                  [ ILLUMINATED GREEN EMERGENCY EXIT SIGN ]              |
|                      (Mission 3 Victory Checkpoint)                     |
+-------------------------------------------------------------------------+
```

---

## 5. Script Directory & Module Breakdown

All source scripts reside in `Assets/Scripts/`:

1. **`Core/GameManager.cs`**: Singleton state machine governing active mode (`Desktop` vs `VR`), pause state, player comfort options, and scene reloading.
2. **`Player/PlayerController.cs`**: Desktop first-person controller with kinematic acceleration, clamped mouse look, and item manipulation.
3. **`Player/XRPlayerController.cs`**: VR rig controller handling OpenXR locomotion, turn snapping, and hand tracking.
4. **`Player/HealthSystem.cs` & `Player/OxygenSystem.cs`**: Biological survival monitors driving HUD meters and damage vignettes.
5. **`Player/PlayerModeManager.cs`**: Detects selected platform and enables either `DesktopPlayer` or `VR_XROrigin`.
6. **`Fire/FireController.cs`**: Component on fire hazards managing multi-tier particle systems, flickering light sources, and spray extinguishment.
7. **`Fire/FireExtinguisher.cs`**: Physics grabbable extinguisher with limited pressure tank, gauge needle animation, and spherecast spray hit detection.
8. **`Fire/FireSpreadSystem.cs`**: Global thermal propagation evaluator.
9. **`Interaction/GrabbableObject.cs`**: Unified physics pickup script disabling held colliders to prevent self-collision physics jitter.
10. **`Interaction/DoorController.cs`**: Smooth rotational hinge door supporting interact key `E` and VR raycast.
11. **`Interaction/EmergencyLightFlicker.cs`**: Klaxon flasher and alarm sound controller.
12. **`NPC/NPCRescueController.cs`**: Civilian state machine, procedural injured animations, and NavMesh agent rescue routing.
13. **`Missions/MissionManager.cs`**: 3-phase objective tracker, countdown timer, event listener, and victory/defeat evaluator.
14. **`Missions/ScoreManager.cs`**: End-game performance rating calculator ($S, A, B, C, F$).
15. **`UI/UIManager.cs`**: In-game HUD controller (vitals, timer, objectives, mission announcement popups, room minimap).
16. **`UI/MainMenuUI.cs`**: Main menu event controller with single-click game start and instruction cards.
17. **`Audio/ProceduralAudioLibrary.cs`**: Algorithmic sound synthesizer creating PCM wave samples (fire crackle, spray hiss, alarms, footsteps) at runtime.
18. **`Editor/RescueProjectBuilder.cs`**: Automation script that builds materials, prefabs, NavMesh, lighting, and scenes from code.

---

## 6. How to Explain this Project to Your Professor (Viva / Defense Guide)

### Key Talking Points (What Makes This Project High-Level):
1. **Industry-Standard Render Pipeline (URP)**:
   Explain that rather than using the legacy Built-in pipeline, you opted for URP, which uses single-pass forward rendering, custom particle shaders, and real-time point light attenuation for VR performance efficiency.
2. **Physical Simulation over Scripted Tricks**:
   Emphasize that spraying an extinguisher is **not** an animation or a timer. It uses real physics spherecasting, raycast line-of-sight obstruction checks, particle collision delegates, and math-based distance decay.
3. **Inclusive Hardware Architecture**:
   Explain that VR projects often cannot be graded if the faculty does not have an active VR headset connected. You designed a unified dual architecture: OpenXR VR tracking with controller direct grabbing, along with a desktop first-person controller.
4. **Autonomous AI Pathfinding**:
   Trapped NPCs do not disappear when rescued; they use `NavMeshAgent` to pathfind through doorways and around desks out to the exterior safe zone.
5. **Procedural Audio Synthesis**:
   Showcase that you built a mathematical procedural audio engine (`ProceduralAudioLibrary.cs`) that synthesizes White Noise, Low-Pass filtering, and sine oscillators into fire crackles, alarm tones, and extinguisher spray at runtime.

---

## 7. Anticipated Viva / Defense Questions & Model Answers

### Q1: Why did you choose OpenXR instead of Oculus/Meta or SteamVR SDK directly?
> **Answer:** *"OpenXR is the royalty-free, open standard developed by the Khronos Group. By using OpenXR and the XR Interaction Toolkit, the simulator is hardware-agnostic. It runs seamlessly on Meta Quest, Valve Index, HTC Vive, or Windows Mixed Reality without rewriting controller interaction code or locking the project to a single vendor."*

### Q2: How does the fire extinguisher detect whether the player hit the fire?
> **Answer:** *"We use a two-tiered verification system in `FireExtinguisher.cs`. First, a `Physics.SphereCastAll` casts a cone along the nozzle's forward vector. Second, a line-of-sight raycast confirms no solid wall or closed door occludes the target. Third, the extinguisher's foam particles use Unity's `OnParticleCollision` callback. When verified, `FireController.ApplyExtinguisherSpray()` gradually reduces fire intensity until it reaches zero."*

### Q3: How do the NPCs navigate to safety without getting stuck?
> **Answer:** *"The entire building geometry is baked using Unity's `NavMeshSurface`. Obstacles like desks and walls have static colliders that generate navigational holes. When the player triggers `Interact()`, the NPC activates its `NavMeshAgent` and sets its destination vector to the `SafeEvacuationZone` outside the building, allowing the agent to steer around corners and through open doors autonomously."*

### Q4: How is player survival calculated in smoke zones?
> **Answer:** *"We placed `SmokeZone` trigger volumes across corridors. When the player's collider enters, `OxygenSystem.cs` switches consumption from a nominal baseline (0.45%/sec) to an accelerated smoke decay rate (4.5%/sec). If oxygen reaches 0%, a suffocation damage tick deducts 8 HP/sec from `HealthSystem.cs`. Stepping into the exterior safe zone reverses this, regenerating oxygen at +12%/sec."*

### Q5: How is performance maintained for stable 60+ FPS in VR?
> **Answer:** *"We optimized render calls by using Universal Render Pipeline batching, using unlit particle materials for flame and smoke particles, avoiding unnecessary `Update()` loops, using trigger volumes instead of distance polling, and baking static mesh geometry for the NavMesh."*
