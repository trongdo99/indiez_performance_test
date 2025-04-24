using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    public static Bootstrapper Instance { get; private set; }

    public static string ActiveScenePath
    {
        get
        {
#if UNITY_EDITOR
            return EditorPrefs.GetString("ActiveScenePath");
#else
            return string.Empty;
#endif
        }

        set
        {
#if UNITY_EDITOR
            EditorPrefs.SetString("ActiveScenePath", value);
#endif
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private const int SceneIndex = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Debug.Log("Bootstrapper initializing ...");
        
#if UNITY_EDITOR
        Debug.Log($"Active scene path {ActiveScenePath}");
        EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[SceneIndex].path);
#endif
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public static class SceneTracker
{
    static SceneTracker()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }
    
    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string scenePath = EditorSceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(scenePath))
            {
                Bootstrapper.ActiveScenePath = scenePath;
                Debug.Log($"SceneTracker: Active scene {Bootstrapper.ActiveScenePath}");
            }
            else
            {
                Debug.LogWarning("SceneTracker: Active scene has no path");
            }
        }
    }
}
#endif

