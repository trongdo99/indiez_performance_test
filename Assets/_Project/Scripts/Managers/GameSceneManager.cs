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

        await UnloadScene();

        int sceneCount = SceneManager.loadedSceneCount;
        var loadedScenes = new List<string>();
        for (int i = 0; i < sceneCount; i++)
        {
            loadedScenes.Add(SceneManager.GetSceneAt(i).name);
        }

        if (reloadDupScene == false && loadedScenes.Contains(sceneData.Name)) return;
        
        var operationGroup = new AsyncOperationGroup(1);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneData.ScenePath, LoadSceneMode.Additive);
        operationGroup.Operations.Add(operation);
        
        while (!operationGroup.IsDone)
        {
            progress.Report(operationGroup.Progress);
            await Task.Delay(100);
        }
        
        Scene activeScene = SceneManager.GetSceneByName(sceneData.Name);
        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
        }
        
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

        var operationGroup = new AsyncOperationGroup(scenes.Count);
        foreach (string scene in scenes)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null) continue;
            operationGroup.Operations.Add(operation);
        }
        
        while (!operationGroup.IsDone)
        {
            await Task.Delay(100);
        }
        
        await Resources.UnloadUnusedAssets();
    }

    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;
        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
        public bool IsDone => Operations.All(o => o.isDone);
        
        public AsyncOperationGroup(int capacity)
        {
            Operations = new List<AsyncOperation>(capacity);
        }
    }
}