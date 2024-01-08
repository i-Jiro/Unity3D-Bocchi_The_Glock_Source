using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ExternalPropertyAttributes;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlotUI : MonoBehaviour
{
    public float FadeSpeed = 0.5f;
    public float MaxTransparency = 0.80f;
    public Ease EaseType = DG.Tweening.Ease.OutCubic;
    
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _characterImage;
    [SerializeField] private Image _healthBar;
    [SerializeField] private Image _healthBarBackground;
    [SerializeField] private Image _coverBar;
    [SerializeField] private Image _coverBarBackground;
    private List<Image> _images = new List<Image>();

    private void Awake()
    {
        _images.Add(_backgroundImage);
        _images.Add(_characterImage);
        _images.Add(_healthBar);
        _images.Add(_healthBarBackground);
        _images.Add(_coverBar);
        _images.Add(_coverBarBackground);
    }

    [Button("Fade In")]
    public void FadeIn(bool instant = false)
    {
        foreach (var image in _images)
        {
            if (instant)
            {
                image.DOFade(MaxTransparency, 0);
            }
            else
            {
                image.DOFade(MaxTransparency, FadeSpeed)
                    .SetSpeedBased(true)
                    .SetEase(EaseType);
            }
        }
    }

    [Button("Fade Out")]
    public void FadeOut(bool instant = false)
    {
        foreach (var image in _images)
        {
            if (instant)
            {
                image.DOFade(0, 0);
            }
            else
            {
                image.DOFade(0, FadeSpeed)
                    .SetSpeedBased(true)
                    .SetEase(EaseType);
            }
        }
    }
    
    public void SetAlpha(float alpha)
    {
        MaxTransparency = alpha;
        foreach (var image in _images)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }
    }
}
