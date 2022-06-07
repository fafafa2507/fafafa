/*
 * Realistic Sniper and Ballistics System
 * Copyright (c) 2021, Inan Evin, Inc. All rights reserved.
 * Author: Inan Evin
 * https://www.inanevin.com/
 * 
 * Documentation: https://rsb.inanevin.com
 * Contact: inanevin@gmail.com
 * Support Discord: https://discord.gg/RCzpSAmBAb
 *
 * Feel free to ask about RSB, send feature recommendations or any other feedback!
 */

using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Listens to hit events and spawns particles according to the surface that was being hit.
    /// Normally one would check the surface's tag or layer to determine what type of a particle to spawn.
    /// However in RSB we did not want to mess with the user's project settings, so we did not introduced any tags or layers for the demo scene.
    /// Instead, we use HitTagBody, HitTagMetal, HitTagWood.cs scripts to determine what type of a surface a hit object has, and spawn particles accordingly.
    /// In your own project, you can simply write your own particle spawner and check for the surface's tags or layer depending on your project.
    /// </summary>
    public class HitListenerSpawnParticle : MonoBehaviour
    {
        // We used object poolers to spawn particles. These poolers will spawn their target particles
        // right in awake, and whenever we want to get a particle they will deliver us an available one.
        [SerializeField] private ObjectPooler m_defaultHitPooler = null;
        [SerializeField] private ObjectPooler m_metalHitPooler = null;
        [SerializeField] private ObjectPooler m_bodyHitPooler = null;
        [SerializeField] private ObjectPooler m_woodHitPooler = null;

        private void OnEnable()
        {
            SniperAndBallisticsSystem.EAnyHit += OnAnyHit;
        }

        private void OnDisable()
        {
            SniperAndBallisticsSystem.EAnyHit -= OnAnyHit;
        }

        /// <summary>
        /// Check what was being hit & spawn particle accordingly.
        /// </summary>
        /// <param name="point"></param>
        private void OnAnyHit(BulletPoint point)
        {
            GameObject pooled = null;
            bool willStick = false; // whether the spawned particle will be parented to the hit surface or not.

            if (point.m_hitTransform.GetComponent<HitTagMetal>())
            {
                pooled = m_metalHitPooler.GetPooledObject();
                willStick = true;
            }
            else if (point.m_hitTransform.GetComponent<HitTagWood>())
                pooled = m_woodHitPooler.GetPooledObject();
            else if (point.m_hitTransform.GetComponent<HitTagBody>())
                pooled = m_bodyHitPooler.GetPooledObject();
            else
                pooled = m_defaultHitPooler.GetPooledObject();

            pooled.transform.rotation = Quaternion.LookRotation(point.m_hitNormal);
            pooled.transform.position = point.m_endPoint;

            if (willStick)
                pooled.transform.parent = point.m_hitTransform;

            // Enable particle.
            pooled.SetActive(true);
        }
    }

}
