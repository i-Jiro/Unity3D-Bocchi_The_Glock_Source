using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BattleState
{
    Intro, Battle, Outro
}

/// <summary>
/// Manages the overall battle flow and set-up.
/// </summary>
/// 
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    public WaveData waveData;
    public CinemachineVirtualCamera introCam;
    public CinemachineVirtualCamera battleCam;
    public bool PreSpawnInitialWave = true;
    
    private CinemachineBrain _cinemachineBrain;
    [Header("DEBUG")]
    [SerializeField] private bool _activateOnStart = true;
    [MMReadOnly][SerializeField] private bool _battleActive = false;
    public bool BattleActive => _battleActive;
    [MMReadOnly][SerializeField] private BattleState _battleState = BattleState.Intro;
    public BattleState BattleState => _battleState;
    
    public delegate void BattleStartedEventHandler();
    public event BattleStartedEventHandler BattleStarted;
    
    private void Awake()
    {
        //Set Instance to this object;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple BattleManager instances detected.");
            Destroy(this.gameObject);
        }

        if (Camera.main != null) _cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
    }

    private void Start()
    {
        if (PlayerInputManager.Instance)
            PlayerInputManager.Instance.enabled = false;
        if (!WaveSpawner.Instance) {Debug.LogWarning("Wave spawner instance was not found.");return;}
        WaveSpawner.Instance.WaveCompleted += OnWaveCompleted;
        WaveSpawner.Instance.LastWaveCompleted += OnLastWaveCompleted;
        WaveSpawner.Instance.SetWaveData(waveData);
        WaveSpawner.Instance.WarmEnemyPool(waveData);
        if (!_activateOnStart) return;
        if(PreSpawnInitialWave)
            WaveSpawner.Instance.SpawnWave();
        StartCoroutine(IntroSequence());
    }

    private void StartBattle()
    {
        Debug.Log("Starting Battle");
        BattleStarted?.Invoke();
        if (PlayerInputManager.Instance)
            PlayerInputManager.Instance.enabled = true;
        if(!PreSpawnInitialWave)
            WaveSpawner.Instance.SpawnWave();
        _battleActive = true;
        _battleState = BattleState.Battle;
    }

    //Initiate next wave or phase.
    private void OnWaveCompleted()
    {
        Debug.Log("Next phase");
        WaveSpawner.Instance.SpawnWave();
    }
    
    private void OnLastWaveCompleted()
    {
        _battleActive = false;
        _battleState = BattleState.Outro;
        Debug.Log("Battle Complete");
    }
    
    //Play intro sequence
    private IEnumerator IntroSequence()
    {
        //Switch to battle camera POV
        BattleUIManager.Instance.SetUIVisibility(false);
        introCam.enabled = false;
        battleCam.enabled = true;
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => !_cinemachineBrain.IsBlending);
        BattleUIManager.Instance.SetUIVisibility(true);
        StartBattle();
    }

    private void OnDestroy()
    {
        if (WaveSpawner.Instance)
        {
            WaveSpawner.Instance.WaveCompleted -= OnWaveCompleted;
            WaveSpawner.Instance.LastWaveCompleted -= OnLastWaveCompleted;
        }
    }
}
