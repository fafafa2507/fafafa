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
	/// Gets the trail renderer attached to the object & clears it on disable.
	/// Used on tracers.
	/// </summary>
	public class TrailRendererReset : MonoBehaviour
	{
		private TrailRenderer m_trail = null;
		void Awake()
        {
			m_trail = GetComponent<TrailRenderer>();
        }

		void OnDisable()
        {
			m_trail.Clear();
        }
	}

}
