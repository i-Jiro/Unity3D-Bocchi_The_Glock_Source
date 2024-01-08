using System;
using System.Collections;
using System.Collections.Generic;
using DG.DemiLib.Attributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WaveData", menuName = "ScriptableObjects/WaveData", order = 1)]
[DefaultExecutionOrder(-1)]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public struct Enemy
    {
        public GameObject EnemyPrefab;
        public int SpawnCount;
        [Tooltip("What volume to spawn the enemy in.")]
        public SpawnVolumeType SpawnVolume;
        [Tooltip("What location to spawn the enemy in.")]
        public SpawnLocationType SpawnLocation;
        [Tooltip("Time between each spawn.")]
        public float SpawnDelay;
    }
    
    [System.Serializable]
    public struct Wave
    {
        [FormerlySerializedAs("SpawnDelay")] public float StartDelay;
        public List<Enemy> Enemies;
    }
    

    public Dictionary<GameObject, int> Enemies;
    public List<Wave> Waves = new List<Wave>();

#if UNITY_EDITOR
    protected void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayStateChange;
    }
    
    void OnPlayStateChange(PlayModeStateChange state)
    {
        if(state == PlayModeStateChange.EnteredPlayMode)
        {
            CreateEnemyDictionary();
        }
    }
#else
    protected void OnEnable()
    {
        CreateEnemyDictionary();
    }
#endif

    protected void OnValidate()
    {
        CreateEnemyDictionary();
    }

    protected void CreateEnemyDictionary()
    {
        Enemies = new Dictionary<GameObject, int>();
        foreach (var wave in Waves)
        {
            foreach (var enemy in wave.Enemies)
            {
                if (Enemies.ContainsKey(enemy.EnemyPrefab))
                {
                    Enemies[enemy.EnemyPrefab] += enemy.SpawnCount;
                }
                else
                {
                    Enemies.Add(enemy.EnemyPrefab, enemy.SpawnCount);
                }
            }
        }
    }
}
