using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyShooter : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private BipedProceduralWalker _walker;
    [SerializeField] private ProceduralShoot _proceduralShoot;
    [SerializeField] private List<Hitbox> _hitboxes;
    [Header("References")]
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private GameObject _shootFX;
    [SerializeField] private GameObject _aimTarget;

    [Header("Properties")]
    [SerializeField] private float _health = 1000f;
    public float ShootDuration = 3.0f;
    public float ShootFrequency = 5.0f;
    public float FireRate = 1.0f;
    public float FallSpeed = 8f;
    
    public float speed = 2f; // Speed of the movement
    public float leftLimit = -5f; // Left limit of the movement
    public float rightLimit = 5f; // Right limit of the movement

    private float direction = 1f; // Direction of the movement

    [Header("DEBUG")] [SerializeField] private bool _debug = false;
    [SerializeField][MMReadOnly]
    private bool _isDead = false;
    public bool Shooting = false;
    [MMReadOnly][SerializeField]
    private float _frequencyTimer = 0.0f;
    [MMReadOnly][SerializeField]
    private float _durationTimer = 0f;
    [MMReadOnly][SerializeField]
    private float fireTimer = 0f;
    private bool _isFalling = false;
    
    private GameObject _player;

    protected void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    protected void Start()
    {
        foreach (var hitbox in _hitboxes)
        {
            hitbox.SetOwner(this.gameObject);
            hitbox.DamageRecieved += OnDamageRecieved;
        }
    }

    public void Initialize(bool airdrop = false)
    {
        _isDead = false;
        Shooting = false;
        _health = 1000f;
        _frequencyTimer = 0.0f;
        _durationTimer = 0f;
        fireTimer = 0f;
        if (!airdrop)
        {
            _walker.Initialize();
            _isFalling = false;
            AimAtPlayer();
            return;
        }
        //Simulate gravity if spawned from above.
        _isFalling = true;
        var ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out var hitInfo, 100f))
        {
            var endPos = hitInfo.point;
            transform.DOMove(endPos, FallSpeed).SetEase(Ease.Flash)
                .SetSpeedBased(true)
                .OnComplete(()=>
                {
                    _isFalling = false;
                    _walker.Initialize();
                    VFXManager.Instance?.SpawnGroundHit(transform.position);
                    _walker.Landing();
                    AimAtPlayer();
                });
        }
        else
            Debug.LogWarning($"{gameObject.name}: Could not find ground to fall.");
    }
    
    void Update()
    {
        if (_isDead || _isFalling) return;
        if (_frequencyTimer <= ShootFrequency)
        {
            _frequencyTimer += Time.deltaTime;
        }
        else if(!Shooting)
        {
            Shooting = true;
        }
        Strafe();

        //Shoot only when battle manager instance is not null and battle is active. Otherwise if debug bool is true, shoot.
        //This is to prevent enemies from shooting when the battle is not active.
        if (BattleManager.Instance != null && BattleManager.Instance.BattleActive || _debug)
        {
            Shoot();
        }
    }

    protected void AimAtPlayer()
    {
        _aimTarget.transform.DOMove(_player.transform.position, 0.5f).SetEase(Ease.Flash);
    }
    
    protected void Strafe()
    {
        // Move the object
        if (_walker.IsMoving()) return;
        transform.position += Vector3.right * (speed * direction * Time.deltaTime);

        // If the object reaches the right limit, change direction to move left
        if (transform.position.x > rightLimit)
        {
            direction = -1f;
        }

        // If the object reaches the left limit, change direction to move right
        if (transform.position.x < leftLimit)
        {
            direction = 1f;
        }
    }

    protected void Shoot()
    {
        if (!Shooting){_proceduralShoot.IsShooting = false; return;}
        _durationTimer += Time.deltaTime;
        if (_durationTimer >= ShootDuration)
        {
            _frequencyTimer = 0f;
            _durationTimer = 0f;
            Shooting = false;
        }
        
        if (fireTimer <= FireRate)
        {
            fireTimer += Time.deltaTime;
            return;
        }

        fireTimer = 0;
        _proceduralShoot.IsShooting = true;
        Instantiate(_shootFX, _muzzlePoint.position, Quaternion.LookRotation(_muzzlePoint.forward, Vector3.up));
    }

    public void OnDamageRecieved(float damage, bool isGuranteedCrit)
    {
        var type = DamagePopUp.DamageType.Default;
        var finalDamage = damage;
        if (isGuranteedCrit)
        { type = DamagePopUp.DamageType.Critical;
            finalDamage *= 1.25f;
        }
        BattleUIManager.Instance?.DisplayDamage(finalDamage, type,
            transform.position + Vector3.up * 0.5f);
        _health -= damage;
        if (_health <= 0)
        {
            Die();
        }
    }
    
    public void Die()
    {
        _isDead = true;
        WaveSpawner.Instance?.RemoveEnemy(this.gameObject);
        VFXManager.Instance?.SpawnExplosion(transform.position);
        _walker.Disable();
        _aimTarget.transform.localPosition = new Vector3(0, 0.25f, 1);
        transform.position = new Vector3(-10 ,-10,-10);
    }
    
    protected void OnDestroy()
    {
        foreach (var hitbox in _hitboxes)
        {
            hitbox.SetOwner(null);
            hitbox.DamageRecieved -= OnDamageRecieved;
        }
    }
}
