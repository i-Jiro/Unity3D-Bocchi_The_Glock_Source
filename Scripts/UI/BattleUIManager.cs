using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.VisualScripting;
using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject _damagePopUpPrefab;
    [SerializeField] private ShooterController _shooterController; //To be replaced.

    [Header("Components")]
    [SerializeField] private ReloadUI _reloadUI;
    [SerializeField] private Crosshair _crosshairUI;
    [SerializeField] private List<CharacterSlotUI> _characterSlotUIs;
    public static BattleUIManager Instance;
    
    [Header("DEBUG")]
    [SerializeField] private bool _visibleOnStart = false;
    [SerializeField] [MMReadOnly] private bool _isVisible = true;

    public bool IsVisible => _isVisible;
    protected void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    protected void Start()
    {
        if(PoolManager.Instance != null)
            PoolManager.WarmPool(_damagePopUpPrefab, 20);
        else
        {
            Debug.LogWarning("Object pooler instance not found on scene.");
        }

        if(_shooterController == null)
            _shooterController = GameObject.FindObjectOfType<ShooterController>();
        if (_shooterController != null)
        {
            SetActiveShooter(_shooterController);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Could not find a shooter controller. Disabling UI.");
            _isVisible = false;
        }
        if(_visibleOnStart)
            SetUIVisibility(_isVisible);
    }

    protected void SetActiveShooter(ShooterController shooter)
    {
        //Unsubscribe from previous shooter if any.
        if (_shooterController)
        {
            _shooterController.AmmoChange -= OnAmmoChanged;
            _shooterController.Reloading -= OnReload;
        }
        _shooterController = shooter;
        _reloadUI.SetShooter(_shooterController);
        _crosshairUI.UpdateAmmoCount(_shooterController.CurrentAmmo,_shooterController.MaxAmmo);
        shooter.AmmoChange += OnAmmoChanged;
        shooter.Reloading += OnReload;
    }

    public void SetUIVisibility(bool value, bool instant = false)
    {
        _isVisible = value;
        _crosshairUI.gameObject.SetActive(value);
        foreach (var slot in _characterSlotUIs)
        {
            switch (value)
            {
                case true:
                    slot.FadeIn(instant);
                    break;
                case false:
                    slot.FadeOut(instant);
                    break;
            }
        }
    }

    public void DisplayDamage(float damage, DamagePopUp.DamageType type, Vector3 position)
    {
        var damagePopUp = PoolManager.SpawnObject(_damagePopUpPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
        damagePopUp.GetComponent<DamagePopUp>().Fire(damage, type);
    }

    private void OnAmmoChanged(int ammoCount, int maxAmmo)
    {
        _crosshairUI.UpdateAmmoCount(ammoCount, maxAmmo);
    }

    private void OnReload(float duration)
    {
        _reloadUI.StartGauge(duration);
    }

    private void OnDestroy()
    {
        //Unsubscribe from previous shooter if any.
        if (!_shooterController) return;
        _shooterController.AmmoChange -= OnAmmoChanged;
        _shooterController.Reloading -= OnReload;
    }
}
