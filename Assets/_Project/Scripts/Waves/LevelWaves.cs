using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Waves", menuName = "Game/Level Data")]
public class LevelWaves : ScriptableObject
{
    [Tooltip("All waves in this level")]
    public List<WaveData> Waves = new List<WaveData>();
    
    [Tooltip("Time between waves in seconds")]
    public float TimeBetweenWaves = 5f;
    
    [Tooltip("If true, waves will progress automatically")]
    public bool AutoProgressWaves = true;
}