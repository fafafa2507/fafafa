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
    /// Reaches out to the SniperAndBallisticsSystem instance & randomizes the current environment properties wind value.
    /// </summary>
    public class WindRandomizer : MonoBehaviour
    {
        [SerializeField] private Vector3 m_randomization = Vector3.zero;
        [SerializeField] private float m_randomizationSpeed = 5.0f;
        private float m_timer = 0.0f;
        private Vector3 m_originalWindSpeed = Vector3.zero;

        private void Start()
        {
            m_originalWindSpeed = SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric;
        }

        private void Update()
        {
            m_timer += Time.deltaTime;

            if (m_timer > 0.4f)
            {
                m_timer = 0.0f;

                SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric = m_originalWindSpeed +
                    new Vector3(
                        Mathf.Sin(Time.time * m_randomizationSpeed) * Random.Range(-m_randomization.x, m_randomization.x),
                        Mathf.Sin(Time.time * m_randomizationSpeed) * Random.Range(-m_randomization.y, m_randomization.y),
                        Mathf.Sin(Time.time * m_randomizationSpeed) * Random.Range(-m_randomization.z, m_randomization.z));
  
            }


        }
    }

}
