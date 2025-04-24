using System;
using UnityEngine;

public class LoadingProgressSystem
{
    public event Action<float> OnProgressChanged;

    private float _progress;
    private float _sceneProgress;
    private float _initProgress;
    
    private readonly float _sceneWeight;
    private readonly float _initWeight;
    
    public LoadingProgressSystem()
    {
        _sceneWeight = 1.0f;
        _initWeight = 0.0f;
    }
    
    public LoadingProgressSystem(float sceneWeight)
    {
        _sceneWeight = Mathf.Clamp01(sceneWeight);
        _initWeight = 1.0f - _sceneWeight;
    }
    
    public void ReportSceneProgress(float value)
    {
        value = Mathf.Clamp01(value);
        _sceneProgress = value;
        UpdateCombinedProgress();
    }
    
    public void ReportInitProgress(float value)
    {
        value = Mathf.Clamp01(value);
        _initProgress = value;
        UpdateCombinedProgress();
    }
    
    private void UpdateCombinedProgress()
    {
        _progress = (_sceneProgress * _sceneWeight) + (_initProgress * _initWeight);
        NotifyProgressChanged(_progress);
    }
    
    private void NotifyProgressChanged(float value)
    {
        OnProgressChanged?.Invoke(value);
    }
    
    public IProgress<float> CreateSceneProgressTracker()
    {
        return new ProgressTracker(this, ProgressType.Scene);
    }
    
    public IProgress<float> CreateInitProgressTracker()
    {
        return new ProgressTracker(this, ProgressType.Init);
    }
    
    private enum ProgressType
    {
        Scene,
        Init
    }
    
    private class ProgressTracker : IProgress<float>
    {
        private readonly LoadingProgressSystem _system;
        private readonly ProgressType _type;
        
        public ProgressTracker(LoadingProgressSystem system, ProgressType type)
        {
            _system = system;
            _type = type;
        }
        
        public void Report(float value)
        {
            switch (_type)
            {
                case ProgressType.Scene:
                    _system.ReportSceneProgress(value);
                    break;
                case ProgressType.Init:
                    _system.ReportInitProgress(value);
                    break;
            }
        }
    }
}