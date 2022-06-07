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

using System.Collections.Generic;
using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Scriptable object defining a bullet. You can right-click anywhere in the project and go to RBS > BulletProperty to create a new 
    /// asset instance of this class. In order to fire a ballistic bullet, you'd need a reference to a bullet property.
    /// So in short, each new bullet property asset represents a different bullet type. E.g you can refer to different property assets in different
    /// ammo pick-ups in an fps game, where your systems will change the current property according to the picked up ammo or the weapon that player currently
    /// has.
    /// </summary>
    [CreateAssetMenu(fileName = "BulletProperty", menuName = "RSB/BulletProperties", order = 1)]
    public class BulletProperties : ScriptableObject
    {
#pragma warning disable

        // Preset enum.
        public enum BulletPresets { PistolSMG9x19mm, PistolSMG38ACP, PistolSMG40SW, PistolSMG45ACP, Revolver38Special, Revolver357Magnum, LongRifle22, Rifle223Rem, Rifle762x39Soviet, Rifle556NATO, SR338Magnum, SR762x51NATO, SR7mmRemMagnum, SR50BMG, Custom };
     
        // Exposed member variables.
        [SerializeField] private BulletPresets m_bulletPreset = BulletPresets.PistolSMG9x19mm;
        [SerializeField] private BulletPresets m_previousBulletPreset = BulletPresets.PistolSMG9x19mm;
        [SerializeField] private SniperAndBallisticsSystem.MeasurementUnits m_measurementUnit = SniperAndBallisticsSystem.MeasurementUnits.Metric;
        [SerializeField] private SniperAndBallisticsSystem.MeasurementUnits m_previousMeasurementUnit = SniperAndBallisticsSystem.MeasurementUnits.Metric;
        [SerializeField] private float m_lifeTime = 3.0f;
        [SerializeField] private BallisticsUtility.DragGModel m_dragModel = BallisticsUtility.DragGModel.G1;
        [SerializeField] private float m_muzzleVelocity = 500.0f;
        [SerializeField] private float m_mass = 12.0f;
        [SerializeField] private float m_diameter = 9.0f;
        [SerializeField] private float m_length = 30.0f;
        [SerializeField] private float m_barrelTwist = 254;
        [SerializeField] private float m_ballisticCoef = 0.295f;
        [SerializeField] private bool m_affectedByWind = true;
        [SerializeField] private bool m_affectedByAirResistance = true;
        [SerializeField] private bool m_affectedBySpinDrift = true;

        // Metrics are used primarily.
        [SerializeField] private float m_muzzleVelocityInMetric = 500.0f;
        [SerializeField] private float m_massInMetric = 16.0f;
        [SerializeField] private float m_diameterInMetric = 9.0f;
        [SerializeField] private float m_lengthInMetric = 29.69f;
        [SerializeField] private float m_barrelTwistInMetric = 254;

#pragma warning enable

        /// <summary>
        /// Used internally by BulletProperties. In order to handle zero range calculations, or predict how much a bullet drop at which distance etc.
        /// It is possible to ask a BulletProperty to pre-calculate a trajectory information. This is done internally by SniperAndBallisticsSystem.cs.
        /// This class is used to represent previous calculations, so we can store them in a list & have a quick-access.
        /// </summary>
        public class BulletTrajectoryInfo
        {
            public Vector3 m_position = Vector3.zero;
            public float m_time = 0.0f;
            public float m_givenZeroDistance = 0.0f;
            public float m_calculatedZeroAngle = 0.0f;
        }

        private const float DEFAULT_STEPTIME = 0.01f;
        [SerializeField] private List<BulletTrajectoryInfo> m_calculatedTrajectoryInfo = new List<BulletTrajectoryInfo>();

        /// <summary>
        /// Internal usage only, should not be used by the public API.
        /// </summary>
        public bool m_zeroCalculated = false;


        // Public properties.
        public BallisticsUtility.DragGModel DragModel { get { return m_dragModel; } }
        public float MuzzleVelocity { get { return m_muzzleVelocityInMetric; } }
        public float Mass { get { return m_massInMetric; } }
        public float Diameter { get { return m_diameterInMetric; } }
        public float Length { get { return m_lengthInMetric; } }
        public float BarrelTwist { get { return m_barrelTwistInMetric; } }
        public float StepTime { get { return DEFAULT_STEPTIME; } }
        public float LifeTime { get { return m_lifeTime; } }
        public float BallisticCoefficient { get { return m_ballisticCoef; } }
        public bool UseWind { get { return m_affectedByWind; } }
        public bool UseAirResistance { get { return m_affectedByAirResistance; } }
        public bool UseSpinDrift { get { return m_affectedBySpinDrift; } }
        public bool IsZeroCalculated { get { return m_zeroCalculated; } }

        // For editor only
        [SerializeField] private bool m_ballisticSimulationFoldout = false;
        [SerializeField] private int m_horizontalRange = 500;
        [SerializeField] private int m_verticalRange = 100;
        [SerializeField] private int m_shootingAngle = 0;
        [SerializeField] private float m_timeShown = 0.0f;

        /// <summary>
        /// For editor only, should not be used by the public API.
        /// </summary>
        public EnvironmentProperties m_environmentProperties = null;

        /// <summary>
        /// Returns the required angle for zeroing the shot at 'distance'. Directly looks up to the calcualed trajectories list,
        /// so before calling this method CalculateTrajectoryForDistance must have been called for corresponding distance.
        /// SniperAndBallisticsSystem handles this, so it's not necessary for call any functions here from the user-side.
        /// </summary>
        /// <param name="distance">Distance to get the zero angle of.</param>
        /// <returns></returns>
        public float GetZeroAngleAtDistance(float distance)
        {
            int foundIndex = m_calculatedTrajectoryInfo.FindIndex(o => o.m_position.z == distance);
            if (foundIndex != -1) return m_calculatedTrajectoryInfo[foundIndex].m_calculatedZeroAngle;
            else return 0.0f;
        }

        /// <summary>
        /// Pre-calculates a bullet trajectory based on a given distance, filling information such as traveltime, bullet drop, required zero angle etc.
        /// Used by SniperAndBallisticsSystem.cs, should not be used by the users unless customization is desired.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="fireDirection"></param>
        /// <param name="zeroDistance"></param>
        /// <param name="dynamicCalculation"></param>
        /// <returns></returns>
        public BulletTrajectoryInfo CalculateTrajectoryForDistance(float distance, Vector3 fireDirection, float zeroDistance = 0.0f, bool dynamicCalculation = false)
        {
            // Return if info is there.
            if (!dynamicCalculation)
            {
                int foundIndex = m_calculatedTrajectoryInfo.FindIndex(o => o.m_position.z == distance && o.m_givenZeroDistance == zeroDistance);
                if (foundIndex != -1) return m_calculatedTrajectoryInfo[foundIndex];
            }
       
            float travelTime = 0.0f;
            float calculationStepTime = DEFAULT_STEPTIME;
            // Vars needed for iterations & records.
            Vector3 position = Vector3.zero;

            // Get zero angle.
            float zeroAngle = zeroDistance == 0.0f ? 0.0f : GetZeroAngleAtDistance(zeroDistance);

            // Calculate velocity
            Vector3 velocity = new Vector3(0.0f, zeroAngle == 0.0f ? 0 : -MuzzleVelocity * Mathf.Sin(zeroAngle * Mathf.Deg2Rad), MuzzleVelocity);

            // Flatten fire direction
            Vector3 flatFireDirecion = fireDirection;
            flatFireDirecion.y = 0.0f;

            // Predetermine wind vector
            Vector3 gravityVector = BallisticsUtility.GetGravity(SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_gravityInMetric, calculationStepTime);
          
            // Z velocity average
            float totalZVelocity = 0.0f;
            int zVelocityCount = 0;

            while (true)
            {
                // Advance time.
                travelTime += calculationStepTime;

                // Gravity.
                velocity += gravityVector;

                // Drag factor.
                if (UseAirResistance)
                    velocity -= BallisticsUtility.GetDragVector(velocity, DragModel, BallisticCoefficient, calculationStepTime);

                position.x += velocity.x * calculationStepTime;
                position.y += velocity.y * calculationStepTime;
                position.z += velocity.z * calculationStepTime;

                // Record for average velocity.
                totalZVelocity += velocity.z;
                zVelocityCount++;

                if (position.z > distance)
                {
                    position.z = distance;

                    BulletTrajectoryInfo info = new BulletTrajectoryInfo();

                    // If given zero distance is 0.0, calculate the required zero angle for this distance.
                    if (zeroDistance == 0.0f)
                    {
                        // Calculate required zero angle to hit the target at the distance.
                        float a = position.y / ((totalZVelocity / (float)zVelocityCount) * travelTime);
                        float angle = Mathf.Asin(a) * Mathf.Rad2Deg;
                        info.m_calculatedZeroAngle = angle;
                    }

                    // Fill in info
                    info.m_givenZeroDistance = zeroDistance;
                    info.m_position = position;
                    info.m_time = travelTime;

                    if (!dynamicCalculation)
                        m_calculatedTrajectoryInfo.Add(info);

                    return info;
                }
            }
        }
    }
}
