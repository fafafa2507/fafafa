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
	/// Attached to the ejected bullet shell objects, simply applies force & torque to the object upon enabling them to fly them off.
	/// </summary>
	public class EjectedShell : MonoBehaviour
	{
		[SerializeField] private float m_force = 0.2f;
		[SerializeField] private Vector3 m_torque = Vector3.zero;

		private Rigidbody m_rigidbody = null;

		private void Awake()
        {
			m_rigidbody = GetComponent<Rigidbody>();
        }

		private void OnEnable()
        {
			m_rigidbody.AddForce(transform.right * m_force, ForceMode.Impulse);
		}

		private void FixedUpdate()
        {
			m_rigidbody.AddRelativeTorque(m_torque * Time.fixedDeltaTime);
		}
		
	}

}
