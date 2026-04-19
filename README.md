# VR Shooting Range | Unity & OpenXR

A performance-oriented Virtual Reality simulation focused on realistic weapon mechanics, physical interactions, and optimized spatial environments. Built using Unity's XR Interaction Toolkit and C#.

## Technical Highlights

The project serves as a showcase of high-performance VR systems, where implemented the following solutions:

* **Advanced Physics-Based Ballistics:**
    * **Rigidbody Projectile Simulation:** Unlike standard raycasting, this system utilizes physical projectiles to accurately calculate complex trajectories, including **bullet ricochets** and **material penetration** logic.
    * **Optimization:** Fine-tuned collision detection and life-cycle management to maintain high performance despite the overhead of active rigidbodies.
* **XR Interaction Architecture:**
    * Custom implementation of grab mechanics and socket interactors for realistic weapon handling.
    * Physics-based hand-object interaction ensuring tactile feedback and high immersion.
* **Unity 6 Rendering & VRAM Optimization:**
    * **Single Pass Instanced Rendering:** Optimized pipeline to reduce draw calls by 50%, critical for stable VR frame rates.
    * **GPU Resident Drawer:** Leveraging Unity 6's newest features to minimize CPU overhead in complex environments.
    * **Baked Lighting & Static Batching:** Aggressive optimization to keep CPU/GPU usage minimal.
* **State-Driven Weapon Logic:** Modular weapon states (Safe, Semi, Auto, Reloading) handled via a robust, decoupled C# state machine.

## Tech Stack & Tools
* **Engine:** Unity 6 (6000.x)
* **SDK:** OpenXR / XR Interaction Toolkit (XRI)
* **Language:** C# (Advanced OOP, Events-driven architecture)

## Key Features
* **Realistic Handling:** Manual magazine reloading, slide/bolt manipulations, and haptic feedback integration.
* **Procedural Target System:** Dynamic spawning of targets based on a custom "Wave Manager" logic.
* **Physics-Based Movement:** Smooth locomotion combined with snap-turning to ensure accessibility and comfort.

---
**Author:** KacperZ402
**Target Platforms:** Meta Quest / SteamVR
