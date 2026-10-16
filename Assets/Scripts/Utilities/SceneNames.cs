#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

// Scene names used in code, plus an editor check that a scene name is actually in the Build Settings
// (SceneManager.LoadScene fails at runtime otherwise).
public static class SceneNames
{
    public const string StartScreen = "Start_Screen";

#if UNITY_EDITOR
    //Purpose: Warn in the Console if sceneName isn't an enabled scene in File > Build Settings
    public static void WarnIfNotInBuild(string sceneName, Object context)
    {
        var inBuild = EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && Path.GetFileNameWithoutExtension(scene.path) == sceneName);
        if (!inBuild)
        {
            Debug.LogWarning($"Scene \"{sceneName}\" is not an enabled scene in the Build Settings, so it can't be loaded.", context);
        }
    }
#endif
}
