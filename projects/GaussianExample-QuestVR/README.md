# GaussianExample-QuestVR

Unity 6 project skeleton for rendering 3D Gaussian Splats on **Meta Quest 3**
(Android, Vulkan, stereo multiview, controller navigation).

Full plan and background: [`../../3DGSQuestAndroidVulkan.md`](../../3DGSQuestAndroidVulkan.md)

## Quick setup checklist

1. Open with **Unity 6000.4.4f1** (Android Build Support module installed).
   If package resolution fails, install *OpenXR Plugin* / *Input System* via
   Package Manager at their recommended versions.
2. Run **Tools > Gaussian Splats > Apply Quest Android Settings**, then switch
   platform to **Android** (File > Build Profiles).
3. **XR Plug-in Management** (Android tab): enable **OpenXR**; in OpenXR settings set
   **Render Mode: Single Pass Instanced \ Multi-view**, add the **Oculus Touch
   Controller Profile**, enable the **Meta Quest Support** feature group.
4. Create a **URP Asset (with Universal Renderer)**; on its Renderer Data add the
   **Gaussian Splat URP Feature**; set **MSAA: Disabled**, HDR off,
   Intermediate Texture: **Always**. Assign it in Project Settings > Graphics
   (Render Graph Compatibility Mode must stay OFF).
5. Scene: `XR Rig` (root, with `VRFlyController`) → child `Main Camera`
   (tag MainCamera, **Tracked Pose Driver (Input System)**, Center Eye).
   Assign the camera to the controller's *Head* field.
6. Add a GO with **GaussianSplatRenderer** + your splat asset
   (try rotation (-160, 0, 0), scale (1, 1, -1)). Set **SH Order 0**.
7. Splat asset: **≤ 300K splats** (reduce the PLY in [SuperSplat](https://superspl.at/editor)),
   created via *Tools > Gaussian Splats > Create GaussianSplatAsset* with quality
   **Low or Medium — never "Very Low"** (BC7 is unsupported on Quest).
8. **Build And Run** on the Quest (Developer Mode enabled, USB debugging allowed).

## Controls (VRFlyController)

| Input | Action |
|---|---|
| Left stick | Fly forward/back + strafe (gaze-relative) |
| Right stick X | Smooth turn (set `snapTurnDegrees` > 0 for snap turn) |
| Right stick Y | Move up / down |
| Left grip (hold) | Sprint |
| Right A button | Reset to start pose |
