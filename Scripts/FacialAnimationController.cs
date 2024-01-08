using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FacialAnimationController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _skinnedMesh;
    [SerializeField] private Texture _baseTexture;
    [SerializeField] private Texture _shadowTexture;
    public AnimationCurve BlinkCurve;
    public AnimationCurve MouthQuiverCurve;
    public bool CanQuiver = false;
    public bool CanBlink = true;
    private float _blinkTimer;
    private float _mouthTimer;

    private Coroutine emotionRoutine;

    // Start is called before the first frame update
    void Start()
    {
        _blinkTimer = 0.0f;
    }

    private void Update()
    {
        Blink();
        QuiverMouth();
    }

    private void Blink()
    {
        if (!CanBlink) return;
        var weight = BlinkCurve.Evaluate(_blinkTimer);
        _skinnedMesh.SetBlendShapeWeight(20, weight * 100f);
        _blinkTimer += Time.deltaTime;
        if (BlinkCurve.keys[BlinkCurve.length-1].time <= _blinkTimer)
            _blinkTimer = 0.0f;
    }
    
    private void QuiverMouth()
    {
        if (!CanQuiver) return;
        var weight = MouthQuiverCurve.Evaluate(_mouthTimer);
        _skinnedMesh.SetBlendShapeWeight(14, weight * 100f);
        _mouthTimer += Time.deltaTime;
        if (MouthQuiverCurve.keys[MouthQuiverCurve.length-1].time <= _mouthTimer)
            _mouthTimer = 0.0f;
    }

    //Reset all blend shapes to default.
    public void ResetFace()
    {
        for (int i = 0; i < _skinnedMesh.sharedMesh.blendShapeCount; i++)
        {
            _skinnedMesh.SetBlendShapeWeight(i,0);
        }
        CanBlink = true;
        CanQuiver = false;
        _skinnedMesh.material.mainTexture = _baseTexture;
    }

    public void StartAnxious()
    {
        ResetFace();
        CanBlink = false;
        CanQuiver = true;
        _skinnedMesh.SetBlendShapeWeight(17, 100f);
        _skinnedMesh.SetBlendShapeWeight(20, 100f);
        _skinnedMesh.material.mainTexture = _shadowTexture;
    }
}
