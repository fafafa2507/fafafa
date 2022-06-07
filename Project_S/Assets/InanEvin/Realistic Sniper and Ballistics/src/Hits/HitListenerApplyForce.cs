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

using System.Collections.Generic;
using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Listens to bullet hit events, and if the hit object contains a rigidbody, applies a force to it immediately based on the bullet's
    /// kinetic energy and a custom multiplier.
    /// </summary>
	public class HitListenerApplyForce : MonoBehaviour
	{
        
        [SerializeField] private float m_addForceMultiplier = 2.0f;

        private List<KeyValuePair<Rigidbody, Vector3>> m_rigidbodies = new List<KeyValuePair<Rigidbody, Vector3>>();

		private void OnEnable()
        {
            SniperAndBallisticsSystem.EAnyHit += OnAnyHit;
        }

		private void OnDisable()
        {
            SniperAndBallisticsSystem.EAnyHit -= OnAnyHit;
        }

        private void OnAnyHit(BulletPoint point)
        {
            Rigidbody hitBody = point.m_hitTransform.GetComponent<Rigidbody>();

            // If the body exists add it and the force to be applied to a list.
            if (hitBody != null)
            {
                m_rigidbodies.Add(new KeyValuePair<Rigidbody, Vector3>(hitBody, point.m_direction * Mathf.Sqrt(Mathf.Sqrt(point.m_kineticEnergy)) * m_addForceMultiplier));
            }
        }
        

        /// <summary>
        /// In late update, we iterate through the list of rigidbodies that will be applied force, then apply the recorded amounts of force & clear the list.
        /// The reason for doing this like that, instead of doing it on OnAnyHit function, is that there can be some functions that enable kinematic rigidbodies
        /// when their respective objects are hit. For instance, DemoEnemy.cs script listens to hit events, and if the body parts are hit, it makes the parts kinematic.
        /// However, since the order of calls in events are not guaranteed, if we were to apply force to the hit body part in OnAnyHit function in this script, it might
        /// be the case those body parts were still kinematic, thus no force would have been applied. (This is because we can't know if the OnAnyHit function in DemoEnemy.cs will get
        /// called before the OnAnyHit function in here, it's non-deterministic)
        /// So in here, when we receive a hit, we delegate it's apply force logic to the late update, making sure that any other script who wants to enable kinematic bodies are done
        /// with their job of listening to events first.
        /// </summary>
        private void LateUpdate()
        {
            // Iterate & apply force if nonkinematic
            for(int i =0; i < m_rigidbodies.Count; i++)
            {
                if (m_rigidbodies[i].Key.isKinematic) continue;
                m_rigidbodies[i].Key.AddForce(m_rigidbodies[i].Value, ForceMode.Impulse);
            }

            // Clear list.
            if (m_rigidbodies.Count != 0)
                m_rigidbodies.Clear();
        }

	}

}
