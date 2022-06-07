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
    /// Most basic bullet time camera logic. You cand duplicate this class & extend upon it to write your own bullet time camera effect,
    /// if you don't want to use the automatic tool BulletTimeCameraDefault.
    /// The idea is that we listen to the events coming from SniperAndBallisticsSystem. These events tell us whether a bullet time has started,
    /// it's updated, bullet finished it's path or whether the bullet time is ended. By acting on these events, we can enable a new camera
    /// & code our behaviour to track the bullet object.
    /// </summary>
    public class BulletTimeCameraEmpty : MonoBehaviour
    {
        // Exposed properties.
        [SerializeField] private Camera m_camera = null;
        [SerializeField] private float m_timeToWaitBeforeResettingTimescale = 1.0f;
        [SerializeField] private float m_timeToWaitBeforeEnding = 1.0f;

        // Private class members.
        private Transform m_bullet = null;
#pragma warning disable
        private Transform m_hitTarget = null;
        private Transform m_cameraTransform = null;
        private List<BulletPoint> m_bulletPath = null;
#pragma warning restore

        void Awake()
        {
            // Get camera transform
            m_cameraTransform = m_camera.transform;

            // Disable camera by default
            m_camera.enabled = false;
        }

        private void OnEnable()
        {
            SniperAndBallisticsSystem.EBulletTimeStarted += OnBulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeUpdated += OnBulletTimeUpdated;
            SniperAndBallisticsSystem.EBulletTimePathFinished += OnBulletTimePathFinished;
            SniperAndBallisticsSystem.EBulletTimeEnded += OnBulletTimeEnded;
        }

        private void OnDisable()
        {
            SniperAndBallisticsSystem.EBulletTimeStarted -= OnBulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeUpdated -= OnBulletTimeUpdated;
            SniperAndBallisticsSystem.EBulletTimePathFinished -= OnBulletTimePathFinished;
            SniperAndBallisticsSystem.EBulletTimeEnded -= OnBulletTimeEnded;
        }

        private void OnBulletTimeStarted(Transform bullet, Transform hitTarget, ref List<BulletPoint> bulletPath, float totalDistance)
        {
            // Save transforms.
            m_bullet = bullet;
            m_hitTarget = hitTarget;
            m_bulletPath = bulletPath;

            // Set the time to wait before resetting the timescale as well as before ending. After the bullet's path has finished
            // the system will first wait for 'time to reset timescale' amount of time, then set game's timescale back to 1.0,
            // afterwards it will wait 'time to end' amount of time before casting bullet time ended event.
            SniperAndBallisticsSystem.instance.BulletTimeWaitBeforeResettingTimescale = m_timeToWaitBeforeResettingTimescale;
            SniperAndBallisticsSystem.instance.BulletTimeWaitBeforeEnding = m_timeToWaitBeforeEnding;

            // Use virtual timescale to play with the bullet's timescale.
            // DO NOT, change the actual timescale, as it will be handled internally.
            // BulletTimeUtility.SetVirtualTimescale(0.1f);

            // We started the bullet time so we can activate the camera.
            m_camera.enabled = true;
        }

        private void OnBulletTimeUpdated(float distanceTravelled, float totalDistance)
        {
            // Do checks depending on how far the bullet has travelled.
        }

        private void OnBulletTimePathFinished()
        {
            // Bullet's path has finished, do it here if you'd like to cast effects, sounds etc.
        }

        private void OnBulletTimeEnded()
        {
            // Reset everything & disable the camera.
            m_bullet = null;
            m_hitTarget = null;
            m_bulletPath = null;
            m_camera.enabled = false;
        }

        private void Update()
        {
            // If we are active.
            if(m_bullet)
            {
                // Write your look at logic for the camera here.
                // If you do any kind of interpolation, remember to use Time.unscaledDeltaTime instead of Time.deltaTime, as the Time.timeScale will be extremely low
                // during bullet time effect.
            }
        }

        private void LateUpdate()
        {
            // If we are active
            if(m_bullet)
            {
                // Write your movement logic for the camera here.
                // Be advised, as the bullet objects will be moving extremely fast, common interpolation techniques might fail.
                // The best structure to follow the bullet is to use something like:
                // m_cameraTransform = m_bullet.position + m_bullet.rotation * offset;
                // offset being the offset position you want the camera to stay away from the bullet.
                // Also if you do any kind of interpolation, remember to use Time.unscaledDeltaTime instead of Time.deltaTime, as the Time.timeScale will be extremely low
                // during bullet time effect.
            }
        }
    }

}
