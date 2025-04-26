using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour, ISyncInitializable
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _gameplayHUD;
    [SerializeField] private GameObject _pausedPanel;
    [SerializeField] private GameObject _virtualButtons;
    
    [SerializeField] private GameObject _countDownPanel;
    [SerializeField] private TMP_Text _countdownText;
    
    public void Initialize(IProgress<float> progress = null)
    {
        HideAllUI();
        
        EventBus.Instance.Subscribe<GameEvents.GameStateChanged, EventData.GameStateChangedData>(HandleGameStateChanged);
        EventBus.Instance.Subscribe<GameEvents.GameStartingCountDown, EventData.GameStartingCountDownData>(HandleOnCountDownTick);
        EventBus.Instance.Subscribe<GameEvents.GameStartingCountDownCompleted>(HandleOnCountDownCompleted);
        EventBus.Instance.Subscribe<GameEvents.PlayerDeathAnimationCompleted>(HandlePlayerDeathAnimationCompleted);
    }

    private void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<GameEvents.GameStateChanged, EventData.GameStateChangedData>(HandleGameStateChanged);
        EventBus.Instance.Unsubscribe<GameEvents.GameStartingCountDown, EventData.GameStartingCountDownData>(HandleOnCountDownTick);
        EventBus.Instance.Unsubscribe<GameEvents.GameStartingCountDownCompleted>(HandleOnCountDownCompleted);
        EventBus.Instance.Unsubscribe<GameEvents.PlayerDeathAnimationCompleted>(HandlePlayerDeathAnimationCompleted);
    }

    private void HandleGameStateChanged(EventData.GameStateChangedData data)
    {
        switch (data.NewState)
        {
            case GameplayStateType.Starting:
                _countDownPanel.SetActive(true);
                break;
            case GameplayStateType.Playing:
                _gameplayHUD.SetActive(true);
                _virtualButtons.SetActive(true);
                break;
        }
    }
    
    private void HandlePlayerDeathAnimationCompleted()
    {
        _gameplayHUD.SetActive(false);
        _gameOverPanel.SetActive(true);
    }

    private void HideAllUI()
    {
        _gameplayHUD.SetActive(false);
        _countDownPanel.SetActive(false);
        _gameOverPanel.SetActive(false);
    }
    
    private void HandleOnCountDownTick(EventData.GameStartingCountDownData data)
    {
        if (!_countDownPanel.activeSelf)
        {
            _countDownPanel.SetActive(true);       
        }
        
        if (data.Seconds > 0)
        {
            _countdownText.text = data.Seconds.ToString();
        }
        else
        {
            _countdownText.text = "SURVIVE";
        }
        
        StartCoroutine(PulseCountdownText());
    }
    
    private IEnumerator PulseCountdownText()
    {
        if (_countdownText == null) yield break;
    
        Vector3 originalScale = _countdownText.transform.localScale;
    
        _countdownText.transform.localScale = originalScale * 2f;
    
        var duration = 0.8f;
        var elapsed = 0f;
    
        while (elapsed < duration)
        {
            float scale = Mathf.Lerp(2f, 1f, elapsed / duration);
            _countdownText.transform.localScale = originalScale * scale;
        
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        _countdownText.transform.localScale = originalScale;
    }
    
    private void HandleOnCountDownCompleted()
    {
        _countDownPanel.SetActive(false);
    }

    public async void UI_RestartButtonClicked()
    {
        try
        {
            await SceneLoader.Instance.ReloadCurrentSceneAsync();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
    
    public async void UI_MainMenuButtonClicked()
    {
        try
        {
            await SceneLoader.Instance.LoadSceneAsync("MainMenu");
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void UI_PauseButtonClicked()
    {
        _gameplayHUD.SetActive(false);
        _pausedPanel.SetActive(true);
        GameplayManager.Instance.PauseGame();
    }

    public void UI_ResumeButtonClicked()
    {
        _pausedPanel.SetActive(false);
        _gameplayHUD.SetActive(true);
        GameplayManager.Instance.ResumeGame();
    }
}