using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DoorForceField : MonoBehaviour
{
    [SerializeField] private GameObject _forceFieldVisual;
    [Tooltip("In seconds.")]
    [SerializeField] private float _timeToReset = 5f;
    [SerializeField] private AnimationCurve _healthCurve = AnimationCurve.EaseInOut(0,0,1,1);
    
    [SerializeField] private List<Hitbox> _hitboxes = new List<Hitbox>();
    
    [SerializeField] private float _baseHealth = 5000f;
    private bool _isActive = false;
    public bool IsActive => _isActive;
    
    [Header("DEBUG")]
    [SerializeField] private float _curveStep;
    [SerializeField] private float _currentHealth;
    
    private void Start()
    {
        _currentHealth = _healthCurve.Evaluate(0);
        if(BattleManager.Instance != null)
            BattleManager.Instance.BattleStarted += OnBattleStarted;
        foreach (var hitbox in _hitboxes)
        {
            hitbox.SetOwner(this.gameObject);
            hitbox.DamageRecieved += OnDamageRecieved;
            hitbox.gameObject.SetActive(false);
        }
        _forceFieldVisual.SetActive(false);
    }

    private void OnDamageRecieved(float damage, bool isCritical)
    {
        var type = DamagePopUp.DamageType.Shield;
        var finalDamage = damage;
        BattleUIManager.Instance?.DisplayDamage(finalDamage, type,
            transform.position + Vector3.up * 0.5f);
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            Shutdown();
        }
    }

    private void Shutdown()
    {
        StartCoroutine(ResetRoutine());
        _forceFieldVisual.SetActive(false);
        SetHitboxState(false);
    }

    // Start reset timer once battle starts.
    private void OnBattleStarted()
    {
        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        var timer = 0f;
        while (timer < _timeToReset)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        _curveStep = Mathf.Clamp01(_curveStep + 0.1f);
        _currentHealth = _baseHealth + _healthCurve.Evaluate(_curveStep) * _baseHealth;
        _forceFieldVisual.SetActive(true);
        SetHitboxState(true);
    }

    private void SetHitboxState(bool state)
    {
        foreach (var hitbox in _hitboxes)
        {
            hitbox.gameObject.SetActive(state);
        }
    }

    private void OnDestroy()
    {
        if(BattleManager.Instance != null)
            BattleManager.Instance.BattleStarted -= OnBattleStarted;
        foreach (var hitbox in _hitboxes)
        {
            hitbox.SetOwner(null);
            hitbox.DamageRecieved -= OnDamageRecieved;
        }
    }
}
