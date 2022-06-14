using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IE.RSB;

public class Shot : MonoBehaviour
{
    public BulletProperties myBulletProperties;

    public ObjectPooler defaultHitPooler;

    // Start is called before the first frame update
    void Start()
    {
        SniperAndBallisticsSystem.instance.ActivateBullet(myBulletProperties);
    }


    private void OnEnable()
    {
        SniperAndBallisticsSystem.EAnyHit += OnAnyHit;

    }

    private void OnDisable()
    {
        SniperAndBallisticsSystem.EAnyHit -= OnAnyHit;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SniperAndBallisticsSystem.instance.FireBallisticsBullet(myBulletProperties);
        }
    }

    private void OnAnyHit(BulletPoint point)
    {
        Debug.Log("Hit Position: " + point.m_endPoint + " Hit Type: " + point.m_hitType);
        GameObject hitParticle = defaultHitPooler.GetPooledObject();
        hitParticle.transform.position = point.m_endPoint;
        hitParticle.transform.rotation = Quaternion.LookRotation(point.m_hitNormal);
        hitParticle.SetActive(true);
    }
}
