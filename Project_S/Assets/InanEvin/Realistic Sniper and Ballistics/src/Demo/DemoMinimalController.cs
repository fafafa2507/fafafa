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
	/// Demo script demonstrating the minimal usage of ballistics simulation in RSB:
	/// </summary>
	public class DemoMinimalController : MonoBehaviour
	{
		[SerializeField] private BulletProperties m_properties = null;		// Bullet properties to simulate
		[SerializeField] private Transform m_bulletTimeTransform = null;	// If bullet time is used, bullet will start from this transform. You can set it to e.g barrel of a gun.
		[SerializeField] private Transform m_bulletTimeBullet = null;		// Bullet object that will be shown during bullet time effects.

		private void Update()
		{
			// Fire a bullet if space is pressed.
			if(Input.GetKeyDown(KeyCode.Space))
            {
				SniperAndBallisticsSystem.instance.FireBallisticsBullet(m_properties, m_bulletTimeTransform, m_bulletTimeBullet);
            }
		}
	}

}
