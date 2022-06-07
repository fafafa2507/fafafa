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
    /// Deactivates the object after a specified time has passed, resets the rigidbody if there is one attached to the object.
    /// </summary>
    public class ObjectDeactivator : MonoBehaviour
    {
        [SerializeField] private float m_time = 2.0f;

        private Rigidbody m_rigidbody = null;

        void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            Invoke("Deactivate", m_time);
        }

        private void OnDisable()
        {
            CancelInvoke("Deactivate");
            gameObject.SetActive(false);

            if (m_rigidbody != null)
            {
                m_rigidbody.velocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void Deactivate()
        {
            transform.parent = null;
            gameObject.SetActive(false);
        }
    }

}
