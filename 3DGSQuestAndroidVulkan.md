# 3D Gaussian Splatting on Oculus Quest 3 — Android / Vulkan / Stereo VR Plan

Goal: a standalone **.apk** for **Meta Quest 3** that renders a 3D Gaussian Splatting
dataset in **stereo VR** (single-pass multiview — both eyes in one draw call), with
**thumbstick controller navigation**, using the **Vulkan** graphics backend, built from
a new Unity 6 project in this repo.

---

## 1. Where we start (good news)

This fork (`becksteppo/UnityGaussianSplatting`, forked from `arghyasur1991`, upstream
`aras-p`) **already contains everything Quest-specific at the code level**:

- **Stereo rendering support** in the package (`package/Runtime/GaussianSplatRenderer.cs`,
  `package/Runtime/GaussianSplatURPFeature.cs`, shaders): detects
  `SinglePassInstanced` / `SinglePassMultiview` XR mode, computes per-eye view data in one
  compute dispatch, renders both eyes' splats in a single instanced draw
  (`instID * 2 + eyeIndex`), and composites into the XR `Tex2DArray` swapchain with a
  Quest 3 workaround (`_CustomStereoEyeIndex`, one composite blit per eye — this is the
  "one draw call per eye" part; the heavy splat draw itself is multiview, one call for both eyes).
- **Quest 3 performance optimizations** (commits `c957007`, `6f1d8bc`): adaptive sort,
  partial radix sort, contribution culling, SH LOD, reduced-resolution rendering,
  per-splat adaptive quad extent, packed 32B/splat view data.
- Proven numbers (fork readme): **~300K gaussians at 16–18 FPS** in stereo at native
  Quest 3 resolution. So expect a smooth-enough reprojected experience, **not** native
  72/90 Hz. Fewer splats (~150K) will feel noticeably better.

What does **not** exist yet: an Android/Quest-configured Unity project. That is what
`projects/GaussianExample-QuestVR/` (scaffolded, see §3) is for.

## 2. Key technical decisions

| Decision | Choice | Why |
|---|---|---|
| Render pipeline | **URP** | `GaussianSplatURPFeature` contains the Quest 3 stereo workarounds; URP is the standard Quest pipeline. Requires Unity 6 with Render Graph (compatibility mode OFF). |
| XR plugin | **OpenXR** (`com.unity.xr.openxr`) with "Meta Quest Support" feature group | Unity 6 standard, Meta's recommended path. (The Oculus XR plugin would also work.) |
| Stereo mode | **Single Pass Instanced / Multiview** | The package requires it (`XRSettings.stereoRenderingMode` check); multipass would fall back to broken/mono rendering. On Android+Vulkan this is `VK_KHR_multiview`. |
| Graphics API | **Vulkan only** | Required by the package (compute shaders, no GLES support — upstream issue #26). |
| Scripting | **IL2CPP + ARM64** | Quest Store requirement and only sane perf choice. |
| Color space | **Linear** | Required by URP/OpenXR on Quest. |
| Input | Plain `UnityEngine.XR.InputDevices` fly controller | No XR Interaction Toolkit dependency needed for simple thumbstick navigation. |

## 3. What is already scaffolded (this repo)

New project skeleton at **`projects/GaussianExample-QuestVR/`**:

```
projects/GaussianExample-QuestVR/
├── Packages/manifest.json          # URP + OpenXR + Input System + local gaussian-splatting package
├── ProjectSettings/ProjectVersion.txt  # 6000.4.4f1 (same editor as GaussianExample)
├── Assets/
│   ├── Scripts/VRFlyController.cs  # thumbstick fly navigation (VR analog of your FlyCamera.cs)
│   └── Editor/QuestPlayerSettingsSetup.cs  # menu: Tools > Gaussian Splats > Apply Quest Android Settings
└── README.md                       # condensed version of the steps below
```

Unity generates the rest of `ProjectSettings/`, `Library/` etc. on first open.
Scene, URP assets, and XR Plug-in Management settings must be created in the editor
(serialized formats are too version-fragile to hand-author) — steps below.

## 4. Editor setup, step by step

### 4.1 Prerequisites

- Unity Hub: install **Unity 6000.4.4f1** with modules **Android Build Support**
  (+ OpenJDK + Android SDK & NDK Tools).
- Quest 3: enable **Developer Mode** (Meta Horizon phone app → headset → Developer Mode),
  connect USB-C, allow USB debugging in the headset. Verify with `adb devices`.

### 4.2 Open the project & install packages

1. Unity Hub → *Add project from disk* → `projects/GaussianExample-QuestVR`.
2. First open resolves `manifest.json`. If a pinned XR/Input package version is not
   available for your editor, open **Window > Package Manager** and install
   **OpenXR Plugin**, **XR Plugin Management**, **Input System** at recommended versions.
3. When prompted to enable the **Input System backend** → Yes (restarts editor).

### 4.3 Player settings

Run **Tools > Gaussian Splats > Apply Quest Android Settings** (scaffolded editor script).
It sets: Linear color space, Android graphics API = **Vulkan only** (Auto off),
IL2CPP, ARM64, min SDK 32, ASTC texture compression, multithreaded rendering.
Also: **File > Build Profiles → Android → Switch Platform**.

### 4.4 XR Plug-in Management (manual — cannot be scripted reliably)

1. **Edit > Project Settings > XR Plug-in Management** → Install (if button shown).
2. **Android tab** → check **OpenXR**.
3. Under **OpenXR** settings (Android tab):
   - **Render Mode: Single Pass Instanced \ Multi-view**  ← critical, package requires it
   - **Depth Submission Mode**: None (splats don't write depth anyway)
   - Add Interaction Profile: **Oculus Touch Controller Profile**
   - Enabled OpenXR feature groups: **Meta Quest Support** (check it; open its settings
     and select target device Quest 3).
4. Fix any red warnings the OpenXR Project Validation window shows.

### 4.5 URP setup (manual)

1. `Assets` → right-click → **Create > Rendering > URP Asset (with Universal Renderer)**
   → name it e.g. `QuestURP`.
2. On the generated **Universal Renderer Data** asset → **Add Renderer Feature** →
   **Gaussian Splat URP Feature**.
3. On the **URP asset**:
   - **MSAA: Disabled** ← splat compositing breaks with any MSAA (URP default is 4x — change it!)
   - **HDR: off** (bandwidth), but then keep **Intermediate Texture: Always**
     (known upstream issue: HDR off + "Auto" renders upside down)
   - Render Scale 1.0 (the package has its own splat-only `m_RenderScale` — cheaper, use that instead)
4. **Project Settings > Graphics** → assign `QuestURP` as the default render pipeline
   (and in **Quality** settings for the active quality level; delete unused quality levels).
5. **Project Settings > Graphics > Render Graph**: leave **Compatibility Mode OFF**
   (the URP feature requires the Render Graph path).

### 4.6 Scene setup

Create scene `Assets/GSQuestScene.unity`:

1. Delete default camera. Create hierarchy:
   - `XR Rig` (empty GO at origin) — add **`VRFlyController`** (scaffolded script)
     - `Main Camera` (child, tag *MainCamera*) — add **Tracked Pose Driver (Input System)**
       (Position + Rotation, Center Eye / HMD bindings), near clip ~0.1, far ~100,
       background solid color (skybox costs fill rate).
   - Assign `Main Camera` to the controller's **Head** field.
2. `Gaussian Splats` GO — add **GaussianSplatRenderer**, assign your splat asset.
   PLY captures are usually upside down: start with rotation **(-160, 0, 0)** and scale
   **(1, 1, -1)** like `GSTestScene`, then adjust. Scale/position the object so the
   captured room is at a comfortable real-world scale around the rig.
3. Set Quest-friendly values on the GaussianSplatRenderer (defaults are already
   desktop-safe; these lean further toward Quest):

   | Field | Quest value | Note |
   |---|---|---|
   | `SH Order` | 0 | biggest single win; SH bands are expensive |
   | `Sort Nth Frame` | 10–15 | with Adaptive Sort on, mostly moot |
   | `Adaptive Sort` | on (default) | re-sorts only on real head movement |
   | `Render Scale` | 0.5 (default) | splats-only upscale; 0.5 barely visible |
   | `Contribution Cull Threshold` | 0.1 (default) | culls invisible splats |
   | `Sort Passes` | 2 (default) | 16-bit depth sort, indistinguishable |
   | `High Precision RT` | off (default) | RGBA8 composite, half the bandwidth |
   | `SH LOD` | on (default) | only matters if SH Order > 0 |
4. Add the scene to Build Profiles scene list.

### 4.7 Prepare the splat data (important!)

Your current asset (`GaussianExample/Assets/Resources/25.4.2024.asset`) has
**1,450,458 splats — ~5x too many for Quest**. Target **≤300K** (≈16–18 FPS), or
**~150K** for extra headroom.

1. Reduce the PLY first. Easiest: **SuperSplat editor** (https://superspl.at/editor —
   open source, runs in browser): load `25.4.2024.ply`, crop to the interesting region,
   delete outlier/floater splats, use splat reduction until ≤300K, export as PLY.
2. In the Quest project: **Tools > Gaussian Splats > Create GaussianSplatAsset** on the
   reduced PLY. **Quality: "Low" or "Medium"** — do **NOT** use "Very Low":
   it compresses color as **BC7, which Quest's Adreno GPU does not support**
   ("Low"/"Medium" use Norm8x4, fine on Android). Your existing asset used Float16x4
   color — also fine, but recreate anyway from the reduced PLY.
3. Alternative for iterating without rebuilding the APK: the fork has a **runtime PLY
   loader** (`GaussianSplatPlyLoader`, commit `f285012`) — `adb push` a PLY to
   `Application.persistentDataPath` and load it at startup. Optional; asset-in-build
   is the simpler first step.

### 4.8 Build & deploy

1. **File > Build Profiles > Android**: set your device under Run Device (or build APK only).
2. **Build And Run** → installs directly on the Quest. Or:
   `adb install -r GSQuestVR.apk`; find it under *Library > Unknown Sources* in the headset.
3. Profiling: `adb logcat -s Unity`, or **OVR Metrics Tool** / Meta Quest Developer Hub
   for FPS overlay in-headset.

## 5. Controller navigation (scaffolded `VRFlyController.cs`)

VR analog of your desktop `FlyCamera.cs`, moving the rig (never the tracked camera):

- **Left stick**: fly forward/back + strafe, relative to where you look (head yaw + pitch)
- **Right stick X**: smooth turn; **Right stick Y**: move up/down
- **Left grip (hold)**: sprint multiplier
- **Right A button**: reset to start pose (like your `R` key)

Tunables (speed, sprint, turn rate, snap-vs-smooth turn) are inspector fields.

## 6. Known gotchas checklist

- [ ] MSAA must be **Disabled** everywhere (URP asset default is 4x!)
- [ ] Render Mode must be **Multiview/Single Pass Instanced** — multipass will not render splats correctly
- [ ] Render Graph **Compatibility Mode OFF** (URP feature needs Render Graph)
- [ ] **Vulkan only** in Android graphics APIs (GLES3 fallback will not run compute path)
- [ ] No **BC7** color format in splat assets (Android has no BC texture support)
- [ ] HDR off ⇒ Intermediate Texture must stay **"Always"**
- [ ] Splats don't write depth / don't receive lighting — keep the scene otherwise empty or opaque-only
- [ ] Expect 16–18 FPS at 300K splats — Quest's reprojection (ASW) keeps it comfortable, but don't expect 72 Hz
- [ ] GPU memory: asset size + ~48 bytes/splat runtime buffers (300K splats ≈ 15 MB runtime — fine)

## 7. Suggested order of work

1. ✅ Scaffold project (done, this commit)
2. Reduce PLY to ≤300K splats in SuperSplat
3. Open project, apply player settings, set up XR + URP + scene (§4.2–4.6)
4. **Sanity check in editor first**: press Play with Quest over **Meta Quest Link** (PC VR)
   or just a desktop preview — verifies the pipeline before any Android build
5. First Android build with the reduced asset; verify stereo + tracking + input
6. Tune renderer perf fields on-device; adjust splat count as needed
7. Optional: runtime PLY loading, snap turn, teleport, multiple datasets menu
