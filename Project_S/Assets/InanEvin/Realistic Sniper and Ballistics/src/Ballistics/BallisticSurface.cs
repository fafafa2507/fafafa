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
    /// Represents a penetratable/ricochet-able object. Attach this to any game object with a collider in your scene, setup the parameters on inspector
    /// and that object will be treated as a ballistic surface, meaning bullets will check for penetration and ricochet based on it's parameters.
    /// </summary>
    public class BallisticSurface : MonoBehaviour
    {
#pragma warning disable

        public enum SurfacePreset { Custom, Wood, HardWood, Metal, Steel, LightSteel, Plastic, Cloth };

        // Member variables.
        [SerializeField] private SniperAndBallisticsSystem.MeasurementUnits m_measurementUnit = SniperAndBallisticsSystem.MeasurementUnits.Metric;
        [SerializeField] private SniperAndBallisticsSystem.MeasurementUnits m_previousMeasurementUnit = SniperAndBallisticsSystem.MeasurementUnits.Metric;
        [SerializeField] private SurfacePreset m_preset = SurfacePreset.Custom;
        [SerializeField] private SurfacePreset m_previousPreset = SurfacePreset.Custom;

        public bool m_penetrationEnabled = true;
        public float m_penetrationEnergyConsumptionPercent = 0.5f;
        public float m_minEnergyToPenetrateInMetrics = 500;
        [MinMaxSlider(0.0f, 70.0f)]
        public Vector2 m_penetrationDeflectionAngles = Vector2.zero;

        public bool m_ricochetEnabled = true;
        public float m_ricochetEnergyConsumptionPercent = 0.5f;
        public float m_minEnergyToRicochetInMetrics = 500;
        [MinMaxSlider(0.0f, 70.0f)]
        public Vector2 m_ricochetDeflectionAngles = Vector2.zero;

    }

}
