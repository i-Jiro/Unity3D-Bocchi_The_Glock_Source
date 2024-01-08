using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class DamagePopUp : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Color DefaultColor;
    public Color CriticalColor;
    public Color ShieldColor;
    [SerializeField] private MMFeedbacks _feedback;

    public enum DamageType
    {
        Default, Critical, Shield
    }

    protected void Awake()
    {
        gameObject.SetActive(false);
    }
    

    public virtual void Fire(float damageValue, DamageType type)
    {
        switch (type)
        {
            case DamageType.Default:
                text.color = DefaultColor;
                break;
            case DamageType.Critical:
                text.color = CriticalColor;
                break;
            case DamageType.Shield:
                text.color = ShieldColor;
                break;
        }

        text.text = ((int)damageValue).ToString();
        gameObject.SetActive(true);
        StartCoroutine(Tick());
    }


    private IEnumerator Tick()
    {
        _feedback.Initialization();
        yield return new WaitForEndOfFrame();
        
        _feedback.PlayFeedbacks();
        while (_feedback.IsPlaying)
        {
            yield return null;
        }
        //release object from pool.
        PoolManager.ReleaseObject(this.gameObject);
    }
}
