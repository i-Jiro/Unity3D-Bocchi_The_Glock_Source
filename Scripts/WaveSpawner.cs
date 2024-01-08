using System;
using System.Collections;
using System.Collections.Generic;
using ExternalPropertyAttributes;
using MoreMountains.Tools;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SpawnVolumeType
{
    Left, Right, Center
}

public enum SpawnLocationType
{
    Top, Floor, Random
}

/// <summary>
/// Handles spawning enemies in waves.
/// </summary>

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }
    [SerializeField] private List<SpawnVolume> _spawnVolumes = new List<SpawnVolume>();
    [SerializeField] private List<GameObject> _enemyPrefabs = new List<GameObject>();
    
    [SerializeField] private int _amountToWarmUpPool = 5;

    [Header("DEBUG")]   
    [MMReadOnly]
    [SerializeField] private int _enemyCount = 0;
    [MMReadOnly]
    [SerializeField] private int _waveCount = 0;
    [MMReadOnly]
    [SerializeField] private List<GameObject> _spawnedEnemies = new List<GameObject>();
    [MMReadOnly]
    [SerializeField] private bool _waveActive = false;
    [SerializeField] private WaveData _activeWaveData;


    [System.Serializable]
    private struct SpawnVolume
    {
        public Vector3 Center;
        public Vector3 Size;
        public SpawnVolumeType Type;
    }
    
    public delegate void WaveStartedEventHandler(int waveNumber);
    public event WaveStartedEventHandler WaveStarted;
    
    public delegate void WaveCompletedEventHandler();
    public event WaveCompletedEventHandler WaveCompleted;
    
    public delegate void LastWaveCompletedEventHandler();
    public event LastWaveCompletedEventHandler LastWaveCompleted;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple WaveSpawner instances detected.");
            Destroy(this.gameObject);
        }
    }

    public void WarmEnemyPool(WaveData data)
    {
        if(!_activeWaveData) {Debug.LogError("Wave data was not loaded. Cannot warm pool.");}
        foreach(var enemy in data.Enemies.Keys)
        {
            PoolManager.WarmPool(enemy, _amountToWarmUpPool);
        }
    }

    public void SetWaveData(WaveData data)
    {
        _waveCount = 0;
        _activeWaveData = data;
    }
    
    //Spawn a wave of enemies from wave data
    [Button("Spawn Wave")]
    public void SpawnWave()
    {
        if(_activeWaveData.Waves.Count <= _waveCount)
        {
            Debug.LogWarning("No more waves to spawn.");
            return;
        }
        WaveStarted?.Invoke(_waveCount);
        StartCoroutine(SpawnWaveCoroutine(_activeWaveData, _waveCount));
        _waveActive = true;
        _waveCount++;
    }

    IEnumerator SpawnWaveCoroutine(WaveData data, int waveIndex)
    {
        yield return new WaitForSeconds(data.Waves[waveIndex].StartDelay);
        foreach (var enemy in data.Waves[waveIndex].Enemies)
        {
            for (int i = 0; i < enemy.SpawnCount; i++)
            {
                //Find the volume to spawn the enemy in
                var volume = _spawnVolumes.Find(v => v.Type == enemy.SpawnVolume);
                switch (enemy.SpawnLocation)
                {
                    case SpawnLocationType.Floor:
                        SpawnEnemyOnFloor(enemy.EnemyPrefab, volume);
                        break;
                    case SpawnLocationType.Top:
                        SpawnEnemyOnCeiling(enemy.EnemyPrefab, volume);
                        break;
                    case SpawnLocationType.Random:
                        SpawnEnemy(enemy.EnemyPrefab, volume);
                        break;
                    default:
                        Debug.LogWarning("Could not find spawn location type. Defaulting to random.");
                        SpawnEnemy(enemy.EnemyPrefab, volume);
                        break;
                }
                yield return new WaitForSeconds(enemy.SpawnDelay);
            }
        }
    }
    
    //Create a spawn volume
    private SpawnVolume CreateSpawnVolume(Vector3 center, Vector3 size)
    {
        return new SpawnVolume
        {
            Center = center,
            Size = size
        };
    }
    
    
    //Spawn enemy prefab in a random position in a given spawn volume
    private void SpawnEnemy(GameObject enemy, SpawnVolume volume)
    {
        var pos = volume.Center + new Vector3(Random.Range(-volume.Size.x / 2, volume.Size.x / 2),
            Random.Range(-volume.Size.y / 2, volume.Size.y / 2),
            Random.Range(-volume.Size.z / 2, volume.Size.z / 2));
        Spawn(enemy, pos);
    }

    //Spawn an enemy on the bottom floor of a spawn volume.
    private void SpawnEnemyOnFloor(GameObject enemy, SpawnVolume volume)
    {
        var pos = volume.Center + new Vector3(Random.Range(-volume.Size.x / 2, volume.Size.x / 2),
            -volume.Size.y / 2,
            Random.Range(-volume.Size.z / 2, volume.Size.z / 2));
        Spawn(enemy, pos);
    }
    
    //Spawn an enemy on the top floor of a spawn volume.
    private void SpawnEnemyOnCeiling(GameObject enemy, SpawnVolume volume)
    {
        var pos = volume.Center + new Vector3(Random.Range(-volume.Size.x / 2, volume.Size.x / 2),
            volume.Size.y / 2,
            Random.Range(-volume.Size.z / 2, volume.Size.z / 2));
        Spawn(enemy, pos, true);
    }
    
    //Spawns an enemy from the object pool manager.
    private void Spawn(GameObject enemy, Vector3 position, bool drop = false)
    { 
        var clone = PoolManager.SpawnObject(enemy, position, Quaternion.Euler(0,-180,0));
        _spawnedEnemies.Add(clone);
        var enemyController = clone.GetComponent<EnemyShooter>();
        enemyController.Initialize(drop);
        _enemyCount++;
    }

    private IEnumerator SpawnRoutine(GameObject enemy, Vector3 position, bool drop = false)
    {
        var clone = PoolManager.SpawnObject(enemy, position, Quaternion.Euler(0,-180,0));
        var enemyController = clone.GetComponent<EnemyShooter>();
        //Give the enemy a frame to initialize before activating it.
        enemyController.gameObject.SetActive(false);
        yield return new WaitForEndOfFrame();
        enemyController.gameObject.SetActive(true);
        _spawnedEnemies.Add(clone);
        enemyController.Initialize(drop);
        _enemyCount++;
    }
    
    //Lower enemy count by 1 and releases enemy back into pooler. Also checks if the wave is completed.
    public void RemoveEnemy(GameObject enemy)
    {
        PoolManager.ReleaseObject(enemy);
        _spawnedEnemies.Remove(enemy);
        _enemyCount--;
        if(_enemyCount <= 0 && _waveActive)
        {
            _waveActive = false;
            if (_waveCount >= _activeWaveData.Waves.Count)
            {
                LastWaveCompleted?.Invoke();
                return;
            }
            WaveCompleted?.Invoke();
        }
    }
    
    //Spawn an enemy in any random volume on the bottom floor of the volume. For testing purposes.
    [Button("Spawn Random Enemy On Floor")]
    private void SpawnRandomEnemyOnFloor()
    {
        if (!Application.isPlaying){Debug.LogWarning("Enter playmode to spawn enemies."); return;}
        var volume = _spawnVolumes[Random.Range(0, _spawnVolumes.Count)];
        var pos = volume.Center + new Vector3(Random.Range(-volume.Size.x / 2, volume.Size.x / 2),
            -volume.Size.y / 2,
            Random.Range(-volume.Size.z / 2, volume.Size.z / 2));
        Spawn(_enemyPrefabs[Random.Range(0, _enemyPrefabs.Count)], pos);
    }
    
    //Kill all spawned enemies. For testing purposes.
    [Button("Kill All Enemies")]
    private void KillAllEnemies()
    {
        if (!Application.isPlaying){Debug.LogWarning("Enter playmode to kill enemies."); return;}
        var copy = _spawnedEnemies.ToArray();
        foreach (var enemy in copy)
        {
            enemy.GetComponent<EnemyShooter>().Die();
        }
    }
    
    
    //Display all spawn volumes in the editor. Each with a different color.
    private void OnDrawGizmos()
    {
        for (int i = 0; i < _spawnVolumes.Count; i++)
        {
#if UNITY_EDITOR
            Handles.Label(_spawnVolumes[i].Center, $"Spawn Volume {i}");
            Handles.Label(_spawnVolumes[i].Center + new Vector3(0,-0.5f,0), $"Type: {_spawnVolumes[i].Type}");
#endif
            Gizmos.color = Color.HSVToRGB(i / (float)_spawnVolumes.Count, 1, 1);
            Gizmos.DrawWireCube(_spawnVolumes[i].Center, _spawnVolumes[i].Size);
        }
    }
}
