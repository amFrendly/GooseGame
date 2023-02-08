using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private float fireRate = .1f;
    [SerializeField] private BulletData bulletData;
    [SerializeField] private float spread = 1f;
    [SerializeField] private Transform bulletSpawn;
    private ParticleSystem muzzleFlash;
    private float fireTime;
    
    // Start is called before the first frame update
    void Start()
    {
        muzzleFlash = GetComponentInChildren<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        fireTime += Time.deltaTime;
        if(fireRate < fireTime && Input.GetMouseButton(0))
        {
            fireTime = 0;
            FireBullet();
            muzzleFlash.Emit(1);
        }
    }

    void FireBullet()
    {
        float randomNumberX = Random.Range(-spread, spread);
        float randomNumberY = Random.Range(-spread, spread);
        float randomNumberZ = Random.Range(-spread, spread);
        Quaternion rotation = Quaternion.Euler(randomNumberX, randomNumberY, randomNumberZ);

        BulletManager.Instance.SpawnBullet(bulletData, bulletSpawn.position, bulletSpawn.rotation * rotation);
        //Bullet bullet = BulletManager.Instance.SpawnBullet();
        //bullet.transform.Rotate(randomNumberX, randomNumberY, randomNumberZ);
        //bullet.Init(bulletData, bulletSpawn.position, bulletSpawn.rotation * rotation);
        
    }
}
