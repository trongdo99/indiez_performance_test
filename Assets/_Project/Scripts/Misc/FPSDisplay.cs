using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    [SerializeField] private float _updateInterval = 0.5f;
    [SerializeField] private bool _showAverageFPS = true;
    
    private float _accumulatedFPS = 0;
    private float _timeLeft;
    private int _frameCount = 0;
    private float _currentFPS = 0;
    private GUIStyle _style;

    private void Start()
    {
        _timeLeft = _updateInterval;
        _style = new GUIStyle();
        _style.fontSize = 24;
        _style.normal.textColor = Color.white;
        _style.fontStyle = FontStyle.Bold;
    }

    private void Update()
    {
        _timeLeft -= Time.deltaTime;
        _accumulatedFPS += Time.timeScale / Time.deltaTime;
        _frameCount++;

        if (_timeLeft <= 0.0f)
        {
            _currentFPS = _showAverageFPS ? 
                _accumulatedFPS / _frameCount : 
                Time.timeScale / Time.deltaTime;

            _timeLeft = _updateInterval;
            _accumulatedFPS = 0;
            _frameCount = 0;
        }
    }

    private void OnGUI()
    {
        if (_currentFPS >= 60)
            _style.normal.textColor = Color.green;
        else if (_currentFPS >= 30)
            _style.normal.textColor = Color.yellow;
        else
            _style.normal.textColor = Color.red;

        GUI.Label(new Rect(10, 10, 200, 30), $"FPS: {Mathf.Round(_currentFPS)}", _style);
    }
}