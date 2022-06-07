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
    /// Attach this to any game-object if you want it to trigger bullet time effects when it's hit.
    /// Remember the chance to trigger the effect depends on the chance % in SniperAndBallisticsSystem. 
    /// </summary>
    public class BulletTimeTarget : MonoBehaviour
    {
        public bool IsActive { get { return m_isActive; } }
        private bool m_isActive = true;
 
        public void SetActivation(bool activate)
        {
            m_isActive = activate;
        }

  

    }

}