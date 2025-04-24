using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Image _loadingBar;
    [SerializeField] private float _fillSpeed = 0.5f;
    [SerializeField] private Canvas _loadingCanvas;
    [SerializeField] private Camera _loadingCamera;
    [SerializeField] private List<SceneData> _scenes;

    private float _targetProgress;
    private bool _isLoading;
    
    public readonly GameSceneManager GameSceneManager = new GameSceneManager();

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
        
        float currentFillAmount = _loadingBar.fillAmount;
        float progressDifference = Mathf.Abs(currentFillAmount - _targetProgress);
        float dynamicFillSpeed = progressDifference * _fillSpeed;
        _loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, _targetProgress, Time.deltaTime * dynamicFillSpeed);
    }

    public async Task LoadSceneAsync(int index)
    {
        _loadingBar.fillAmount = 0f;
        _targetProgress = 1f;

        if (index < 0 || index >= _scenes.Count)
        {
            Debug.LogError($"Invalid scene index: {index}");
            return;
        }
        
        var loadingProgress = new LoadingProgress();
        loadingProgress.Progressed += target => _targetProgress = Mathf.Max(target, _targetProgress);
        
        EnableLoadingCanvas();
        await GameSceneManager.LoadScene(_scenes[index], loadingProgress);
        EnableLoadingCanvas(false);
    }

    private void EnableLoadingCanvas(bool enable = true)
    {
        _isLoading = enable;
        _loadingCanvas.gameObject.SetActive(enable);
        _loadingCamera.gameObject.SetActive(enable);
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

public class LoadingProgress : IProgress<float>
{
    public event Action<float> Progressed;

    private const float Ratio = 1f;

    public void Report(float value)
    {
        Progressed?.Invoke(value / Ratio);
    }
}
