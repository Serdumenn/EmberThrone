using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    public static class ProjectSetup
    {
        public const string BootstrapScene = "Assets/EmberThrone/Scenes/Bootstrap.unity";

        [MenuItem("EmberThrone/Setup Project")]
        public static void Run()
        {
            if (!File.Exists(BootstrapScene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BootstrapScene));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, BootstrapScene);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScene, true) };
            PlayerSettings.runInBackground = true;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            AssetDatabase.SaveAssets();
            if (!EditorApplication.isPlaying) EditorSceneManager.OpenScene(BootstrapScene, OpenSceneMode.Single);
            Debug.Log("[EmberThrone] Bootstrap scene ready and set as the only build scene.");
        }
    }
}
