using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour, ISyncInitializable
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _gameplayHUD;
    [SerializeField] private GameObject _pausedPanel;
    [SerializeField] private GameObject _virtualButtons;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private GameObject _countDownPanel;
    [SerializeField] private TMP_Text _countdownText;
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _killsCounter;
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private Slider _throwWeaponCooldownSlider;
    [SerializeField] private TMP_Text _throwWeaponCounter;
    
    private Coroutine _cooldownAnimationCoroutine;
    
    public void Initialize(IProgress<float> progress = null)
    {
        HideAllUI();
        
        _throwWeaponCooldownSlider.value = 0f;
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<GameEvents.GameStateChanged, EventData.GameStateChangedData>(HandleGameStateChanged);
        EventBus.Instance.Subscribe<GameEvents.GameStartingCountDown, EventData.GameStartingCountDownData>(HandleOnCountDownTick);
        EventBus.Instance.Subscribe<GameEvents.GameStartingCountDownCompleted>(HandleOnCountDownCompleted);
        EventBus.Instance.Subscribe<GameEvents.PlayerDeathAnimationCompleted>(HandlePlayerDeathAnimationCompleted);
        EventBus.Instance.Subscribe<GameEvents.PlayerHealthChanged, EventData.PlayerHealthChangedData>(HandlePlayerHealthChanged);
        EventBus.Instance.Subscribe<GameEvents.WaveCompleted, EventData.WaveCompletedData>(HandleWaveCompleted);
        EventBus.Instance.Subscribe<GameEvents.TotalZombiesKilled, EventData.TotalZombiesKilledData>(HandleTotalZombiesKilled);
        EventBus.Instance.Subscribe<GameEvents.ShowVictoryPanel>(HandleShowVictoryPanel);
        EventBus.Instance.Subscribe<GameEvents.ThrowWeaponCooldown, EventData.ThrowWeaponCooldownData>(HandleThrowWeaponCooldown);
        EventBus.Instance.Subscribe<GameEvents.GrenadeCountChanged, EventData.GrenadeCountChangedData>(HandleGrenadeCountChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameEvents.GameStateChanged, EventData.GameStateChangedData>(HandleGameStateChanged);
        EventBus.Instance.Unsubscribe<GameEvents.GameStartingCountDown, EventData.GameStartingCountDownData>(HandleOnCountDownTick);
        EventBus.Instance.Unsubscribe<GameEvents.GameStartingCountDownCompleted>(HandleOnCountDownCompleted);
        EventBus.Instance.Unsubscribe<GameEvents.PlayerDeathAnimationCompleted>(HandlePlayerDeathAnimationCompleted);
        EventBus.Instance.Unsubscribe<GameEvents.PlayerHealthChanged, EventData.PlayerHealthChangedData>(HandlePlayerHealthChanged);
        EventBus.Instance.Unsubscribe<GameEvents.WaveCompleted, EventData.WaveCompletedData>(HandleWaveCompleted);
        EventBus.Instance.Unsubscribe<GameEvents.TotalZombiesKilled, EventData.TotalZombiesKilledData>(HandleTotalZombiesKilled);
        EventBus.Instance.Unsubscribe<GameEvents.ShowVictoryPanel>(HandleShowVictoryPanel);
        EventBus.Instance.Unsubscribe<GameEvents.ThrowWeaponCooldown, EventData.ThrowWeaponCooldownData>(HandleThrowWeaponCooldown);
        EventBus.Instance.Unsubscribe<GameEvents.GrenadeCountChanged, EventData.GrenadeCountChangedData>(HandleGrenadeCountChanged);
        
        if (_cooldownAnimationCoroutine != null)
        {
            StopCoroutine(_cooldownAnimationCoroutine);
            _cooldownAnimationCoroutine = null;
        }
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
        _victoryPanel.SetActive(false);
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

    private void HandlePlayerHealthChanged(EventData.PlayerHealthChangedData data)
    {
        _healthSlider.value = Mathf.Clamp01(data.PlayerController.CurrentHealth / data.PlayerController.MaxHealth);
    }

    private void HandleWaveCompleted(EventData.WaveCompletedData data)
    {
        if (data.IsFinalWave)
        {
            _waveText.text = "FINAL WAVE";
        }
        else
        {
            _waveText.text = $"WAVE {data.WaveNumber + 1}";
        }
    }

    private void HandleTotalZombiesKilled(EventData.TotalZombiesKilledData data)
    {
        _killsCounter.text = $"{data.TotalZombiesKilled} KILLS";
    }
    
    private void HandleThrowWeaponCooldown(EventData.ThrowWeaponCooldownData data)
    {
        if (_throwWeaponCooldownSlider == null) return;
        
        if (_cooldownAnimationCoroutine != null)
        {
            StopCoroutine(_cooldownAnimationCoroutine);
        }
        
        _cooldownAnimationCoroutine = StartCoroutine(AnimateThrowWeaponCooldown(data.CooldownDuration));
    }
    
    private IEnumerator AnimateThrowWeaponCooldown(float cooldownDuration)
    {
        if (_throwWeaponCooldownSlider == null) yield break;
        
        _throwWeaponCooldownSlider.value = 1f;
        
        var timeElapsed = 0f;
        while (timeElapsed < cooldownDuration)
        {
            float normalizedValue = Mathf.Lerp(1f, 0f, timeElapsed / cooldownDuration);
            _throwWeaponCooldownSlider.value = normalizedValue;
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        _throwWeaponCooldownSlider.value = 0f;
        _cooldownAnimationCoroutine = null;
    }

    private void HandleGrenadeCountChanged(EventData.GrenadeCountChangedData data)
    {
        if (_throwWeaponCounter == null) return;
        
        _throwWeaponCounter.text = data.NewGrenadeCount.ToString();
    }

    private void HandleShowVictoryPanel()
    {
        _gameplayHUD.SetActive(false);
        
        _victoryPanel.SetActive(true);
        
        StartCoroutine(AnimateVictoryPanel());
    }

    private IEnumerator AnimateVictoryPanel()
    {
        if (_victoryPanel == null) yield break;
        
        var canvasGroup = _victoryPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = _victoryPanel.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        
        var duration = 1f;
        var elapsed = 0f;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        
        Time.timeScale = 1f;

        canvasGroup.enabled = false;
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