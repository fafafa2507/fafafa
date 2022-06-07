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

#pragma warning disable

namespace IE.RSB
{
    /// <summary>
    /// Represents the environment conditions, such as gravity, wind speed, temperature etc.
    /// SniperAndBallisticsSystem instance requires one EnvironmentProperties asset file to be referred to run. 
    /// You can create new & different properties by right-clicking anywhere on your project window and going RSB > EnvironmentProperty
    /// which will create a new asset file as an instance of this scriptable object. You can then use it in different scenes to define scene
    /// properties.
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentProperty", menuName = "RSB/EnvironmentProperties", order = 2)]
    public class EnvironmentProperties : ScriptableObject
    {
        // Exposed class members.
        [SerializeField] private SniperAndBallisticsSystem.MeasurementUnits m_measurementUnit = SniperAndBallisticsSystem.MeasurementUnits.Metric;
        [SerializeField] private SniperAndBallisticsSystem.MeasurementUnits m_previousMeasurementUnit = SniperAndBallisticsSystem.MeasurementUnits.Metric;
        [SerializeField] private float m_gravity = -9.81f;
        [SerializeField] private float m_airPressure = 101.325f;
        [SerializeField] private float m_temperature = 25.0f;
        [SerializeField] private Vector3 m_windSpeed = Vector3.zero;

        // Metrics are used primarily.
        public float m_gravityInMetric = -9.81f;
        public float m_airPressureInMetric = 101.325f;
        public float m_temperatureInMetric = 25.0f;
        public Vector3 m_windSpeedInMetric = Vector3.zero;

        // Public properties.
        public SniperAndBallisticsSystem.MeasurementUnits MeasurementUnit { get { return m_measurementUnit; } }
  
    }
}
