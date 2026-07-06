using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

// One-click Android player settings for Quest 3 + Vulkan + this repo's
// gaussian splatting package. XR Plug-in Management and URP asset setup
// remain manual — see the project README / 3DGSQuestAndroidVulkan.md.
public static class QuestPlayerSettingsSetup
{
    [MenuItem("Tools/Gaussian Splats/Apply Quest Android Settings")]
    public static void Apply()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // Vulkan only — the package needs compute shaders; GLES3 will not work.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { GraphicsDeviceType.Vulkan });

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32; // Quest 3 baseline

        PlayerSettings.MTRendering = true;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)) ||
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android).StartsWith("com.DefaultCompany"))
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.consensive.gsquestvr");
        }

        Debug.Log("Quest Android settings applied: Linear color, Vulkan-only, IL2CPP, ARM64, minSdk 32, ASTC, MT rendering.\n" +
                  "Still manual: (1) XR Plug-in Management > Android > enable OpenXR, Meta Quest Support feature, " +
                  "Render Mode 'Single Pass Instanced \\ Multi-view'; (2) URP asset with GaussianSplatURPFeature, MSAA Disabled; " +
                  "(3) switch platform to Android in Build Profiles.");
    }
}
