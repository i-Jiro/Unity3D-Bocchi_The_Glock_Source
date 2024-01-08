using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralShoot : MonoBehaviour
{
    [SerializeField]
    private Transform _chestBone;
    public float Speed = 10f;
    public float ForwardMagnitude = 25f;
    public float BackwardMagnitude = -25f;
    public AnimationCurve RecoilCurve = AnimationCurve.EaseInOut(0,0,1,1);
    [Header("DEBUG")]
    public bool IsShooting = false;
    
    private Quaternion _originRot;
    private Quaternion _startRot;
    private Quaternion _endRot;


    private void Start()
    {
        _originRot = _chestBone.rotation;
        _startRot = _originRot * Quaternion.Euler(BackwardMagnitude, 0 ,0);
        _endRot = _startRot * Quaternion.Euler(ForwardMagnitude, 0, 0);
    }

    private void OnValidate()
    {
        _startRot = _originRot * Quaternion.Euler(BackwardMagnitude, 0 ,0);
        _endRot = _startRot * Quaternion.Euler(ForwardMagnitude, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsShooting) return;
        var t = Mathf.PingPong(Time.time * Speed, 1f);
        var newRot = Quaternion.Slerp(_startRot, _endRot, RecoilCurve.Evaluate(t));
        _chestBone.rotation = newRot;
    }
}
