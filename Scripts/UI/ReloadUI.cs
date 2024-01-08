using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ReloadUI : MonoBehaviour
{
    [SerializeField] private Image reloadGauge;
    [SerializeField] private ShooterController _shooterController;
    protected Tween _tween;
    protected Coroutine _interruptRoutine;

    protected void Start()
    {
        gameObject.SetActive(false);
    }

    public void StartGauge(float duration)
    {
        gameObject.SetActive(true);
        _tween = DOVirtual.Float(0, 1f, duration, (v) => reloadGauge.fillAmount = v)
            .OnComplete(() => { transform.gameObject.SetActive(false);});
        _interruptRoutine = StartCoroutine(CheckForInterrupt());
    }

    public void StopGauge()
    {
        _tween.Kill();
        transform.gameObject.SetActive(false);
    }

    public void SetShooter(ShooterController shooter)
    {
        if (_interruptRoutine != null)
        {
            StopCoroutine(_interruptRoutine);
            _interruptRoutine = null;
            StopGauge();
        }
        _shooterController = shooter;
    }

    protected IEnumerator CheckForInterrupt()
    {
        while (_shooterController.IsReloading)
        {
            yield return null;
        }
        StopGauge();
    }
}
