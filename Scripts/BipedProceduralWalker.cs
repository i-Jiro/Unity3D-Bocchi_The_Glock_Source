using System;
using System.Collections;
using System.Collections.Generic;
using ExternalPropertyAttributes;
using MoreMountains.Tools;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(FullBodyBipedIK))]
public class BipedProceduralWalker : MonoBehaviour
{
    public bool DebugGizmo = false;
    [SerializeField] private FullBodyBipedIK _fullBodyBipedIK;
    [SerializeField] private LayerMask _groundLayerMask;
    public  Transform Body;
    
    private IKEffector _leftLeg;
    private Vector3 _currentLeftFootPosition;
    private Vector3 _newLeftFootPosition;
    private Vector3 _oldLeftFootPosition;
    private float _leftLerp = 1f;
    
    private IKEffector _rightLeg;
    private Vector3 _currentRightFootPosition;
    private Vector3 _newRightFootPosition;
    private Vector3 _oldRightFootPosition;
    private float _rightLerp = 1f;

    private float _landingLerp = 0f;
    [SerializeField] private float _maxDepth = 0.25f;

    [SerializeField] private AnimationCurve _walkCurve = AnimationCurve.Linear(0,0,1,1);
    [SerializeField] private AnimationCurve _liftCurve = new AnimationCurve(new Keyframe(0,0),
        new Keyframe(0.5f,1f), new Keyframe(1,0));
    [SerializeField] private AnimationCurve _landingCurve = new AnimationCurve(new Keyframe(0,0),
        new Keyframe(0.5f,1f), new Keyframe(1,0));

    public float walkSpeed = 4f;
    public float footSpacing = 0.2f;
    public float stepDistance = 0.25f;
    public float stepHeight = 0.5f;
    public float forwardOffset = 0.1f;
    public float rayLength = 0.75f;
    
    [SerializeField][MMReadOnly]
    private bool _isGrounded = false;
    public bool IsGrounded => _isGrounded;
    [SerializeField]
    private bool _landed = false;

    public delegate void LandingStartEventHandler();
    public event LandingStartEventHandler LandingStart;
    public delegate void LandingCompleteEventHandler();
    public event LandingCompleteEventHandler LandingComplete;
    
    
    protected void Awake()
    {
        _fullBodyBipedIK = GetComponent<FullBodyBipedIK>();
        _leftLeg = _fullBodyBipedIK.solver.leftFootEffector;
        _rightLeg = _fullBodyBipedIK.solver.rightFootEffector;
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRightLeg();
        UpdateLeftLeg();
    }
    
    //Reset the feet to their original position
    public void Initialize()
    {
        _leftLeg.positionWeight = 1f;
        _rightLeg.positionWeight = 1f;

        _currentLeftFootPosition = _oldLeftFootPosition = _newLeftFootPosition = transform.position + (Body.right * -footSpacing) + (Body.forward * forwardOffset);
        _currentRightFootPosition = _oldRightFootPosition = _newRightFootPosition = transform.position + (Body.right * footSpacing) + (Body.forward * -forwardOffset);
        
        _leftLerp = _rightLerp = 1f;
        
        _leftLeg.position = _currentLeftFootPosition;
        _rightLeg.position = _currentRightFootPosition;
    }

    //Sets IK weight to 0 to disable IK. Should be called when gameobject is deactivated for pooling.
    public void Disable()
    {
        _leftLeg.positionWeight = 0f;
        _rightLeg.positionWeight = 0f;
    }

    [Button("Test Landing")]
    public void Landing()
    {
        StartCoroutine(LandingRoutine());
    }

    private IEnumerator LandingRoutine()
    {
        LandingStart?.Invoke();
        yield return new WaitForEndOfFrame();
        while(_landingLerp <= 1f)
        {
            var yPos = Mathf.Lerp(0f, _maxDepth, _landingCurve.Evaluate(_landingLerp));
            
            var newPos = transform.localPosition;
            newPos.y = yPos;
            
            transform.localPosition = newPos;
            
            _landingLerp += Time.deltaTime * walkSpeed;
            yield return new WaitForEndOfFrame();
        }
        
        LandingComplete?.Invoke();
        _landingLerp = 0f;
    }

    private void UpdateLeftLeg()
    {
        _leftLeg.position = _currentLeftFootPosition;
        if (_rightLerp < 1) return;
        
        Ray groundRay = new Ray(Body.position + (Body.right * -footSpacing + (Body.forward * -forwardOffset) ), Vector3.down);
        if (Physics.Raycast(groundRay, out var hitInfo, rayLength, _groundLayerMask.value))
        {
            if (Vector3.Distance(_newLeftFootPosition, hitInfo.point) > stepDistance)
            {
                _newLeftFootPosition = hitInfo.point;
                _oldLeftFootPosition = _currentLeftFootPosition;
                _leftLerp = 0f;
            }
            if(DebugGizmo)
                Debug.DrawLine(Body.position + (Body.right * -footSpacing), hitInfo.point);
        }
        else
        {
            _currentLeftFootPosition =  _newLeftFootPosition = transform.position + (Body.right * -footSpacing) + (Body.forward * forwardOffset);
        }

        if (!(_leftLerp < 1)) return;
        var footPosition = Vector3.Lerp(_oldLeftFootPosition, _newLeftFootPosition, _walkCurve.Evaluate(_leftLerp));
        footPosition.y += _liftCurve.Evaluate(_leftLerp) * stepHeight;
        _currentLeftFootPosition = footPosition;
        
        _leftLerp += Time.deltaTime * walkSpeed;
    }
    
    private void UpdateRightLeg()
    {
        _rightLeg.position = _currentRightFootPosition;
        if (_leftLerp < 1f)  return;
        
        Ray groundRay = new Ray(Body.position + (Body.right * footSpacing) +  (Body.forward * forwardOffset), Vector3.down);
        if (Physics.Raycast(groundRay, out var hitInfo, rayLength, _groundLayerMask.value))
        {
            if (Vector3.Distance(_newRightFootPosition, hitInfo.point) > stepDistance)
            {
                _newRightFootPosition = hitInfo.point;
                _oldRightFootPosition = _currentRightFootPosition;
                _rightLerp = 0f;
            }
            if(DebugGizmo)
                Debug.DrawLine(Body.position + (Body.right * footSpacing), hitInfo.point);
        }
        else
        {
            _currentRightFootPosition = _newRightFootPosition = transform.position + (Body.right * footSpacing) + (Body.forward * -forwardOffset);
        }

        if (!(_rightLerp < 1)) return;
        var footPosition = Vector3.Lerp(_oldRightFootPosition, _newRightFootPosition, _walkCurve.Evaluate(_rightLerp));
        footPosition.y += _liftCurve.Evaluate(_rightLerp) * stepHeight;
        _currentRightFootPosition = footPosition;
        
        _rightLerp += Time.deltaTime * walkSpeed;
    }

    public bool IsMoving()
    {
        return (_leftLerp < 1 || _rightLerp < 1);
    }
    
    private void OnDrawGizmos()
    {
        if (DebugGizmo == false) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_newLeftFootPosition,0.1f);
        Gizmos.DrawSphere(_newRightFootPosition,0.1f);
    }
}
