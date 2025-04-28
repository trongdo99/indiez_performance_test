using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Data", menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    [Tooltip("Wave name for identification")]
    public string WaveName = "Wave 1";
    
    [Tooltip("The number of zombies to spawn in this wave")]
    public int ZombiesToSpawn = 10;
    
    [Tooltip("The spawn rate (zombies per second)")]
    public float SpawnRate = 1f;
    
    [Tooltip("Zombie type indices and their spawn weights")]
    public List<ZombieTypeWeight> ZombieTypes = new List<ZombieTypeWeight>();
    
    [Tooltip("Whether this wave includes a boss zombie")]
    public bool IncludesBoss;
    
    [Tooltip("Boss zombie type index")]
    public int BossZombieTypeIndex = 0;
    
    [Tooltip("When to spawn the boss (percentage of wave completion, 0-1)")]
    [Range(0, 1)]
    public float BossSpawnTiming = 0.5f;
    
    [Serializable]
    public class ZombieTypeWeight
    {
        [Tooltip("Index of the zombie type in ZombieManager")]
        public int ZombieTypeIndex = 0;
        
        [Tooltip("Relative probability weight for this zombie type")]
        public float Weight = 1f;
    }
}