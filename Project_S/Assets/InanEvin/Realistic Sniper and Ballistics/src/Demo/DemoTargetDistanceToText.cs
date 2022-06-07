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
using UnityEngine.UI;

namespace IE.RSB
{
	/// <summary>
	/// Simply displays the distance to main camera in a text component. Used in far targets in the demo scene.
	/// </summary>
	public class DemoTargetDistanceToText : MonoBehaviour
	{
		private Text m_text = null;

		private void Awake()
        {
			m_text = GetComponent<Text>();
        }

		void Update()
		{
			if(m_text)
            {
				float distance = Vector3.Distance(transform.parent.position, Camera.main.transform.position);
				m_text.text = distance.ToString("F0") + " M";
            }
		}
	}

}
