using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ammoCountText;
    [SerializeField] private Image _ammoGauge;
    [SerializeField] private Image _crosshairImage;
    [Range(0f,1f)]
    public float lowAmmoPercent = 0.25f;
    public Color lowAmmoColor;
    public float CrosshairRotationSpeed = 25f;
    [SerializeField] private MMFeedbacks _lowAmmoFeedback;
    private Tween _crosshairTween;

    private void OnEnable()
    {
        Cursor.visible = false;
        _crosshairTween = _crosshairImage.rectTransform.DORotate(new Vector3(0, 0, 1 * CrosshairRotationSpeed), 1f).SetLoops(-1, LoopType.Incremental);
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        _crosshairTween?.Kill();
    }

    private void LateUpdate()
    {
        transform.position = PlayerInputManager.Instance.MousePosition;
    }

    public void UpdateAmmoCount(int ammoCount, int maxAmmo)
    {
        var percentage = (float)ammoCount / maxAmmo;
        if (percentage <= lowAmmoPercent)
        {
            _ammoGauge.color = lowAmmoColor;
            _ammoCountText.color = lowAmmoColor;
            if(!_lowAmmoFeedback.IsPlaying)
                _lowAmmoFeedback?.PlayFeedbacks();
        }
        else
        {
            _ammoGauge.color = Color.white;
            _ammoCountText.color = Color.white;
            if(_lowAmmoFeedback.IsPlaying)
                _lowAmmoFeedback?.StopFeedbacks();
        }
        
        _ammoGauge.fillAmount = percentage;
        switch (ammoCount)
        {
            case < 10 and < 100:
                _ammoCountText.text = "00" + ammoCount;
                break;
            case >= 10 and < 100:
                _ammoCountText.text = "0" + ammoCount;
                break;
            default:
            {
                _ammoCountText.text = ammoCount.ToString();
                break;
            }
        }
    }
}
