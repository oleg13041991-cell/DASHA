#nullable enable
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RagsToRiches.Editor
{
    /// <summary>
    /// Стадия 0: сборка debug APK одной кнопкой. Настраивает Android-параметры
    /// (min API 24 по плану) и собирает development-билд перф-сцены.
    /// IL2CPP/ARM64 — репрезентативный замер (нужен NDK из Android Build Support);
    /// Mono/ARMv7 — быстрая сборка, но не встанет на 64-бит-only устройства (Pixel 7+).
    /// </summary>
    public static class AndroidBuildScript
    {
        private const string ScenePath = "Assets/Scenes/PerfTest.unity";

        [MenuItem("RagsToRiches/3. Собрать Debug APK (IL2CPP, ARM64)")]
        public static void BuildIl2CppApk() =>
            Build(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Builds/RagsToRiches-Stage0-il2cpp.apk");

        [MenuItem("RagsToRiches/4. Собрать Debug APK (Mono, быстрая сборка)")]
        public static void BuildMonoApk() =>
            Build(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7, "Builds/RagsToRiches-Stage0-mono.apk");

        private static void Build(ScriptingImplementation backend, AndroidArchitecture architectures, string apkPath)
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Stage0] Сцена {ScenePath} не найдена. Сначала выполните меню «RagsToRiches/2. Собрать перф-сцену».");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[Stage0] Не удалось переключиться на Android. Установлен ли модуль Android Build Support (SDK/NDK/OpenJDK)?");
                return;
            }

            PlayerSettings.productName = "Rags to Riches";
            PlayerSettings.companyName = "OlegDev";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.olegdev.ragstoriches");
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24; // план: min API 24
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
            PlayerSettings.Android.targetArchitectures = architectures;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory(Path.GetDirectoryName(apkPath)!);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[Stage0] APK готов: {apkPath} ({report.summary.totalSize / (1024 * 1024)} МБ). Установка: adb install -r {apkPath}");
            else
                Debug.LogError($"[Stage0] Сборка не удалась: {report.summary.result}. Смотрите Console/Editor.log.");
        }
    }
}
