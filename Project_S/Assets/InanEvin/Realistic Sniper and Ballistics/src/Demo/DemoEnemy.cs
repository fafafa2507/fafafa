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
    /// Enemy class used in the demo scene, on the bullet time targets.
    /// Checks the hit event from SniperAndBallisticSystem. If there is any hit
    /// and if the hit is on one of the rigidbody parts of this object, then enable ragdoll pose & apply force to the corresponding part.
    /// Restores it's initial position after a delay.
    /// </summary>
    public class DemoEnemy : MonoBehaviour
    {
        /// <summary>
        /// When the enemy is hit, it's going to go into ragdoll mode. We will save the initial transform position & rotations in awake,
        /// store it in instances of this class, so that we can restore the initial orientation after a specific delay has passed when in ragdoll.
        /// </summary>
        public class TransformStamp
        {
            public Transform m_transform;
            public Vector3 m_position;
            public Quaternion m_rotation;
        }

        [SerializeField] private Animator m_animator = null;

        // Private class members.
        private Rigidbody[] m_ragdollBodies;
        private BulletTimeTarget[] m_btTargets;
        private List<TransformStamp> m_transformStamps = new List<TransformStamp>();
        private bool m_isDead = false;

        void Awake()
        {
            m_ragdollBodies = GetComponentsInChildren<Rigidbody>();
            m_btTargets = GetComponentsInChildren<BulletTimeTarget>();

            // Saves the current pose, meaning all the children transforms' position & rotation will be saved in a list of TransformStamps.
            SaveCurrentPose();

            // Disable bodies in awake.
            RagdollBodiesIsKinematic(true);
        }

        private void OnEnable()
        {
            SniperAndBallisticsSystem.EAnyHit += OnAnyHit;
        }

        private void OnDisable()
        {
            SniperAndBallisticsSystem.EAnyHit -= OnAnyHit;

            // Make sure restore pose invoke is not running & restore pose immediately.
            CancelInvoke("RestorePose");
            RestorePose();
        }

        private void OnAnyHit(BulletPoint point)
        {
            if (m_isDead) return;

            // Check if the bullet hit any of the ragdoll bodies.
            for (int i = 0; i < m_ragdollBodies.Length; i++)
            {
                // If yes, enable all ragdolls and set dead flag.
                if (point.m_hitTransform == m_ragdollBodies[i].transform)
                {
                    RagdollBodiesIsKinematic(false);

                    // Some of our rigidbody parts are bullet time targets, e.g when bullet hits head or torso bullet time might be triggered.
                    // But we don't want to trigger bullet time if the enemy is already dead, so deactivate those targets.
                    BulletTimeTargetsActivation(false);

                    // Animator & flag.
                    m_animator.enabled = false;
                    m_isDead = true;

                    // Restore our initial pose after 4 seconds.
                    Invoke("RestorePose", 4f);

                    break;
                }
            }
        }

        void RestorePose()
        {
            // Make bodies kinematic & enable bullet time target components in the body parts again so that we can trigger bullet time again.
            RagdollBodiesIsKinematic(true);
            BulletTimeTargetsActivation(true);

            for (int i = 0; i < m_transformStamps.Count; i++)
            {
                m_transformStamps[i].m_transform.localPosition = m_transformStamps[i].m_position;
                m_transformStamps[i].m_transform.localRotation = m_transformStamps[i].m_rotation;
            }

            m_animator.enabled = true;
            m_isDead = false;
        }


        private void SaveCurrentPose()
        {
            m_transformStamps.Clear();
            Transform[] allTransforms = GetComponentsInChildren<Transform>();

            for (int i = 0; i < allTransforms.Length; i++)
            {
                TransformStamp stamp = new TransformStamp();
                stamp.m_transform = allTransforms[i];
                stamp.m_position = allTransforms[i].localPosition;
                stamp.m_rotation = allTransforms[i].localRotation;
                m_transformStamps.Add(stamp);
            }

        }
        private void RagdollBodiesIsKinematic(bool activate)
        {
            for (int i = 0; i < m_ragdollBodies.Length; i++)
                m_ragdollBodies[i].isKinematic = activate;
        }

        private void BulletTimeTargetsActivation(bool activate)
        {
            for (int i = 0; i < m_btTargets.Length; i++)
                m_btTargets[i].SetActivation(activate);
        }
    }

}
