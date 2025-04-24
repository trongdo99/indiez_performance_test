using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour, ISyncInitializable
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _gameplayHUD;
    [SerializeField] private GameObject _virtualButtons;
    
    [SerializeField] private GameObject _countDownPanel;
    [SerializeField] private TMP_Text _countdownText;
    
    private GameplayManager _gameplayManager;
    private Player _player;
    
    public void Initialize(IProgress<float> progress = null)
    {
        HideAllUI();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    public void SetGameplayManager(GameplayManager gameplayManager)
    {
        _gameplayManager = gameplayManager;
    }
    
    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public void SubscribeToEvents()
    {
        _gameplayManager.OnGameStateChanged += HandleGameStateChanged;
        _gameplayManager.OnCountDownTick += HandleOnCountDownTick;
        _gameplayManager.OnCountDownCompleted += HandleOnCountDownCompleted;
        _player.OnPlayerDeathAnimationCompleted += HandlePlayerDeathAnimationCompleted;
    }

    private void UnsubscribeFromEvents()
    {
        _gameplayManager.OnCountDownTick -= HandleOnCountDownTick;
        _gameplayManager.OnCountDownCompleted -= HandleOnCountDownCompleted;
        _gameplayManager.OnGameStateChanged -= HandleGameStateChanged;
        _player.OnPlayerDeathAnimationCompleted -= HandlePlayerDeathAnimationCompleted;
    }

    private void HandleGameStateChanged(GameplayManager.GameState newState, GameplayManager.GameState previousState)
    {
        switch (newState)
        {
            case GameplayManager.GameState.Starting:
                _countDownPanel.SetActive(true);
                break;
            case GameplayManager.GameState.Playing:
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
    
    private void HandleOnCountDownTick(int seconds)
    {
        if (!_countDownPanel.activeSelf)
        {
            _countDownPanel.SetActive(true);       
        }
        
        if (seconds > 0)
        {
            _countdownText.text = seconds.ToString();
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
            await SceneLoader.Instance.LoadSceneAsync(0);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
}
