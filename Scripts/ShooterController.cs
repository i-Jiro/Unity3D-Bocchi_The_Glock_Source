using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DG.Tweening;
using MoreMountains.Feedbacks;
using RootMotion.FinalIK;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ShooterController : MonoBehaviour
{
    public Transform AimPositon;
    public Transform MuzzleTransform;
    public GameObject ShootFX;
    public FacialAnimationController facialController;
    public MMFeedbacks ShootingFeedback;
    public LayerMask ShootableLayer;

    [SerializeField] protected string ReloadAnimClipName;

    public enum ActionState {Cover, Aim, Dead}
    public ActionState CurrentState;
    
    private float _health = 100f;
    private float _coverHealth = 100f;
    private float _baseDamage = 10f;
    private float _burstGen = 10f;
    private int _maxAmmo = 30;
    private int _currentAmmo = 30;
    [SerializeField][Tooltip("Shots per second.")]
    private float _fireRate = 1.0f;
    [Tooltip("Total time to reload.")][SerializeField]
    private float _reloadDuration = 1f;
    private float _gunCDtimer = 0.0f;
    private Vector3 _startPosition;

    private bool _isAiming = false;
    private bool _isReloading = false;

    private Animator _animator;
    private AimIK _aimIK;
    private LookAtIK _lookAtIK;
    
    public float Health => _health;
    public float CoverHealth => _coverHealth;
    public float BaseDamage => _baseDamage;
    public float BurstGen => _burstGen;
    public bool IsAiming => _isAiming;
    public bool IsReloading => _isReloading;
    public int MaxAmmo => _maxAmmo;
    public int CurrentAmmo => _currentAmmo;

    public delegate void AmmoChangeEventHandler(int currentAmmo, int maxAmmo);
    public event AmmoChangeEventHandler AmmoChange;

    public delegate void ReloadEventHandler(float duration);
    public event ReloadEventHandler Reloading;
    
    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        _aimIK = GetComponent<AimIK>();
        _lookAtIK = GetComponent<LookAtIK>();
    }

    protected virtual void OnEnable()
    {
        if (!PlayerInputManager.Instance){Debug.LogWarning($"{gameObject.name}: could not find instance of Player Input Controller."); return;}
        PlayerInputManager.Instance.AimKeyPressed += OnAimKeyPressed;
        PlayerInputManager.Instance.AimKeyReleased += OnAimKeyReleased;
    }

    protected virtual void OnDisable()
    {
        if (!PlayerInputManager.Instance){Debug.LogWarning($"{gameObject.name}: could not find instance of Player Input Controller."); return;}
        PlayerInputManager.Instance.AimKeyPressed -= OnAimKeyPressed;
        PlayerInputManager.Instance.AimKeyReleased -= OnAimKeyReleased;
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        CurrentState = ActionState.Cover;
        _aimIK.solver.IKPositionWeight = 0f;
        _gunCDtimer = 0.0f;
        _startPosition = transform.position;
        ChangeReloadDuration(_reloadDuration);
    }
    

    // Update is called once per frame
    protected virtual void Update()
    {
        if(_gunCDtimer < _fireRate)
            _gunCDtimer += Time.deltaTime;
        switch(CurrentState)
        {
            //TODO: Throw player back into cover to force reload?
           case ActionState.Aim:
               //On first entering aim state.
               if (!_isAiming)
               {
                   _isAiming = true;
                   DOVirtual.Float(0f, 1f, 0.25f, (v) => {_aimIK.solver.IKPositionWeight = v; });
                   DOVirtual.Float(0.75f, 0f, 0.25f, (v) => { _lookAtIK.solver.IKPositionWeight = v; });
                   _animator.SetBool("isAiming", _isAiming);
                   //Interrupt reload.
                   if (_isReloading)
                   {
                       _isReloading = false;
                   }
               }

               //Find aim world position relative to mouse/crosshair position.
               Vector3 aimPos;
               var screenRay = Camera.main.ScreenPointToRay(PlayerInputManager.Instance.MousePosition);
               if (Physics.Raycast(screenRay, out var screenHit))
               {
                   aimPos = screenHit.point;
               }
               else //Fallback aim if the raycast hits nothing.
               {
                   aimPos = PlayerInputManager.Instance.MousePosition;
                   aimPos.z = Camera.main.nearClipPlane + 20f;
                   aimPos = Camera.main.ScreenToWorldPoint(aimPos);
               }
               AimPositon.transform.position = aimPos;

               var dir = (aimPos - MuzzleTransform.position).normalized;
               Ray ray = new Ray(MuzzleTransform.position, dir);
               Debug.DrawRay(MuzzleTransform.position, dir * 100, Color.red);
               
               //Shoot when fully in aiming state in animator.
               if (!_animator.GetCurrentAnimatorStateInfo(0).IsTag("Aiming")) return;

               //Ammo check.
               if (_currentAmmo <= 0)
               {
                   _animator.SetBool("isFiring", false);
                   return;
               }
               
               //Raycast from gun muzzle to aim target position.
               if (Physics.Raycast(ray, out var hit, Single.PositiveInfinity, ShootableLayer))
               {
                   //TODO: avoid get component in update.
                   var shootable = hit.collider.gameObject.GetComponent<IShootable>();
                   if (shootable != null)
                   {
                       if (_gunCDtimer >= _fireRate)
                       {
                           var damage = UnityEngine.Random.Range(100f, 200f);
                           shootable.TakeDamage(damage);
                           _gunCDtimer = 0.0f;
                           _currentAmmo--;
                           AmmoChange?.Invoke(_currentAmmo, _maxAmmo);
                           _animator.SetBool("isFiring", true);
                           var lookRotation = Quaternion.LookRotation(dir);
                           Instantiate(ShootFX, MuzzleTransform.position, lookRotation);
                           ShootingFeedback?.PlayFeedbacks();
                       }
                   }
               }
               else
               {
                   _animator.SetBool("isFiring", false);
               }
               break;
           case ActionState.Cover:
               //On first entering cover state after aiming state.
               if (_isAiming)
               {
                   _isAiming = false;
                   _animator.SetBool("isFiring", false);
                   _animator.SetBool("isAiming", _isAiming);
                   DOVirtual.Float(1f, 0f, 0.25f, (v) => {_aimIK.solver.IKPositionWeight = v; });
                   DOVirtual.Float(0f, 0.75f, 0.25f, (v) => { _lookAtIK.solver.IKPositionWeight = v; });
                   SnapIntoPosition();
               }
               
               //Wait until character fully enters cover animation.
               if (!_animator.GetCurrentAnimatorStateInfo(0).IsTag("Cover")) return;
               if (_currentAmmo < _maxAmmo && !_isReloading)
               {
                   _isReloading = true;
                   facialController.StartAnxious();
                   Reloading?.Invoke(_reloadDuration);
                   _animator.SetTrigger("reload");
               }
               
               break;
           case ActionState.Dead:
           default:
               break;
        }
    }

    //Adjust reload animation to match given duration.
    protected virtual void ChangeReloadDuration(float duration)
    {
        _reloadDuration = duration;
        if (_animator == null || string.IsNullOrEmpty(ReloadAnimClipName)) return;
        AnimationClip clip = _animator.runtimeAnimatorController.animationClips
            .FirstOrDefault(c => c.name == ReloadAnimClipName);

        if (clip == null) return;
        AnimationEvent reloadEvent = null;
        for (int i = 0; i < clip.events.Length; i++)
        {
            if (!clip.events[i].functionName.Equals("OnReloadComplete")) continue;
            reloadEvent = clip.events[i];
            break;
        }
                
        float speed;
        if (reloadEvent == null) //Fallback if reload event was not found.
        {
            speed = clip.length / duration;
        }
        else
        {
            speed = reloadEvent.time / duration;
        }
        _animator.SetFloat("reloadSpeed", speed);
    }

    //Snaps character back to position.
    //TODO: Remove hard coded values.
    protected void SnapIntoPosition()
    {
        transform.DORotate(new Vector3(0,150,0),0.5f);
        transform.DOMove(_startPosition, 0.5f);
    }

    //Called by animation event when reload is complete.
    public virtual void OnReloadComplete()
    {
        _currentAmmo = _maxAmmo;
        _isReloading = false;
        AmmoChange?.Invoke(_currentAmmo, _maxAmmo);
        facialController.ResetFace();
    }
    
    protected virtual void OnAimKeyPressed()
    {
        if (CurrentState == ActionState.Dead) return;
        CurrentState = ActionState.Aim;
    }

    protected virtual void OnAimKeyReleased()
    {
        if (CurrentState == ActionState.Dead) return;
        CurrentState = ActionState.Cover;
    }
}
