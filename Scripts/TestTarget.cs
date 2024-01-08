using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTarget : MonoBehaviour, IShootable
{
    public void TakeDamage(float value)
    {
        BattleUIManager.Instance.DisplayDamage(value, DamagePopUp.DamageType.Default,
            transform.position + Vector3.up * 0.5f);
    }
}
