using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IE.RSB;

public class Shot : MonoBehaviour
{
    //public BulletProperties myBulletProperties;

    public ObjectPooler defaultHitPooler;

    //public Transform myBulletTimeObject;
    //public Transform mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        //SniperAndBallisticsSystem.instance.ActivateBullet(myBulletProperties);
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
        /*
        if (Input.GetMouseButtonDown(0))
        {
            SniperAndBallisticsSystem.instance.FireBallisticsBullet(myBulletProperties, mainCamera, myBulletTimeObject);
        }

        // Cycle zero up
        if (Input.GetKeyDown(KeyCode.C))
        {
            SniperAndBallisticsSystem.instance.CycleZeroDistanceUp();
            Debug.Log(SniperAndBallisticsSystem.instance.CurrentZeroDistance);
        }

        // Cycle zero down
        if (Input.GetKeyDown(KeyCode.V))
        {
            SniperAndBallisticsSystem.instance.CycleZeroDistanceDown();
            Debug.Log(SniperAndBallisticsSystem.instance.CurrentZeroDistance);
        }


        if (Input.GetMouseButtonDown(1))
        {
            DynamicScopeSystem.instance.ScopeActivation(true, myBulletProperties);
        }

        // Disable scope
        if (Input.GetMouseButtonUp(1))
        {
            DynamicScopeSystem.instance.ScopeActivation(false, myBulletProperties);
        }
        */
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
