using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    /// <summary>
    /// Android build. From the menu, or headless with the editor closed:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod EmberThrone.EditorTools.BuildTools.BuildApk -logFile Logs/build.log
    /// The result is signed with Unity's debug key, which is fine for test installs but not for the store.
    /// </summary>
    public static class BuildTools
    {
        public const string ApkPath = "Builds/EmberThrone.apk";

        [MenuItem("EmberThrone/Build Android APK")]
        public static void BuildApk()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootstrapScene },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log("[EmberThrone] Build " + summary.result + ": " + ApkPath + ", " +
                      (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalErrors + " errors, " +
                      (int)summary.totalTime.TotalSeconds + " s");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Game view size, to try screen shapes. Uses the current Play Mode window.</summary>
        public static void SetGameViewSize(int width, int height)
        {
            PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.GameView);
            PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, width + "x" + height);
        }
    }
}
