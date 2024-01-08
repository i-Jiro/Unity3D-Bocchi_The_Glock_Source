using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;
    [SerializeField] private List<GameObject> _explosionPrefabs;
    [SerializeField] private List<GameObject> _groundHitPrefabs;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple VFXManager instances detected.");
            Destroy(this.gameObject);
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        foreach (var explosion in _explosionPrefabs)
        {
            PoolManager.WarmPool(explosion, 10);
        }
        foreach (var hit in _groundHitPrefabs)
        {
            PoolManager.WarmPool(hit, 10);
        }
    }
    
    public void SpawnExplosion(Vector3 position)
    {
        StartCoroutine(SpawnExplosionCoroutine(position));
    }
    
    public void SpawnGroundHit(Vector3 position)
    {
        StartCoroutine(SpawnGroundHitCoroutine(position));
    }
    
    private IEnumerator SpawnExplosionCoroutine(Vector3 position)
    {
        var explosion = PoolManager.SpawnObject(_explosionPrefabs[Random.Range(0, _explosionPrefabs.Count)], position, Quaternion.identity);
        var particle = explosion.GetComponent<ParticleSystem>();
        particle.Play();
        yield return new WaitUntil(() => !particle.isPlaying);
        PoolManager.ReleaseObject(explosion);
    }
    
    private IEnumerator SpawnGroundHitCoroutine(Vector3 position)
    {
        var hit = PoolManager.SpawnObject(_groundHitPrefabs[Random.Range(0, _groundHitPrefabs.Count)], position, Quaternion.identity);
        var particle = hit.GetComponent<ParticleSystem>();
        particle.Play();
        yield return new WaitUntil(() => !particle.isPlaying);
        PoolManager.ReleaseObject(hit);
    }
}
