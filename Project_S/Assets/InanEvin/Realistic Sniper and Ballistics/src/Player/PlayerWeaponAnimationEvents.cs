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
	/// Weapon controller needs to when animations are finished, or when to eject shells, play audio etc.
	/// This script is attached to the demo player's weapon animator to notify the controller of such events.
	/// The methods are called via the animation's keyframe events.
	/// </summary>
	public class PlayerWeaponAnimationEvents : MonoBehaviour
	{
		[SerializeField] private PlayerWeaponController m_weaponController = null;

		public void PlayBoltSFX()
        {
			m_weaponController.PlayBoltSFX();
        }

		public void EjectShell()
        {
			m_weaponController.EjectShell();
		}

		public void PlayMagEjectSFX()
        {
			m_weaponController.PlayMagEjectSFX();
        }

		public void PlayMagInsertSFX()
        {
			m_weaponController.PlayMagInsertSFX();
        }
	}

}
