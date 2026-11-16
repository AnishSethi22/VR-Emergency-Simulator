# Project Presentation Guide
## VR Emergency Rescue Simulator – Firefighter Training Experience

---

### 1. Problem Statement
Real-life firefighter and emergency rescue training inside burning buildings is expensive, logistically complex, and exposes trainees to severe physical hazards (toxic smoke, extreme heat, structural disorientation). Traditional classroom or 2D video training lacks spatial awareness, hands-on tool operation, and real-time decision-making under pressure.

### 2. Project Objective
Develop an interactive, physics-driven **VR Emergency Rescue Simulator** in Unity 6 LTS that trains users in fire suppression, civilian evacuation, and hazard survival inside a burning office building—with dual **VR (OpenXR)** and **Desktop** support so it can be demonstrated and tested on any hardware.

### 3. Technologies Used
- **Engine**: Unity 6 LTS (`6000.0.85f1`) with C#
- **Rendering**: Universal Render Pipeline (URP) — real-time point/spot lighting, flickering emergency lights, exponential smoke fog, and GPU-friendly particle systems.
- **VR Stack**: OpenXR + XR Interaction Toolkit (`XROrigin`, `XRDirectInteractor`, `XRRayInteractor`, `XRGrabInteractable`, `ContinuousMoveProvider`, `SnapTurnProvider`).
- **Physics**: Unity PhysX (`Rigidbody`, `CapsuleCollider`, `BoxCollider`, `SphereCollider` triggers, `SphereCastAll` spray collision).
- **AI Navigation**: Unity AI Navigation (`NavMeshSurface` + `NavMeshAgent`) for autonomous civilian evacuation.
- **UI & Audio**: TextMeshPro HUD/Menus + procedural spatialized 3D audio (`AudioManager` & `ProceduralAudioLibrary`).

### 4. VR Interaction Techniques
- **Direct Grabbing (`XRDirectInteractor`)**: Natural hand-to-object pickup of fire extinguishers and debris crates.
- **Ray Interaction (`XRRayInteractor`)**: Distant aiming and UI/door interaction.
- **Trigger Activation**: Pulling the VR controller trigger while holding an extinguisher activates the pressurized foam stream (`XRGrabInteractable.activated`).
- **VR Comfort**: Configurable movement speed and optional 45° Snap Turning (`SnapTurnProvider`) to minimize motion sickness.

### 5. Physics Implementation
- **Extinguisher & Debris**: Dynamic `Rigidbody` components with continuous collision detection so dropped extinguishers and pushed crates behave naturally.
- **Physical Spray Detection**: `FireExtinguisher` performs `SphereCastAll` cone checks combined with line-of-sight wall raycasts and particle world-collision callbacks (`OnParticleCollision`)—ensuring sprays are physically blocked by walls and require aiming at the fire base.

### 6. Fire Simulation
- **`FireController.cs`**: Tracks fire intensity (`0–100`), dynamically scales flame, smoke, and spark particle emission rates, modulates flickering orange/red `PointLight` intensity, and applies distance-weighted heat damage (`OnTriggerStay`).
- **`FireSpreadSystem.cs`**: Periodically checks active fires and ignites nearby unobstructed dormant fire nodes within `SpreadRadius`.

### 7. NPC Rescue System
- **`NPCRescueController.cs`**: Implements a 3-state machine (`TrappedInjured` -> `Evacuating` -> `EvacuatedSafe`).
- Upon rescue interaction, the NPC switches from an injured coughing/waving procedural animation to `NavMeshAgent` pathfinding, navigating around office furniture and through doorways to the exterior **Safe Evacuation Zone** without disappearing.

### 8. Real-World Applications
- Fire department & industrial safety drills.
- Corporate office & university building evacuation familiarization.
- High-risk hazard awareness training without physical danger or material waste.

### 9. Future Enhancements
- Multi-floor building layouts with elevators/stairwells and structural collapse events.
- Multiplayer co-op firefighter teams over Netcode for GameObjects.
- Haptic vest / controller force-feedback integration for heat proximity and hose recoil.
- Voice command support for directing trapped civilians.
