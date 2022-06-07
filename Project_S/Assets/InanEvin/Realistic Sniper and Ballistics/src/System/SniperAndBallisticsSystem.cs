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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Central class for Realistic Sniper and Ballistics System. There needs to be one game object in everyscene that has this class as a component.
    /// Since it's a singleton instance, it can be reached from anywhere in the code, you don't have to reference this object after you make sure you have
    /// one instance existing in the scene.
    /// 
    /// SniperAndBallisticsSystem is responsible for:
    /// - Adjusting the zero distances users can use throughout their games.
    /// - Cycling these zero distances.
    /// - Activating a bullet, this will pre-calculate some important values. It is advised to call ActivateBullet(BulletProperties yourBullet) in awake
    /// for all of the bullet assets you plan to use in the scene, so that their heavy calculations can be handled quickly in awake.
    /// - Firing a ballistics bullet, call FireBallisticsBullet method anywhere in your project to fire a bullet. 
    /// - Handling bullet time.
    /// 
    /// More info about this class can be found in the documentation.
    /// </summary>
    public class SniperAndBallisticsSystem : Singleton<SniperAndBallisticsSystem>
    {
        // Used in editor.
        public enum MeasurementUnits { Imperial, Metric };

        // Bullet hit events.
        public delegate void BulletHitEvent(BulletPoint bulletPoint);
        public static event BulletHitEvent EPenetrationInHit;                 // Called when bullet penetrates an object.
        public static event BulletHitEvent EPenetrationOutHit;                // Called when bullet exits the penetrated object.
        public static event BulletHitEvent ERicochetHit;                      // Called when bullet ricochets off a surface.
        public static event BulletHitEvent ENormalHit;                        // Called when bullet hits a surface, but does not penetrate or ricochet.
        public static event BulletHitEvent EAnyHit;                           // Called when any of the above is fired.

        // Bullet time events.
        public delegate void BulletTimeStartEvent(Transform bullet, Transform hitTarget, ref List<BulletPoint> bulletPath, float totalDistance);
        public delegate void BulletTimeUpdateEvent(float distanceTravelled, float totalDistance);
        public delegate void BulletTimeEndEvent();
        public static event BulletTimeStartEvent EBulletTimeStarted;          // Called when bullet time starts, e.g this is where one enables bullet time camera.
        public static event BulletTimeUpdateEvent EBulletTimeUpdated;         // Called each frame that bullet travels while in bullet time.
        public static event BulletTimeEndEvent EBulletTimePathFinished;       // Called when bullet in bullet time reaches to the hit target.
        public static event BulletTimeEndEvent EBulletTimeEnded;              // Called when whole bullet time interaction is finished, times are resetted etc.

#pragma warning disable

        // Bullet property events.
        public delegate void BulletPropertiesEvent(BulletProperties properties);
        public static event BulletPropertiesEvent EBulletActivated;           // Called when a bullet is activated through ActivateBullet method.

#pragma warning restore

        // Zero distance events.
        public delegate void ZeroDistanceEvent();
        public static event ZeroDistanceEvent EZeroDistanceChanged;           // Called when the current zero distance is changed.

        // Global Access.
        public float BulletTimeVirtualTimescale = 1.0f;                       // A virtual timescale, bullet time camera's should use this one instead of Time.timeScale as that will be set to extremely low amount during bullet time.
        public float BulletTimeWaitBeforeResettingTimescale = 1.0f;           // System will wait this amount of time before resetting the actual timescale back to 1 during bullet time.
        public float BulletTimeWaitBeforeEnding = 1.0f;                       // After the actual timescale is set to 1, the system will wait this amount of time to finalize bullet time.
        public bool BulletTimeSkipPoint = false;                              // If true, bullet will travel immediate along it's path without waiting, until set to false. Useful when e.g if you want to start bullet time effects from halfway through bullet's path.

        // Property access.

        public float CurrentZeroDistance { get { return m_zeroDistances[m_currentZeroDistanceIndex]; } }
        public bool BulletTimeRunning { get { return m_isBulletTimeRunning; } }
        public bool UseInGameTrajectory { get { return m_enableInGameTrajectoryDebug; } set { m_enableInGameTrajectoryDebug = value; } }
        public bool UseTracers { get { return m_tracersEnabled; } set { m_tracersEnabled = value; } }
        public bool UseBulletTime { get { return m_bulletTimeEnabled; } set { m_bulletTimeEnabled = value; } }
        public float BulletTimeChance { get { return m_btChance; } set { m_btChance = value; } }
        public Transform FireTransform { get { return m_fireTransform; }  set { m_fireTransform = value; } }

        public EnvironmentProperties GlobalEnvironmentProperties { get { return m_environmentProperties; } }

        // General.
        [SerializeField] private List<float> m_zeroDistances = new List<float>();
        [SerializeField] private LayerMask m_rayMask = new LayerMask();
        [SerializeField] private EnvironmentProperties m_environmentProperties = null;
        [SerializeField] private Transform m_fireTransform = null;

        // Bullet Time
        [SerializeField] private bool m_bulletTimeEnabled = false;
        [SerializeField] private float m_btChance = 1.0f;
        [SerializeField] private float m_btMinDistance = 90.0f;

        // Tracer
        [SerializeField] private bool m_tracersEnabled = true;
        [SerializeField] private ObjectPooler m_tracerPooler = null;
        [SerializeField] private float m_tracerChance = 1.0f;
        [SerializeField] private float m_enableTracerAfterTime = 0.1f;

        // Debug.
        [SerializeField] private bool m_drawBulletTrajectory = true;
        [SerializeField] private bool m_drawHits = true;
        [SerializeField] private float m_debugRaysDuration = 10.0f;

        // In-game Debug
        [SerializeField] private bool m_enableInGameTrajectoryDebug = false;
        [SerializeField] private float m_trajectoryRendererDisableAfter = 5.0f;
        [SerializeField] private ObjectPooler m_trajectoryRendererPooler = null;

        // Private class members.
        private const int MAX_ITERATION_AFTERBTIMETARGET = 5;
        private bool m_isBulletTimeRunning = false;
        private int m_currentZeroDistanceIndex = 0;

        /// <summary>
        /// During bullet time, we want everything to almost stop moving. We don't want the target to change it's position, or e.g AI to attack the player etc.
        /// That's why in bullet time, we set the actual timescale to really low value, like this one.
        /// </summary>
        private const float BT_GENERALTIMESCALE = 0.01f;

        protected override void Awake()
        {
            base.Awake();
            m_environmentProperties = Instantiate(m_environmentProperties);
        }

        /// <summary>
        /// Will switch to the next zero distance in the list & call event to nofity any listeners.
        /// </summary>
        public void CycleZeroDistanceUp()
        {
            m_currentZeroDistanceIndex++;

            if (m_currentZeroDistanceIndex >= m_zeroDistances.Count)
                m_currentZeroDistanceIndex = m_zeroDistances.Count - 1;
            else
            {
                if (EZeroDistanceChanged != null)
                    EZeroDistanceChanged.Invoke();
            }
        }

        /// <summary>
        /// Will switch to the previous zero distance in the list & call event to nofity any listeners.
        /// </summary>
        public void CycleZeroDistanceDown()
        {
            m_currentZeroDistanceIndex--;

            if (m_currentZeroDistanceIndex < 0)
                m_currentZeroDistanceIndex = 0;
            else
            {
                if (EZeroDistanceChanged != null)
                    EZeroDistanceChanged.Invoke();
            }
        }

        /// <summary>
        /// Pre-calculates various variables that will be used by the system for a bullet. Call this method for any bullet properties asset you plan to use in your scene,
        /// preferably somewhere in awake so everything is pre-calculated when the level is loading.
        /// NOTE: System will throw an error if you try to fire a bullet which is not activated.
        /// </summary>
        /// <param name="properties"></param>
        public void ActivateBullet(BulletProperties properties)
        {
            // Calculate zero angle & bullet drop at specified zero distances.
            for (int i = 0; i < m_zeroDistances.Count; i++)
                properties.CalculateTrajectoryForDistance(m_zeroDistances[i], Vector3.forward);

            properties.m_zeroCalculated = true;
        }

        /// <summary>
        /// Fires a bullet (starts raycasting) with given properties. This will start simulating the bullet's trajectory, penetrating, ricochetting, checking
        /// for bullet time etc. and casting relevant events for you to check what was hit.
        /// 
        /// The system will start raycasting using m_fireTransform's position in SniperAndBallisticsSystem instance. That variable usually refers to the main camera,
        /// as you'd want to hit where you are aiming at.
        /// </summary>
        /// <param name="properties">Bullet properties to be used in simulation. </param>
        /// <param name="bulletTimeTransform">When bullet time starts, bullet will come out of this transform. You can keep null if you are not using bullet time.</param>
        /// <param name="bulletTimeBullet">Bullet Transform that will be used in bullet time. You can leave null if you are not using bullet time.</param>
        public void FireBallisticsBullet(BulletProperties properties, Transform bulletTimeTransform = null, Transform bulletTimeBullet = null)
        {
            if (m_isBulletTimeRunning) return;

            if (!properties.m_zeroCalculated)
            {
                Debug.LogError("The bullet properties you are trying to shoot do not have it's zero distances calculated. Please use CalculateBulletZeroDistances method, preferably in Awake, to calculate the zero distances on your bullet properties object. See documentation for more details.");
                return;
            }

            bool checkForBulletTime = (bulletTimeBullet != null && m_bulletTimeEnabled) ? (EBulletTimeStarted != null && (Random.value > (1.0f - m_btChance))) : false;
            StartCoroutine(FireBallisticsBulletRoutine(properties, bulletTimeTransform, bulletTimeBullet, checkForBulletTime));
        }

        /// <summary>
        /// Actual simulation routine. Fires the bullet, starts casting rays and checks for hits, penetration, ricochet, bullet velocity & kinetic energy etc.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="bulletTimeFireTransform"></param>
        /// <param name="bulletTimeBullet"></param>
        /// <param name="checkForBulletTime"></param>
        /// <returns></returns>
        private IEnumerator FireBallisticsBulletRoutine(BulletProperties properties, Transform bulletTimeFireTransform, Transform bulletTimeBullet, bool checkForBulletTime)
        {
            float remainingTime = properties.LifeTime;
            float stepTime;
            float travelTime = 0.0f;

            // Get directional ray angle.
            Vector3 fireDirection = m_fireTransform.forward;
            float shootingAngle = m_fireTransform.eulerAngles.x;
            shootingAngle = fireDirection.y < 0 ? -shootingAngle : (360.0f - shootingAngle);
            float zeroAngle = CurrentZeroDistance == 0.0f ? 0.0f : properties.GetZeroAngleAtDistance(CurrentZeroDistance);

            // Calculate velocity & stability components.
            float v0x = properties.MuzzleVelocity * Mathf.Cos(shootingAngle * Mathf.Deg2Rad);
            float v0y = properties.MuzzleVelocity * Mathf.Sin(shootingAngle * Mathf.Deg2Rad) - properties.MuzzleVelocity * Mathf.Sin(zeroAngle * Mathf.Deg2Rad);
            double stability = BallisticsUtility.GetStability(properties.Length, properties.Diameter, properties.BarrelTwist, properties.Mass, properties.MuzzleVelocity,
                GlobalEnvironmentProperties.m_airPressureInMetric, GlobalEnvironmentProperties.m_temperatureInMetric);

            // Flatten fire direction.
            Vector3 flatFireDir = fireDirection;
            flatFireDir.y = 0.0f;
            Vector3 flatFireDirRight = Vector3.Cross(flatFireDir, new Vector3(0, -1, 0));
            flatFireDir.Normalize();

            // Vars needed for iterations & records.
            Vector3 position = m_fireTransform.position;
            Vector3 previousPosition = m_fireTransform.position;
            Vector3 velocity = new Vector3(0.0f, v0y, v0x);
            Vector3 previousDrift = Vector3.zero;

            // Predetermine wind & gravity vectors
            Vector3 gravityVector = BallisticsUtility.GetGravity(GlobalEnvironmentProperties.m_gravityInMetric, properties.StepTime);

            // Initial kinetic energy.
            float kineticEnergyInMetric = velocity.magnitude * velocity.magnitude * 0.5f * properties.Mass * 0.001f;

            // Penetration flag & entry point for penetration.
            BallisticSurface penetratingSurface = null;
            Vector3 penetratingSurfaceInPoint = Vector3.zero;

            // List for collecting points to use during bullet time.
            List<BulletPoint> bulletPoints = new List<BulletPoint>();

            // Flag to skip raycasting if necessary.
            bool skipNormalRay = false;

            // Found bullet time target
            BulletTimeTarget foundTarget = null;
            int iterationAfterTargetIsFound = 0;

            // Tracers setup
            bool useTracers = (!checkForBulletTime && m_tracersEnabled) ? (m_tracerChance == 1.0f || (m_tracerChance != 0.0f && Random.value > 1.0f - m_tracerChance)) : false;
            Transform tracerTransform = null;
            bool tracerHidden = false;
            bool willBreak = false;

            if (useTracers)
            {
                tracerTransform = m_tracerPooler.GetPooledObject().transform;

                if (m_enableTracerAfterTime == 0.0f)
                {
                    tracerTransform.position = m_fireTransform.position;
                    tracerTransform.rotation = Quaternion.LookRotation(m_fireTransform.forward);
                    tracerTransform.gameObject.SetActive(true);
                }
                else
                    tracerHidden = true;
            }


            // In-game trajectory rendering setup.
            int bulletTrajectoryPosition = 0;
            int bulletTrajectoryIndexCounter = 0;
            LineRenderer trajectoryRenderer = null;

            if (m_enableInGameTrajectoryDebug && !checkForBulletTime)
            {
                trajectoryRenderer = m_trajectoryRendererPooler.GetPooledObject().GetComponent<LineRenderer>();
                trajectoryRenderer.positionCount = 0;
                trajectoryRenderer.gameObject.SetActive(true);
            }

            // Begin main simulation loop.
            while (remainingTime > 0.0f)
            {
                // If we hit a bullet time target, we are going to break out of the loop & trigger bullet time.
                // However, let's iterate for MAX_ITERATION_AFTERBTIMETARGET amount of iterations more, so that we can also
                // display if the bullet's penetrated or ricocheted in bullet time after the target is hit.
                if (foundTarget && iterationAfterTargetIsFound >= MAX_ITERATION_AFTERBTIMETARGET)
                    break;
                else if (foundTarget)
                    iterationAfterTargetIsFound++;

                // Advance time.
                stepTime = remainingTime > properties.StepTime ? properties.StepTime : remainingTime;
                travelTime += stepTime;

                if (stepTime != properties.StepTime)
                {
                    // Re-calculate some vectors depending on new steptime.
                    gravityVector = BallisticsUtility.GetGravity(GlobalEnvironmentProperties.m_gravityInMetric, stepTime);
                }

                // Gravity.
                velocity += gravityVector;

                // Drag factor.
                if (properties.UseAirResistance)
                    velocity -= BallisticsUtility.GetDragVector(velocity, properties.DragModel, properties.BallisticCoefficient, stepTime);

                // Kinetic Energy
                kineticEnergyInMetric = BallisticsUtility.GetKineticEnergy(velocity, properties.Mass);

                // Apply velocity.
                position += flatFireDirRight * velocity.x * stepTime;
                position += Vector3.up * velocity.y * stepTime;
                position += flatFireDir * velocity.z * stepTime;

                if (properties.UseWind)
                    position += BallisticsUtility.GetWindVector(GlobalEnvironmentProperties.m_windSpeedInMetric, stepTime) * travelTime;

                // Add spin drift to position if desired.
                if (properties.UseSpinDrift)
                {
                    Vector3 spinDrift = BallisticsUtility.GetSpinDrift(ref previousDrift, stability, stepTime, travelTime);
                    position += flatFireDirRight * spinDrift.x * 2.0f;
                }


                // Extract ray origin & direction
                Vector3 rOrigin = previousPosition;
                Vector3 rDirection = position - previousPosition;
                Vector3 finalRayPosition = rOrigin + rDirection.normalized * rDirection.magnitude;

                // Trajectory debug.
                if (m_enableInGameTrajectoryDebug && trajectoryRenderer && !checkForBulletTime && bulletTrajectoryIndexCounter > 0)
                {
                    trajectoryRenderer.positionCount++;
                    trajectoryRenderer.SetPosition(bulletTrajectoryPosition, rOrigin);
                    bulletTrajectoryPosition++;
                }
                else if (m_enableInGameTrajectoryDebug)
                    bulletTrajectoryIndexCounter++;

                // Tracer setup.
                if (useTracers && tracerHidden)
                {
                    if (travelTime > m_enableTracerAfterTime)
                    {
                        tracerHidden = false;
                        tracerTransform.position = rOrigin;
                        tracerTransform.rotation = Quaternion.LookRotation(rDirection);
                        tracerTransform.gameObject.SetActive(true);
                    }
                }

                BulletPoint.PointHitType hitType = BulletPoint.PointHitType.None;
                RaycastHit hit = new RaycastHit();

                // If the bullet is currently penetrating.
                if (penetratingSurface != null)
                {
                    if (kineticEnergyInMetric > 0.0f)
                    {
                        // Check whether the bullet is out.
                        Vector3 penetrationBackCastDirection = (previousPosition - position) * 1.2f;
                        RaycastHit[] hits = Physics.RaycastAll(position, penetrationBackCastDirection.normalized, penetrationBackCastDirection.magnitude, m_rayMask);

                        for (int i = 0; i < hits.Length; i++)
                        {
                            if (hits[i].transform != penetratingSurface.transform) continue;

                            // We are out.
                            position = hits[i].point;

                            // Get deflection angles.
                            float xDeflect = (Random.value > 0.5f ? 1.0f : -1.0f) * Random.Range(penetratingSurface.m_penetrationDeflectionAngles.x, penetratingSurface.m_penetrationDeflectionAngles.y);
                            float yDeflect = (Random.value > 0.5f ? 1.0f : -1.0f) * Random.Range(penetratingSurface.m_penetrationDeflectionAngles.x, penetratingSurface.m_penetrationDeflectionAngles.y);
                            float zDeflect = (Random.value > 0.5f ? 1.0f : -1.0f) * Random.Range(penetratingSurface.m_penetrationDeflectionAngles.x, penetratingSurface.m_penetrationDeflectionAngles.y);

                            // Deflect
                            float magnitude = velocity.magnitude;
                            velocity += new Vector3(xDeflect, yDeflect, zDeflect);
                            velocity = Vector3.ClampMagnitude(velocity, magnitude);

                            // Iterate x times depending on the penetration distance
                            // On each iteration decrease the bullet's kinetic energy.
                            float penetratedDistance = Vector3.Distance(penetratingSurfaceInPoint, hits[i].point);
                            float distanceCounter = 0.0f;
                            float distanceIncrement = penetratedDistance > 0.5f ? 0.5f : penetratedDistance;
                            float currentKE = kineticEnergyInMetric;
                            float newKE = 0.0f;
                            while (distanceCounter < penetratedDistance)
                            {
                                newKE = currentKE * (1.0f - penetratingSurface.m_penetrationEnergyConsumptionPercent);
                                currentKE = newKE;
                                distanceCounter += distanceIncrement;
                            }

                            // Update the velocity & kinetic energy according to the new kinetic energy.
                            BallisticsUtility.UpdateKEAndVelocity(ref velocity, ref kineticEnergyInMetric, properties.Mass, newKE);

                            // If the kinetic energy is too low, don't register an out hit.
                            if (kineticEnergyInMetric < 0.5f)
                                kineticEnergyInMetric = 0.0f;
                            else
                            {
                                // Set hit parameters.
                                hitType = BulletPoint.PointHitType.PenetrationOut;
                                hit = hits[i];
                                rOrigin = penetratingSurfaceInPoint;
                            }
                           
                            // Set to skip normal ray.
                            skipNormalRay = true;

                            // Set flag.
                            penetratingSurface = null;
                            break;
                        }
                    }

                }


                // If we've just penetrated out of a surface, do not cast rays, wait for the next iteration (emulating the time it takes for the bullet to leave the
                // surface after last iteration)
                if (!skipNormalRay)
                {
                    if (Physics.Raycast(new Ray(rOrigin, rDirection.normalized), out hit, rDirection.magnitude, m_rayMask))
                    {
                        BallisticSurface surface = hit.transform.GetComponent<BallisticSurface>();

                        // Check for bullet time & whether final target was found.
                        if (checkForBulletTime && !foundTarget)
                        {
                            BulletTimeTarget btt = hit.transform.GetComponent<BulletTimeTarget>();
                            float dist = Vector3.Distance(m_fireTransform.position, hit.point);

                            if (btt && btt.IsActive && dist > m_btMinDistance)
                            {
                                // Set target as found.
                                foundTarget = btt;
                            }
                        }

                        // If we have hit a ballistics surface.
                        if (surface != null && penetratingSurface == null)
                        {
                            if (surface.m_penetrationEnabled && kineticEnergyInMetric > surface.m_minEnergyToPenetrateInMetrics)
                            {
                                // Set to penetrate.
                                penetratingSurface = surface;
                                penetratingSurfaceInPoint = hit.point;
                                position = hit.point + rDirection.normalized * 0.1f;
                                hitType = BulletPoint.PointHitType.PenetrationIn;
                            }
                            else if (surface.m_ricochetEnabled && kineticEnergyInMetric > surface.m_minEnergyToRicochetInMetrics)
                            {
                                // Set to ricochet.
                                hitType = BulletPoint.PointHitType.Ricochet;

                                // Update position.
                                position = hit.point - rDirection.normalized * 0.05f;

                                // Calculate new kinetic energy.
                                float newKE = kineticEnergyInMetric * (1.0f - surface.m_ricochetEnergyConsumptionPercent);
                                BallisticsUtility.UpdateKEAndVelocity(ref velocity, ref kineticEnergyInMetric, properties.Mass, newKE);

                                // Reflect.
                                flatFireDir = Vector3.Reflect(rDirection.normalized, hit.normal).normalized;
                                flatFireDirRight = Vector3.Cross(flatFireDir, new Vector3(0, -1, 0));

                                //// Get vectors to deflect
                                Vector3 right = Vector3.Cross(hit.normal, new Vector3(0, -1, 0)).normalized;
                                Vector3 up = Vector3.Cross(hit.normal, new Vector3(-1, 0, 0)).normalized;

                                // Get deflection angles.
                                float xDeflect = (Random.value > 0.5f ? 1.0f : -1.0f) * Random.Range(surface.m_ricochetDeflectionAngles.x, surface.m_ricochetDeflectionAngles.y);
                                float yDeflect = (Random.value > 0.5f ? 1.0f : -1.0f) * Random.Range(surface.m_ricochetDeflectionAngles.x, surface.m_ricochetDeflectionAngles.y);

                                // Deflect
                                flatFireDir = Vector3.Lerp(flatFireDir, xDeflect > 0 ? right : -right, Mathf.Abs(xDeflect / 90.0f));
                                flatFireDir = Vector3.Lerp(flatFireDir, yDeflect > 0 ? up : -up, Mathf.Abs(yDeflect / 90.0f));
                            }
                            else
                            {
                                // Can't penetrate or ricochet due to kinetic energy, apply normal hit.
                                hitType = BulletPoint.PointHitType.Normal;
                                willBreak = true;
                            }
                        }
                        else if (surface != null && surface != penetratingSurface)
                        {
                            // Edge case, meaning two penetratable objects overlap, should not happen.
                            Debug.LogWarning("Edge case detected: Two penetrating surfaces overlap, make sure your penetratable objects are not within each other.");
                        }
                        else // Hit on a surface that does not contain BallisticSurface script.
                        {
                            hitType = BulletPoint.PointHitType.Normal;
                            willBreak = true;
                        }

                        // Since we have a hit, we did not travel for rDirection.magnitude amount of distance, but rather hit.point - rOrigin amount of distance.
                        // Update step time accordingly.
                        stepTime = (hit.point - rOrigin).magnitude / (finalRayPosition - rOrigin).magnitude * stepTime;

                        // Update final point of this step.
                        finalRayPosition = hit.point;
                    }

#if UNITY_EDITOR
                    if (m_drawBulletTrajectory && !checkForBulletTime)
                        Debug.DrawRay(rOrigin, finalRayPosition - rOrigin, Color.red, m_debugRaysDuration);
#endif

                    // Instantly simulate everything to see if we'll start the bullet time if checkForBulletTime is true.
                    // Else, normally simulate, waitng for stepTime amount of time in between iterations.
                    if (!checkForBulletTime)
                    {
                        // Instead of waiting via WaitForSeconds, wait while simultaneously moving the tracer effect if tracers are to be used.
                        if (useTracers)
                        {
                            float timer = 0.0f;
                            Vector3 targetPosition = finalRayPosition;
                            while (timer < stepTime)
                            {
                                timer += Time.deltaTime;
                                float t = timer / stepTime;
                                tracerTransform.position = Vector3.Lerp(rOrigin, targetPosition, t);
                                yield return null;
                            }
                        }
                        else
                            yield return new WaitForSeconds(stepTime);
                    }
                }
                else
                    skipNormalRay = false;

                // Create a new bullet point based on this current iteration's results.
                // If we are checking for bullet time, add the point to the list, as if a bullet time target is found we'll use the list to
                // move a bullet object through the recorded path.
                // If we are not checking for bullet time, simply check for hit events.
                if (checkForBulletTime || hitType != BulletPoint.PointHitType.None)
                {
                    Vector3 hitPoint = hitType == BulletPoint.PointHitType.None ? (rOrigin + rDirection.normalized * rDirection.magnitude) : hit.point;
                    Vector3 hitDirection = hitType == BulletPoint.PointHitType.None ? rDirection : (hit.point - rOrigin);
                    BulletPoint bulletPoint = CreateNewBulletPoint(hit, hitType, rOrigin, hitPoint,
                        velocity, hitDirection, kineticEnergyInMetric, travelTime, iterationAfterTargetIsFound != 0);

                    if (checkForBulletTime)
                        bulletPoints.Add(bulletPoint);
                    else if (hitType != BulletPoint.PointHitType.None)
                        CheckForHitEvents(bulletPoint);
                }

                if (willBreak || kineticEnergyInMetric <= 0)
                    break;

                // Update current position for next iteration.
                previousPosition = position;
                remainingTime -= stepTime;
            }

            if (useTracers)
                tracerTransform.gameObject.SetActive(false);

            // Disable trajectory rendering if used.
            if (m_enableInGameTrajectoryDebug && trajectoryRenderer && !checkForBulletTime)
                StartCoroutine(DisableTrajectoryRenderer(trajectoryRenderer));

            // If this coroutine was started to check for bullet time, either start bullet time if target was found,
            // Or re-start this coroutine but with normal simulation this time.
            if (checkForBulletTime)
            {
                // Start either bullet time or normal simulation.
                if (foundTarget)
                    StartCoroutine(BulletTime(new List<BulletPoint>(bulletPoints), properties, bulletTimeFireTransform, bulletTimeBullet, foundTarget.transform));
                else
                    StartCoroutine(FireBallisticsBulletRoutine(properties, bulletTimeFireTransform, bulletTimeBullet, false));
            }


            // Clear.
            bulletPoints.Clear();
            bulletPoints = null;

        }

        private IEnumerator DisableTrajectoryRenderer(LineRenderer rend)
        {
            yield return new WaitForSeconds(m_trajectoryRendererDisableAfter);
            rend.positionCount = 0;
            rend.gameObject.SetActive(false);
        }

        /// <summary>
        /// Bullet time loop, this is where the bullet is moved throughout it's recorded trajectory.
        /// </summary>
        /// <param name="bulletPath"></param>
        /// <param name="properties"></param>
        /// <param name="bulletTimeFireTransform"></param>
        /// <param name="bulletTransform"></param>
        /// <param name="hitTarget"></param>
        /// <returns></returns>
        private IEnumerator BulletTime(List<BulletPoint> bulletPath, BulletProperties properties, Transform bulletTimeFireTransform, Transform bulletTransform, Transform hitTarget)
        {

            // Flag
            m_isBulletTimeRunning = true;

            // We don't want any objects to move, change position, or e.g AI to attack player etc. during bullet time.
            // So set the actual timescale to an extremely low value, making everything almost stop.
            BulletTimeUtility.SetActualTimescale(BT_GENERALTIMESCALE);

            // Enable bullet.
            bulletTransform.gameObject.SetActive(true);

            // Orient bullet.
            Vector3 viewVector = bulletPath[1].m_origin - bulletPath[0].m_origin;
            if (viewVector != Vector3.zero)
                bulletTransform.rotation = Quaternion.LookRotation(viewVector);
            bulletPath[0].m_origin = bulletTimeFireTransform.position;
            bulletTransform.position = bulletPath[0].m_origin;

            // Cast bullet time started event.
            if (EBulletTimeStarted != null)
                EBulletTimeStarted(bulletTransform, hitTarget, ref bulletPath, Vector3.Distance(hitTarget.position, bulletPath[0].m_origin));

            // Prepare for iteration.
            float distance, duration, j = 0.0f;
            Vector3 currentPosition = Vector3.zero;
            Vector3 nextPosition = Vector3.zero;
            bool firstIteration = true;
            float totalDistance = Vector3.Distance(hitTarget.position, bulletPath[0].m_origin);
            bool pathFinished = false;

            for (int i = 0; i < bulletPath.Count; i++)
            {
                // Setup target & current position, distance, duration
                nextPosition = bulletPath[i].m_origin + bulletPath[i].m_direction;
                distance = Vector3.Distance(bulletPath[i].m_origin, nextPosition);
                duration = distance / 360.0f; // Default muzzle velocity.
                currentPosition = bulletPath[i].m_origin;
                j = 0.0f;

                // Orient bullet towards its path.
                if (bulletPath[i].m_direction != Vector3.zero)
                    bulletTransform.rotation = Quaternion.LookRotation(bulletPath[i].m_direction);

#if UNITY_EDITOR
                if (m_drawBulletTrajectory)
                    Debug.DrawRay(bulletPath[i].m_origin, bulletPath[i].m_direction, Color.red, m_debugRaysDuration);
#endif

                // If skipping points is true, immediately set the bullet's position to next position and continue iteration without waiting.
                // This is useful if e.g some bullet time effects wants to start from the position where bullet is already halfway through it's path.
                // Thus those effects can set BulleTimeSkipPoint to true, and check bullet's travel distance to set it back to false.
                if (BulletTimeSkipPoint)
                {
                    bulletTransform.position = nextPosition;

                    // Cast bullet time update event.
                    if (EBulletTimeUpdated != null)
                    {
                        float distanceTravelled = Vector3.Distance(bulletPath[0].m_origin, bulletTransform.position);
                        EBulletTimeUpdated(distanceTravelled, totalDistance);
                    }
                }
                else
                {
                    // If we are to normally iterate, interpolate the bullet's position towards the next position over time.
                    while (j < 1.0f)
                    {
                        if (!firstIteration)
                            j += Time.unscaledDeltaTime * 1.0f / duration * BulletTimeVirtualTimescale;
                        else
                            firstIteration = false;

                        bulletTransform.position = Vector3.Lerp(currentPosition, nextPosition, j);

                        // Cast bullet time update event.
                        if (EBulletTimeUpdated != null)
                        {
                            float distanceTravelled = Vector3.Distance(bulletPath[0].m_origin, bulletTransform.position);
                            EBulletTimeUpdated(distanceTravelled, totalDistance);
                        }

                        yield return null;
                    }
                }

                // Finish bullet's path, this means bullet has reached the target hit object. Iterating might still continue,
                // e.g when bullet penetrates the target hit object.
                if (!pathFinished && Vector3.Distance(bulletTransform.position, hitTarget.position) < 1f)
                {
                    pathFinished = true;

                    // Call path finished event.
                    if (EBulletTimePathFinished != null)
                        EBulletTimePathFinished.Invoke();
                }

                // Cast hit events.
                CheckForHitEvents(bulletPath[i]);
                currentPosition = nextPosition;
            }

            // Disable bullet.
            bulletTransform.gameObject.SetActive(false);

            // Wait to reset timescale.
            yield return new WaitForSecondsRealtime(BulletTimeWaitBeforeResettingTimescale);
            BulletTimeUtility.ResetActualTimescale();

            // Wait to end bullet time.
            yield return new WaitForSecondsRealtime(BulletTimeWaitBeforeEnding);

            // Cast bullet time ended event.
            if (EBulletTimeEnded != null)
                EBulletTimeEnded();

            // Reset timescale
            BulletTimeUtility.ResetVirtualTimescale();
            m_isBulletTimeRunning = false;
        }

        /// <summary>
        /// Simply check the hit type and cast the relevant event.
        /// </summary>
        /// <param name="point"></param>
        private void CheckForHitEvents(BulletPoint point)
        {
            if (point.m_hitType == BulletPoint.PointHitType.PenetrationIn)
            {
                if (EPenetrationInHit != null)
                    EPenetrationInHit(point);

                if (EAnyHit != null)
                    EAnyHit(point);

#if UNITY_EDITOR
                if (m_drawHits)
                    Debug.DrawRay(point.m_endPoint, Vector3.up * 10, Color.blue, m_debugRaysDuration);
#endif
            }
            else if (point.m_hitType == BulletPoint.PointHitType.PenetrationOut)
            {

                if (EPenetrationOutHit != null)
                    EPenetrationOutHit(point);

                if (EAnyHit != null)
                    EAnyHit(point);

#if UNITY_EDITOR
                if (m_drawHits)
                    Debug.DrawRay(point.m_endPoint, Vector3.up * 10, Color.cyan, m_debugRaysDuration);
#endif
            }
            else if (point.m_hitType == BulletPoint.PointHitType.Ricochet)
            {

                if (ERicochetHit != null)
                    ERicochetHit(point);

                if (EAnyHit != null)
                    EAnyHit(point);

#if UNITY_EDITOR
                if (m_drawHits)
                    Debug.DrawRay(point.m_endPoint, Vector3.up * 10, Color.yellow, m_debugRaysDuration);
#endif
            }
            else if (point.m_hitType == BulletPoint.PointHitType.Normal)
            {
                if (ENormalHit != null)
                    ENormalHit(point);

                if (EAnyHit != null)
                    EAnyHit(point);

#if UNITY_EDITOR
                if (m_drawHits)
                    Debug.DrawRay(point.m_endPoint, Vector3.up * 10, Color.green, m_debugRaysDuration);
#endif
            }
        }
        private BulletPoint CreateNewBulletPoint(RaycastHit hit, BulletPoint.PointHitType hitType, Vector3 origin, Vector3 hitPoint, Vector3 velocity, Vector3 direction, float kineticEnergy, float travelTime, bool isPointAfterTargetHit)
        {
            BulletPoint btp = new BulletPoint();
            btp.m_hitType = hitType;
            btp.m_origin = origin;
            btp.m_endPoint = hitPoint;
            btp.m_hitTransform = hit.transform;
            btp.m_hitNormal = hit.normal;
            btp.m_kineticEnergy = kineticEnergy;
            btp.m_velocity = velocity;
            btp.m_direction = direction;
            btp.m_travelTime = travelTime;
            btp.m_isPointAfterTargetHit = isPointAfterTargetHit;
            return btp;
        }

    }

}
