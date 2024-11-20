using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AutoStartSceneLoader
{
    static AutoStartSceneLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Specify the starting scene
            string startingScenePath = "Assets/Scenes/Login.unity";

            if (EditorSceneManager.GetActiveScene().path != startingScenePath)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(startingScenePath);
            }
        }
    }
}
