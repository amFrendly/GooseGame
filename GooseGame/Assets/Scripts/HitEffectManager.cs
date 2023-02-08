using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.VirtualTexturing;

public class HitEffectManager : MonoBehaviour
{
    [SerializeField] private List<ImpactEffect> impactEffects = new();
    private readonly Dictionary<Material, ObjectPool<ParticleSystem>> impacts = new();

    private static HitEffectManager instance; //singleton bad?
    public static HitEffectManager Instance => instance; 

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            foreach (ImpactEffect effect in impactEffects)
            {
                effect.Init(transform);
                impacts.Add(effect.Material, effect.GetPool());
                Debug.Log(effect.Material + " material added to dictionary");
            }
            transform.parent = null;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // just incase som lunatic changes it
            transform.localScale = Vector3.one;
        }
        else
        {
            Destroy(this);
        }
        
    }

    public void SpawnEffect(Vector3 position, Vector3 normal, Material material)
    {
        Debug.Log(material + " material");

        if (impacts.TryGetValue(material, out ObjectPool<ParticleSystem> pool))
        {
            ParticleSystem system = pool.Get();
            SpawnEffect(position, normal, system);
            StartCoroutine(ReturnToPool(pool, system));
        }
        else
        {
            pool = impactEffects[0].GetPool();
            ParticleSystem system = pool.Get();
            SpawnEffect(position, normal, system); // default
            StartCoroutine(ReturnToPool(pool, system));
        }
    }

    public void SpawnEffect(Vector3 position, Vector3 normal, ParticleSystem system)
    {
        system.transform.position = position;
        system.transform.forward = normal;
        system.Emit(1);
    }

    private IEnumerator ReturnToPool(ObjectPool<ParticleSystem> pool, ParticleSystem system)
    {
        yield return new WaitForSeconds(system.main.duration); //wait until the trail is done and disapreared...
        pool.Release(system);

    }

    [Serializable]
    internal class ImpactEffect
    {
        [SerializeField] private Material material;
        [SerializeField] private ParticleSystem ImpactPrefab;

        public Material Material => material;
        private ObjectPool<ParticleSystem> pariclePool;
        private Transform parent;

        internal ObjectPool<ParticleSystem> GetPool()
        {
            return pariclePool;
        }

        private ParticleSystem OnCreateFunc()
        {
            return Instantiate(ImpactPrefab, parent);
        }

        internal void Init(Transform parent)
        {
            this.parent = parent;
            pariclePool ??= new ObjectPool<ParticleSystem>(OnCreateFunc, pool => pool.gameObject.SetActive(true), pool => pool.gameObject.SetActive(false));
        }
    }
}
