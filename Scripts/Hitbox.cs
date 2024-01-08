using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class Hitbox : MonoBehaviour, IShootable
{
    public bool CriticalGuaranteed;
    [SerializeField] private GameObject _owener;
    public GameObject Owner => _owener;

    public delegate void DamageRecievedEventHandler(float damage, bool isCritical);
    public event DamageRecievedEventHandler DamageRecieved;

    public void SetOwner(GameObject owner)
    {
        _owener = owner;
    }
    
    public void TakeDamage(float value)
    {
        DamageRecieved?.Invoke(value, CriticalGuaranteed);
    }
}
