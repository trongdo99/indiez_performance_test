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
        if (Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            Debug.Log($"Loading progress: bar={_loadingBar.value:F2}, target={_targetProgress:F2}");
        }
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
        
        var combinedProgress = new CombinedLoadingProgress(_sceneLoadWeight);
        
        var sceneLoadingProgress = new LoadingProgress();
        sceneLoadingProgress.Progressed += target =>
        {
            combinedProgress.UpdateSceneProgress(target);
            _targetProgress = combinedProgress.CombinedProgress;
        };
        
        EnableLoadingCanvas();
        
        await _gameSceneManager.LoadScene(_scenes[index], sceneLoadingProgress);
        
        _gameInitializer = FindFirstObjectByType<GameInitializer>();
        if (_gameInitializer == null)
        {
            Debug.Log("GameInitializer not found, proceeding without game initialization phase");
            _targetProgress = 1f;
        }
        else
        {
            var initializationProgress = new LoadingProgress();
            initializationProgress.Progressed += target =>
            {
                combinedProgress.UpdateInitProgress(target);
                _targetProgress = combinedProgress.CombinedProgress;
            };
            
            await _gameInitializer.InitializeGame(initializationProgress);
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

public class CombinedLoadingProgress
{
    private float _sceneProgress;
    private float _initProgress;
    private readonly float _sceneWeight;
    private readonly float _initWeight;
    
    public float CombinedProgress { get; private set; }

    public CombinedLoadingProgress(float sceneWeight = 0.5f)
    {
        _sceneWeight = sceneWeight;
        _initWeight = 1f - sceneWeight;
    }

    public void UpdateSceneProgress(float progress)
    {
        _sceneProgress = progress;
        UpdateCombinedProgress();
    }

    public void UpdateInitProgress(float progress)
    {
        _initProgress = progress;
        UpdateCombinedProgress();
    }

    private void UpdateCombinedProgress()
    {
        CombinedProgress = (_sceneProgress * _sceneWeight) + (_initProgress * _initWeight);
    }
}

public class LoadingProgress : IProgress<float>
{
    public event Action<float> Progressed;

    private const float Ratio = 1f;

    public void Report(float value)
    {
        value = Mathf.Clamp01(value);
        Progressed?.Invoke(value / Ratio);
    }
}
