using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Slider _loadingBar;
    [SerializeField] private float _fillSpeed = 0.5f;
    [SerializeField] private GameObject _loadingScreen;
    [SerializeField] private List<SceneData> _scenes;
    [SerializeField] private float _sceneLoadWeight = 0.5f;
    [SerializeField] private float _maxWaitTime = 3f;

    private GameInitializer _gameInitializer;
    private float _targetProgress;
    private bool _isLoading;
    
    private readonly GameSceneManager _gameSceneManager = new GameSceneManager();

    private async void Start()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(Bootstrapper.ActiveScenePath))
        {
            int sceneIndex = FindSceneIndexByPath(Bootstrapper.ActiveScenePath);
            if (sceneIndex >= 0)
            {
                Debug.Log($"Loading editor scene at {sceneIndex}");
                await LoadSceneAsync(sceneIndex);
                return;
            }
            else
            {
                Debug.LogWarning($"Active scene not found in _scenes list: {Bootstrapper.ActiveScenePath}");
            }
        }
#endif
        
        await LoadSceneAsync(0);
    }

    private void Update()
    {
        if (!_isLoading) return;
        
        _loadingBar.value = Mathf.Lerp(_loadingBar.value, _targetProgress, Time.deltaTime * _fillSpeed);
    }

    public async Task LoadSceneAsync(int index)
    {
        _loadingBar.value = 0f;
        _targetProgress = 1f;

        if (index < 0 || index >= _scenes.Count)
        {
            Debug.LogError($"Invalid scene index: {index}");
            return;
        }
        
        var progressSystem = new LoadingProgressSystem(_sceneLoadWeight);
        progressSystem.OnProgressChanged += (progress) =>
        {
            _targetProgress = progress;
        };
        
        EnableLoadingCanvas();
        
        IProgress<float> sceneProgressTracker = progressSystem.CreateSceneProgressTracker();
        await _gameSceneManager.LoadScene(_scenes[index], sceneProgressTracker);
        
        _gameInitializer = FindFirstObjectByType<GameInitializer>();
        if (_gameInitializer == null)
        {
            Debug.Log("GameInitializer not found, proceeding without game initialization phase");
            _targetProgress = 1f;
        }
        else
        {
            IProgress<float> initProgressTracker = progressSystem.CreateInitProgressTracker();
            await _gameInitializer.InitializeGame(initProgressTracker);
        }

        // Ensure the target is set to 1.0 at the end
        _targetProgress = 1.0f;
        
        // Wait until the loading bar is almost full before proceeding
        var waitTime = 0f;
        while (_loadingBar.value < 0.99f && waitTime < _maxWaitTime)
        {
            await Task.Delay(50);
            waitTime += 0.05f;
        }
        
        EnableLoadingCanvas(false);
    }

    private void EnableLoadingCanvas(bool enable = true)
    {
        _isLoading = enable;
        _loadingScreen.SetActive(enable);
    }

    private int FindSceneIndexByPath(string path)
    {
        for (var i = 0; i < _scenes.Count; i++)
        {
            if (_scenes[i].ScenePath.Equals(path)) return i;
        }

        return -1;
    }
}