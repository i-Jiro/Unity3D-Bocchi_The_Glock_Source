using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAnimationHandler : MonoBehaviour
{
    [SerializeField] private GameObject _magOnHand;
    [SerializeField] private GameObject _magOnGun;
    [SerializeField] private GameObject _magPrefab;
    [SerializeField] private float _ejectForce = 1.25f;

    //Methods to be called by animation events.
    public void OnGrabMagazine()
    {
        _magOnHand.gameObject.SetActive(true);
    }

    public void OnDropMagazine()
    {
        _magOnGun.gameObject.SetActive(false);
        if (_magPrefab != null)
        {
            var droppedMag= Instantiate(_magPrefab, _magOnGun.transform.position, _magOnGun.transform.rotation);
            droppedMag.GetComponent<Rigidbody>().AddForce(-droppedMag.transform.up * _ejectForce, ForceMode.Impulse);
        }
    }

    public void OnInsertMagazine()
    {
        _magOnHand.gameObject.SetActive(false);
        _magOnGun.gameObject.SetActive(true);
    }
}
