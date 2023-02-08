using UnityEngine;
using UnityEngine.Pool;

public class BulletManager : MonoBehaviour
{

    [SerializeField] private Bullet bulletPrefab;
    //[SerializeField] private HitEffectManager hitEffectManager;

    private ObjectPool<Bullet> bulletPool;

    private static BulletManager instance; //singleton bad?
    public static BulletManager Instance => instance;

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            bulletPool = new ObjectPool<Bullet>(OnCreate, OnGet, OnRelease, ActionOnDestroy, true, 100);
            transform.parent = null;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // just incase som lunatic changes it
            transform.localScale = Vector3.one;
        }
        else
        {
            Destroy(this);
        }    
    }

    //OnDestroy was taken...
    private void ActionOnDestroy(Bullet bullet)
    {
        Destroy(bullet);
    }

    private Bullet OnCreate()
    {
        Bullet bullet = Instantiate(bulletPrefab, transform);
        bullet.SetPool(bulletPool);
        //bullet.SetEffectManager(hitEffectManager);
        return bullet;
    }

    private void OnGet(Bullet bullet)
    {
        //bullet.DoSomething();
    }
    private void OnRelease(Bullet bullet)
    {
        //bullet.ResetVelocities();
    }

    public void SpawnBullet(BulletData data, Vector3 position, Quaternion rotation)
    {
        bulletPool.Get().Init(data, position, rotation);
    }

    public Bullet SpawnBullet()
    {     
        return bulletPool.Get();
    }
}
