using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager
{
    public event Action<string> OnSceneLoaded;
    
    private SceneData _activeSceneData;
    
    public async Task LoadScene(SceneData sceneData, IProgress<float> progress, bool reloadDupScene = false)
    {
        _activeSceneData = sceneData;
        
        // Report initial progress
        progress?.Report(0.1f);
        Debug.Log($"[GameSceneManager] Starting to load scene: {sceneData.Name}, reporting 10% progress");
        
        // Unload existing scenes
        await UnloadScene();
        
        // Report progress after unloading
        progress?.Report(0.3f);
        Debug.Log($"[GameSceneManager] Scenes unloaded, reporting 30% progress");
        
        int sceneCount = SceneManager.sceneCount;
        var loadedScenes = new List<string>();
        for (int i = 0; i < sceneCount; i++)
        {
            loadedScenes.Add(SceneManager.GetSceneAt(i).name);
        }
        
        if (reloadDupScene == false && loadedScenes.Contains(sceneData.Name))
        {
            // Scene already loaded, report full progress
            progress?.Report(1.0f);
            Debug.Log($"[GameSceneManager] Scene {sceneData.Name} already loaded, reporting 100% progress");
            return;
        }
        
        Debug.Log($"[GameSceneManager] Starting to load scene asynchronously: {sceneData.Name}");
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneData.ScenePath, LoadSceneMode.Additive);
        
        // Track and report progress continuously
        float startProgress = 0.3f;
        float endProgress = 0.9f;
        float lastReportedProgress = startProgress;
        
        while (!operation.isDone)
        {
            // Scale the operation progress (0-1) to our desired range (0.3-0.9)
            float scaledProgress = startProgress + (operation.progress * (endProgress - startProgress));
            
            // Only report if there's meaningful change
            if (Math.Abs(scaledProgress - lastReportedProgress) > 0.01f)
            {
                progress?.Report(scaledProgress);
                lastReportedProgress = scaledProgress;
                Debug.Log($"[GameSceneManager] Loading progress: {operation.progress:F2}, reporting scaled progress: {scaledProgress:F2}");
            }
            
            await Task.Delay(16); // Roughly 60fps update rate
        }
        
        // Report progress after loading scene
        progress?.Report(0.9f);
        Debug.Log($"[GameSceneManager] Scene loaded, reporting 90% progress");
        
        // Set the loaded scene as active
        Scene activeScene = SceneManager.GetSceneByName(sceneData.Name);
        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
            Debug.Log($"[GameSceneManager] Set {sceneData.Name} as active scene");
        }
        
        // Report final progress
        progress?.Report(1.0f);
        Debug.Log($"[GameSceneManager] Scene activation complete, reporting 100% progress");
        
        OnSceneLoaded?.Invoke(sceneData.Name);
    }
    
    public async Task UnloadScene()
    {
        var scenes = new List<string>();
        string activeSceneName = SceneManager.GetActiveScene().name;
        
        int sceneCount = SceneManager.sceneCount;
        for (var i = sceneCount - 1; i > 0; i--)
        {
            Scene sceneAt = SceneManager.GetSceneAt(i);
            if (!sceneAt.isLoaded) continue;
            
            string sceneName = sceneAt.name;
            if (sceneName.Equals(activeSceneName) && sceneName.Equals("Bootstrapper")) continue;
            scenes.Add(sceneName);
        }
        
        if (scenes.Count == 0)
        {
            Debug.Log("[GameSceneManager] No scenes to unload");
            return;
        }
        
        Debug.Log($"[GameSceneManager] Unloading {scenes.Count} scenes: {string.Join(", ", scenes)}");
        
        var operations = new List<AsyncOperation>();
        foreach (string scene in scenes)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null) continue;
            operations.Add(operation);
        }
        
        while (operations.Count > 0 && !operations.All(o => o.isDone))
        {
            await Task.Delay(16);
        }
        
        Debug.Log("[GameSceneManager] All scenes unloaded, unloading unused assets");
        await Resources.UnloadUnusedAssets();
        Debug.Log("[GameSceneManager] Unused assets unloaded");
    }
}
