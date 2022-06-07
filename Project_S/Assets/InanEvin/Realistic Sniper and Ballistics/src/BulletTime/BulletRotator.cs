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
    /// Simply rotates a bullet object on it's z axis based on Virtual Bullet Timescale in SniperAndBallisticsSystem.
    /// NOTE: Requires a SniperAndBallisticsSystem instance to exists in a scene, otherwise will send a null reference error.
    /// </summary>
    public class BulletRotator : MonoBehaviour
    {
        [SerializeField] private Transform m_visuals = null;
        [SerializeField] private float m_rotateSpeed = 500.0f;

        private void Update()
        {
            // Rotate by multiplying the speed with the Virtual Timescale.
            // This way when the virtual timescale slows down, it will make the rotation slow down as well, creating an actual time-slow illusion.
            // We use the virtual timescale because the actual time scale will be set to something like 0.01 and won't change at all during the bullet time.
            // More information on SniperAndBallisticsSystem.cs
            m_visuals.localEulerAngles += new Vector3(0, 0, -m_rotateSpeed * Time.unscaledDeltaTime * SniperAndBallisticsSystem.instance.BulletTimeVirtualTimescale);
        }
    }

}
