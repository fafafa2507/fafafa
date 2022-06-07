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
    /// You can use this class to automatically create new bullet time effects without any coding. Drag & drop this to an object and the component
    /// controls will let you define new effects, which you can use to track bullet or target object, move & rotate camera, play audio,
    /// adjust camera tilts, timescales, camera shakes & more.
    /// If you don't want to use this tool but want to see how a bullet time camera logic is coded, you can take a look at the BulletTimeCameraEmpty.cs
    /// which is an example of the most basic bullet time camera without any movement. You can use that class as a base to code any camera logic you want
    /// during bullet times.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BulletTimeCameraDefault : MonoBehaviour
    {
        /// <summary>
        /// Represents an effect. You can create multiple effects from the editor-UI in inspector, then select to always play one whenever
        /// a bullet time is triggered, or randomly play your active effects etc.
        /// </summary>
        [System.Serializable]
        public class CameraEffect
        {
            public string m_name = "";
            public bool m_enabled = true;
            public float m_startPathPercentage = 0.0f;      // Bullet will be placed on this percent of it's path when bullet time is triggered and this effect is used.
            public float m_timeToResetTimescale = 1.0f;
            public float m_timeToEndAfterReset = 1.0f;

            public List<CameraEffectStage> m_stages = new List<CameraEffectStage>();

            // For editor purposes
            public bool m_foldout = false;
            public string m_previousName = "";

            // Empty const.
            public CameraEffect()
            {

            }

            // Copy const.
            public CameraEffect(CameraEffect copy)
            {
                m_name = copy.m_name;
                m_enabled = copy.m_enabled;
                m_stages = new List<CameraEffectStage>();
                m_startPathPercentage = copy.m_startPathPercentage;
                m_timeToEndAfterReset = copy.m_timeToEndAfterReset;
                m_timeToResetTimescale = copy.m_timeToResetTimescale;
                for (int i = 0; i < copy.m_stages.Count; i++)
                    m_stages.Add(new CameraEffectStage(copy.m_stages[i]));
            }
        }

        /// <summary>
        /// A camera effect might have multiple stages. These stages represents the camera placement on different parts of the bullet's path.
        /// E.g - Start stage might look at the bullet leaving the barrel, with an extremely slow time. A Follow stage might track the bullet from
        /// behind with a much more normal-time. And a Final stage might show the target object dying/getting hit in slow-motion etc.
        /// </summary>
        [System.Serializable]
        public class CameraEffectStage
        {
            // General
            public string m_name = "Stage";
            public bool m_enabled = true;
            public bool m_playAudio = false;
            public AudioClip m_inClip = null;
            public AudioClip m_outClip = null;

            // When & how will the next stage get triggered?
            public enum TriggerNextMethod { Duration, BulletTravelDistance, DistanceToBulletBiggerThan, DistanceToBulletSmallerThan, DistanceToTargetSmallerThan, BulletPathPercentage, WhenTargetIsHit };
            public TriggerNextMethod m_triggerNextMethod = TriggerNextMethod.Duration;
            public float m_triggerNextDuration = 0.0f;
            public float m_triggerNextDistance = 0.0f;
            public float m_triggerNextBulletPathPercent = 0.0f;

            // Timescale.
            public bool m_timescaleConnectedToHitDistance = false;
            public float m_startTimescale = 0.0f;
            public float m_endTimescale = 0.0f;
            public bool m_interpolateTimescale = false;
            public float m_timescaleDuration = 0.0f;
            public AnimationCurve m_timescaleCurve;

            // Motion.
            public enum PositionType { OnBullet, OnTarget, OnPath };
            public bool m_continueFromPrevious = false;
            public PositionType m_positionType = PositionType.OnBullet;
            public float m_pathPercent = 0.5f;
            public Vector3 m_startPosition = Vector3.zero;
            public Vector3 m_endPosition = Vector3.zero;
            public bool m_randomizeStartPosition = false;
            public bool m_randomizeStartDirectionX = false;
            public bool m_randomizeStartDirectionY = false;
            public bool m_randomizeStartDirectionZ = false;
            public Vector3 m_minStartPosition = Vector3.zero;
            public Vector3 m_maxStartPosition = Vector3.zero;
            public bool m_randomizeEndPosition = false;
            public bool m_randomizeEndDirectionX = false;
            public bool m_randomizeEndDirectionY = false;
            public bool m_randomizeEndDirectionZ = false;
            public Vector3 m_minEndPosition = Vector3.zero;
            public Vector3 m_maxEndPosition = Vector3.zero;
            public bool m_interpolatePosition = false;
            public AnimationCurve m_positionCurve;
            public float m_positionDuration = 0.0f;

            // Look at
            public bool m_lookAtBullet = true;
            public float m_startLookSpeed = 10.0f;
            public float m_endLookSpeed = 0.0f;
            public float m_lookSpeedDuration = 0.0f;
            public bool m_interpolateLookSpeed = false;
            public AnimationCurve m_lookSpeedCurve;
            public Vector3 m_lookAtOffset = new Vector3(0, 0, 0);

            // Z Tilt
            public bool m_interpolateZTilt = false;
            public float m_startZTilt = 0.0f;
            public float m_zTiltSpeed = 0.0f;
            public float m_endZTilt = 0.0f;
            public float m_zTiltDuration = 0.0f;
            public AnimationCurve m_zTiltCurve;

            // Camera shake.
            public bool m_shakeEnabled = false;
            public Vector3 m_shakeAmount = new Vector3(0, 0, 0);
            public Vector3 m_shakeSpeed = new Vector3(0, 0, 0);
            public float m_shakeDuration = 1.0f;

            // For Editor
            public bool m_foldout = false;

            // Empty const.
            public CameraEffectStage()
            {

            }

            // Copy const.
            public CameraEffectStage(CameraEffectStage copy)
            {
                m_startTimescale = copy.m_startTimescale;
                m_endTimescale = copy.m_endTimescale;
                m_interpolateTimescale = copy.m_interpolateTimescale;
                m_startPosition = copy.m_startPosition;
                m_endPosition = copy.m_endPosition;
                m_interpolatePosition = copy.m_interpolatePosition;
                m_continueFromPrevious = copy.m_continueFromPrevious;
                m_startLookSpeed = copy.m_startLookSpeed;
                m_endLookSpeed = copy.m_endLookSpeed;
                m_interpolateLookSpeed = copy.m_interpolateLookSpeed;
                m_lookSpeedCurve = copy.m_lookSpeedCurve;
                m_lookSpeedDuration = copy.m_lookSpeedDuration;
                m_lookAtOffset = copy.m_lookAtOffset;
                m_shakeEnabled = copy.m_shakeEnabled;
                m_shakeAmount = copy.m_shakeAmount;
                m_shakeSpeed = copy.m_shakeSpeed;
                m_shakeDuration = copy.m_shakeDuration;
                m_pathPercent = copy.m_pathPercent;
                m_positionType = copy.m_positionType;
                m_lookAtBullet = copy.m_lookAtBullet;
                m_timescaleDuration = copy.m_timescaleDuration;
                m_timescaleCurve = copy.m_timescaleCurve;
                m_positionCurve = copy.m_positionCurve;
                m_positionDuration = copy.m_positionDuration;
                m_startZTilt = copy.m_startZTilt;
                m_endZTilt = copy.m_endZTilt;
                m_interpolateZTilt = copy.m_interpolateZTilt;
                m_zTiltDuration = copy.m_zTiltDuration;
                m_zTiltCurve = copy.m_zTiltCurve;
                m_randomizeEndPosition = copy.m_randomizeEndPosition;
                m_randomizeStartPosition = copy.m_randomizeStartPosition;
                m_randomizeStartDirectionX = copy.m_randomizeStartDirectionX;
                m_randomizeStartDirectionY = copy.m_randomizeStartDirectionY;
                m_randomizeStartDirectionZ = copy.m_randomizeStartDirectionZ;
                m_randomizeEndDirectionX = copy.m_randomizeEndDirectionX;
                m_randomizeEndDirectionY = copy.m_randomizeEndDirectionY;
                m_randomizeEndDirectionZ = copy.m_randomizeEndDirectionZ;
                m_minStartPosition = copy.m_minStartPosition;
                m_maxStartPosition = copy.m_maxStartPosition;
                m_minEndPosition = copy.m_minEndPosition;
                m_maxEndPosition = copy.m_maxEndPosition;
                m_zTiltSpeed = copy.m_zTiltSpeed;
                m_triggerNextBulletPathPercent = copy.m_triggerNextBulletPathPercent;
                m_triggerNextDistance = copy.m_triggerNextDistance;
                m_triggerNextDuration = copy.m_triggerNextDuration;
                m_triggerNextMethod = copy.m_triggerNextMethod;
                m_enabled = copy.m_enabled;
                m_timescaleConnectedToHitDistance = copy.m_timescaleConnectedToHitDistance;
                m_inClip = copy.m_inClip;
                m_outClip = copy.m_outClip;
                m_playAudio = copy.m_playAudio;
            }
        }

        // Public access

        /// <summary>
        /// Returns the index of the current selected bullet time effect. -1 if randomization is enabled.
        /// </summary>
        public int SelectedEffectIndex { get { if (m_randomizeEffects) return -1; else return m_selectedEffect; } }

        /// <summary>
        /// If true, each time a bullet time is triggered the effect to use will be randomized amongst available active ones.
        /// </summary>
        public bool RandomizeEffects { get { return m_randomizeEffects; } set { m_randomizeEffects = value; } }

        /// <summary>
        /// Returns the total number of camera effects currently in the list.
        /// </summary>
        public int CameraEffectsCount { get { return m_cameraEffects.Count; } }

        // Exposed class members.
        [SerializeField] private bool m_randomizeEffects = false;
        [SerializeField] private int m_selectedEffect = 0;
        [SerializeField] private List<CameraEffect> m_cameraEffects = new List<CameraEffect>();
        [SerializeField] private LayerMask m_collisionDetectionMask = new LayerMask();
        [SerializeField] private bool m_dontCheckCollisionsInSameHierarchy = false;
        [SerializeField] private float m_negligibleCollisionDistance = 0.1f;
        [SerializeField] private AudioSource m_audioSource = null;

        // Interpolation rotuines.
        private Coroutine m_timescaleInterpolation = null;
        private Coroutine m_positionInterpolationRoutine = null;
        private Coroutine m_lookSpeedInterpolation = null;
        private Coroutine m_zTiltInterpolation = null;
        private Coroutine m_cameraShakeRoutine = null;

        // Transforms
        private Transform m_cameraTransform = null;
        private Transform m_bullet = null;
        private Transform m_hitTarget = null;
        private Transform m_currentLookTarget = null;

        // Other internal class members.
        private Vector3 m_currentPositionOffset = Vector3.zero;
        private Vector3 m_currentPathPosition = Vector3.zero;
        private Vector3 m_currentLookOffset = Vector3.zero;
        private Vector3 m_cameraShakeEuler = Vector3.zero;
        private Vector3 m_lastKnownBulletPosition = Vector3.zero;
        private Quaternion m_lastKnownBulletRotation = Quaternion.identity;
        private Quaternion m_currentPathRotation = Quaternion.identity;
        private CameraEffectStage.PositionType m_currentPositionType = CameraEffectStage.PositionType.OnBullet;
        private CameraEffectStage.TriggerNextMethod m_currentTriggerNextMethod = CameraEffectStage.TriggerNextMethod.Duration;
        private CameraEffectStage.PositionType m_previousPositionType = CameraEffectStage.PositionType.OnBullet;
        private CameraEffectStage m_currentStage = null;
        private List<BulletPoint> m_currentPath = null;
        private Camera m_camera = null;

        private bool m_lateUpdateEnabled = false;
        private bool m_updateEnabled = false;
        private bool m_checkForNextStage = false;
        private bool m_bulletsPathFinished = false;
        private float m_currentLookSpeed = 0.0f;
        private float m_currentZTilt = 0.0f;
        private float m_usedZTilt = 0.0f;
        private float m_currentZTiltSpeed = 0.0f;
        private float m_currentTriggerNextPercentage = 0.0f;
        private float m_currentTravelNextDistance = 0.0f;
        private float m_currentTriggerNextMarkDistance = 0.0f;
        private float m_currentPathPercent = -1.0f;
        private float m_currentHitDistance = 0.0f;
        private int m_currentStageIndex = 0;


        private void Awake()
        {
            m_camera = GetComponent<Camera>();
            m_cameraTransform = m_camera.transform;

            for(int i = 0; i < m_cameraEffects.Count; i++)
            {
                if (!m_cameraEffects[i].m_enabled)
                    m_cameraEffects.RemoveAt(i);
            }
        }

        private void OnEnable()
        {
            SniperAndBallisticsSystem.EBulletTimeStarted += BulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeUpdated += BulletTimeUpdated;
            SniperAndBallisticsSystem.EBulletTimePathFinished += BulletTimePathFinished;
            SniperAndBallisticsSystem.EBulletTimeEnded += BulletTimeEnded;
        }

        private void OnDisable()
        {
            SniperAndBallisticsSystem.EBulletTimeStarted -= BulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeUpdated -= BulletTimeUpdated;
            SniperAndBallisticsSystem.EBulletTimePathFinished -= BulletTimePathFinished;
            SniperAndBallisticsSystem.EBulletTimeEnded -= BulletTimeEnded;
        }

        // Called via event.
        private void BulletTimeStarted(Transform bullet, Transform hitTarget, ref List<BulletPoint> bulletPath, float totalDistance)
        {
            if (m_cameraEffects.Count == 0) return;

            // Save transforms & hit distance
            m_bullet = bullet;
            m_hitTarget = hitTarget;
            m_currentHitDistance = totalDistance;

            // Save path
            m_currentPath = bulletPath;

            // Enable camera
            m_camera.enabled = true;

            // Select camera effect if randomization is enabled.
            if (m_randomizeEffects)
            {
                m_selectedEffect = Random.Range(0, m_cameraEffects.Count);
            }
            else
            {
                if (m_selectedEffect == -1 || m_cameraEffects[m_selectedEffect].m_stages.Count == 0)
                {
                    Debug.LogWarning("Selected effect is either -1 or it doesn't contain any stages. Effect:" + m_selectedEffect);
                    return;
                }
            }

            // If the bullet time effect shall start from any other point than the beginning
            // Setup the necessary variables to skip until that path is reached.
            if (m_cameraEffects[m_selectedEffect].m_startPathPercentage != 0.0f)
            {
                // Make sure bullet iterates to the target position right away.
                SniperAndBallisticsSystem.instance.BulletTimeSkipPoint = true;

                // Set current path percent to stop bullet skipping once reached
                m_currentPathPercent = m_cameraEffects[m_selectedEffect].m_startPathPercentage;
            }

            // Set times to wait before resetting the timescale as well as before ending the bullet time.
            SniperAndBallisticsSystem.instance.BulletTimeWaitBeforeResettingTimescale = m_cameraEffects[m_selectedEffect].m_timeToResetTimescale;
            SniperAndBallisticsSystem.instance.BulletTimeWaitBeforeEnding = m_cameraEffects[m_selectedEffect].m_timeToEndAfterReset;

            // Trigger effect.
            m_previousPositionType = m_cameraEffects[m_selectedEffect].m_stages[0].m_positionType;
            StartCoroutine(StartStage());

        }

        /// <summary>
        /// Changes the selected camera effect to the one given with the name. Throws error if not found.
        /// </summary>
        /// <param name="name">The name of the effect to change to.</param>
        public void ChangeSelectedEffect(string name)
        {
            int foundIndex = m_cameraEffects.FindLastIndex(o => o.m_name == name);

            if (foundIndex < 0)
            {
                Debug.LogError("Can not find the camera effect with the name " + name);
                return;
            }

            m_selectedEffect = foundIndex;
        }

        /// <summary>
        /// Changes the selected camera effect to the one with the given index. Throws error if not found.
        /// </summary>
        /// <param name="index">The index of the effect to change to.</param>
        public void ChangeSelectedEffect(int index)
        {
            if (index < 0 && index >= m_cameraEffects.Count)
            {
                Debug.LogError("Camera effect with the index " + index + " does not exists.");
                return;
            }

            m_selectedEffect = index;
        }

        /// <summary>
        /// Returns the name of the effect with the given index.
        /// </summary>
        /// <param name="index">Effect index to return the name of.</param>
        /// <returns></returns>
        public string GetEffectName(int index)
        {
            if (index < 0 && index >= m_cameraEffects.Count)
            {
                Debug.LogError("Camera effect with the index " + index + " does not exists.");
                return "";
            }

            return m_cameraEffects[index].m_name;
        }

        // Called via event.
        private void BulletTimeUpdated(float distanceTravelled, float totalDistance)
        {
            // If the current path percent > 0, it means current camera effect is basing it's position on the bullet path.
            // Which means the Bullet iteration will be skipped until this path is reached to start bullet time from this path percent.
            // We make sure the iteration is unskipped and will continue as normal once we reach it.
            if (m_currentPathPercent > 0.0f)
            {
                if (distanceTravelled / totalDistance > m_currentPathPercent)
                {
                    SniperAndBallisticsSystem.instance.BulletTimeSkipPoint = false;
                    m_currentPathPercent = -1.0f;
                }
            }

            // Check to trigger next stages.
            if (m_checkForNextStage)
            {
                if (m_currentTriggerNextMethod == CameraEffectStage.TriggerNextMethod.BulletPathPercentage)
                {
                    if (distanceTravelled / totalDistance > m_currentTriggerNextPercentage)
                    {
                        TriggerNextStage();
                        m_checkForNextStage = false;
                    }
                }
                else if (m_currentTriggerNextMethod == CameraEffectStage.TriggerNextMethod.BulletTravelDistance)
                {
                    if (m_currentTriggerNextMarkDistance == -1.0f)
                    {
                        m_currentTriggerNextMarkDistance = distanceTravelled;
                    }

                    if (distanceTravelled - m_currentTriggerNextMarkDistance > m_currentTravelNextDistance)
                    {
                        TriggerNextStage();
                        m_checkForNextStage = false;
                    }
                }
                else if (m_currentTriggerNextMethod == CameraEffectStage.TriggerNextMethod.DistanceToBulletBiggerThan)
                {
                    if (Vector3.Distance(m_cameraTransform.position, m_bullet.position) > m_currentTravelNextDistance)
                    {
                        TriggerNextStage();
                        m_checkForNextStage = false;
                    }
                }
                else if (m_currentTriggerNextMethod == CameraEffectStage.TriggerNextMethod.DistanceToBulletSmallerThan)
                {
                    if (Vector3.Distance(m_cameraTransform.position, m_bullet.position) < m_currentTravelNextDistance)
                    {
                        TriggerNextStage();
                        m_checkForNextStage = false;
                    }
                }
                else if (m_currentTriggerNextMethod == CameraEffectStage.TriggerNextMethod.DistanceToTargetSmallerThan)
                {
                    if (Vector3.Distance(m_cameraTransform.position, m_hitTarget.position) < m_currentTravelNextDistance)
                    {
                        TriggerNextStage();
                        m_checkForNextStage = false;
                    }
                }
            }
        }

        // Called via event.
        private void BulletTimePathFinished()
        {
            // It's possible that bullet's path might include points after bullet has penetrated the hit target
            // or ricocheted off the hit target. This is because we want to make sure the bullet does not disappear immediately in bullet time
            // if it would have not in non-bullet time. But we want to make sure that any camera following the bullet only does it so until the target
            // is hit, we don't care about where the bullet went off afterwards. 
            // This flag is used to understand that.
            m_bulletsPathFinished = true;
            m_lastKnownBulletPosition = m_bullet.position;
            m_lastKnownBulletRotation = m_bullet.rotation;

            if (m_currentTriggerNextMethod == CameraEffectStage.TriggerNextMethod.WhenTargetIsHit)
            {
                m_checkForNextStage = false;
                TriggerNextStage();
            }
        }

        protected virtual void BulletTimeEnded()
        {
            // Reset
            m_camera.enabled = false;
            StopAllCoroutines();
            ResetClassMembers();
        }

        private void Update()
        {
            if (m_updateEnabled)
            {
                // Find look point.
                Vector3 point = m_currentLookTarget.position + m_currentLookTarget.rotation * m_currentLookOffset;

                // Interpolate the z axis tilt
                m_usedZTilt = Mathf.Lerp(m_usedZTilt, m_currentZTilt, Time.unscaledTime * m_currentZTiltSpeed);

                // Get rotation to target.
                Vector3 viewVector = point - m_cameraTransform.position;
                Quaternion targetRotation = viewVector == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(viewVector);

                // Accompany z tilt & camera shake with the rotation.
                targetRotation.eulerAngles = new Vector3(
                    targetRotation.eulerAngles.x + m_cameraShakeEuler.x,
                    targetRotation.eulerAngles.y + m_cameraShakeEuler.y,
                    m_usedZTilt + m_cameraShakeEuler.z);

                // Interpolate towards the final rotation.
                m_cameraTransform.rotation = Quaternion.Slerp(m_cameraTransform.rotation, targetRotation, Time.unscaledDeltaTime * m_currentLookSpeed);
            }
        }

        private void LateUpdate()
        {
            if (m_lateUpdateEnabled)
            {
                Vector3 desiredPosition = Vector3.zero;

                if (m_currentPositionType == CameraEffectStage.PositionType.OnBullet)
                {
                    if (!m_bulletsPathFinished)
                        desiredPosition = m_bullet.position + m_bullet.rotation * m_currentPositionOffset;
                    else
                        desiredPosition = m_lastKnownBulletPosition + m_lastKnownBulletRotation * m_currentPositionOffset;
                }
                else if (m_currentPositionType == CameraEffectStage.PositionType.OnTarget)
                {
                    // Base position off hit target, but rotation off still on bullet.
                    desiredPosition = m_hitTarget.position + m_bullet.rotation * m_currentPositionOffset;
                }
                else if (m_currentPositionType == CameraEffectStage.PositionType.OnPath)
                {
                    // Base position off path point, but rotation off still on bullet.
                    desiredPosition = m_currentPathPosition + m_currentPathRotation * m_currentPositionOffset;
                }

                CheckForCollisions(m_currentStage.m_lookAtBullet ? m_bullet : m_hitTarget, ref desiredPosition);

                m_cameraTransform.position = desiredPosition;
            }
        }

        private IEnumerator StartStage()
        {
            // Get stage
            CameraEffectStage stage = m_cameraEffects[m_selectedEffect].m_stages[m_currentStageIndex];
            m_currentStage = stage;

            // Play audio
            if (stage.m_playAudio && stage.m_inClip != null)
                m_audioSource.PlayOneShot(stage.m_inClip);

            // Timescale.
            float finalStartTimescale = stage.m_startTimescale;
            if (stage.m_timescaleConnectedToHitDistance && m_currentHitDistance > 99.0f)
            {
                float multiplier = (m_currentHitDistance / 100.0f);
                finalStartTimescale *= multiplier;
                finalStartTimescale = Mathf.Clamp(finalStartTimescale, 0.0f, 1.0f);
            }
            BulletTimeUtility.SetVirtualTimescale(finalStartTimescale);


            if (stage.m_interpolateTimescale)
            {
                float finalEndTimescale = stage.m_endTimescale;

                if (stage.m_timescaleConnectedToHitDistance && m_currentHitDistance > 99.0f)
                {
                    float multiplier = (m_currentHitDistance / 100.0f);
                    finalEndTimescale *= multiplier;
                    finalEndTimescale = Mathf.Clamp(finalEndTimescale, 0.0f, 1.0f);
                }

                InterpolateTimescale(finalEndTimescale, stage.m_timescaleDuration, stage.m_timescaleCurve);
            }

            // Set positioning, this will allow late update to start placing the camera accordingly.
            if (!stage.m_continueFromPrevious)
            {
                m_currentPositionType = stage.m_positionType;

                m_currentPositionOffset = stage.m_randomizeStartPosition ?
                 BulletTimeUtility.GetRandomVector(stage.m_minStartPosition, stage.m_maxStartPosition)
                 : stage.m_startPosition;

                if (stage.m_randomizeStartPosition)
                {
                    if (stage.m_randomizeStartDirectionX)
                        m_currentPositionOffset.x *= BulletTimeUtility.GetRandomNegation();

                    if (stage.m_randomizeStartDirectionY)
                        m_currentPositionOffset.y *= BulletTimeUtility.GetRandomNegation();

                    if (stage.m_randomizeStartDirectionZ)
                        m_currentPositionOffset.z *= BulletTimeUtility.GetRandomNegation();
                }

                // If the current positioning type is bullet's path, extract the point according to the path percent.
                if (stage.m_positionType == CameraEffectStage.PositionType.OnPath)
                {
                    // Get required pos & rot.
                    int targetPoint = GetIndexOfPathPoint(ref m_currentPath, stage.m_pathPercent);
                    m_currentPathPosition = targetPoint == 1 ? m_currentPath[targetPoint].m_endPoint : m_currentPath[targetPoint].m_origin;
                    m_currentPathRotation = Quaternion.LookRotation(m_currentPath[targetPoint].m_direction);
                }
            }
            else
            {

                // Change the current offset so that we can continue exactly at the same place when position type is changed.
                if (m_previousPositionType != CameraEffectStage.PositionType.OnBullet && stage.m_positionType == CameraEffectStage.PositionType.OnBullet)
                {
                    m_currentPositionOffset = m_cameraTransform.position - m_bullet.position;
                }
                else if (m_previousPositionType != CameraEffectStage.PositionType.OnTarget && stage.m_positionType == CameraEffectStage.PositionType.OnTarget)
                    m_currentPositionOffset = m_cameraTransform.position - m_hitTarget.position;
                else if (m_previousPositionType != CameraEffectStage.PositionType.OnPath && stage.m_positionType == CameraEffectStage.PositionType.OnPath)
                {
                    int targetPoint = GetIndexOfPathPoint(ref m_currentPath, stage.m_pathPercent);
                    m_currentPathPosition = targetPoint == 1 ? m_currentPath[targetPoint].m_endPoint : m_currentPath[targetPoint].m_origin;
                    m_currentPathRotation = Quaternion.LookRotation(m_currentPath[targetPoint].m_direction);
                    m_currentPositionOffset = m_cameraTransform.position - m_currentPathPosition;
                }

                m_currentPositionType = stage.m_positionType;
                m_previousPositionType = m_currentPositionType;
            }
            if (m_positionInterpolationRoutine != null)
                StopCoroutine(m_positionInterpolationRoutine);

            // Start interpolating the position.
            if (stage.m_interpolatePosition)
            {
                Vector3 targetEnd = stage.m_randomizeEndPosition ?
                 BulletTimeUtility.GetRandomVector(stage.m_minEndPosition, stage.m_maxEndPosition) : stage.m_endPosition;

                if (stage.m_randomizeStartPosition)
                {
                    if (stage.m_randomizeEndDirectionX)
                        targetEnd.x *= BulletTimeUtility.GetRandomNegation();

                    if (stage.m_randomizeEndDirectionY)
                        targetEnd.y *= BulletTimeUtility.GetRandomNegation();

                    if (stage.m_randomizeEndDirectionZ)
                        targetEnd.z *= BulletTimeUtility.GetRandomNegation();
                }

                InterpolatePosition(targetEnd, stage.m_positionDuration, stage.m_positionCurve);
            }

            // Set look speed
            m_currentLookSpeed = stage.m_startLookSpeed;

            // Set look target.
            m_currentLookTarget = stage.m_lookAtBullet ? m_bullet : m_hitTarget;
            m_currentLookOffset = stage.m_lookAtOffset;

            // Start interpolating the look speed if desired.
            if (stage.m_interpolateLookSpeed)
                InterpolateLookSpeed(stage.m_endLookSpeed, stage.m_lookSpeedDuration, stage.m_lookSpeedCurve);

            // Set z tilt.
            m_currentZTilt = stage.m_startZTilt;
            m_currentZTiltSpeed = stage.m_zTiltSpeed;

            // Interpolate z tilt if desired.
            if (stage.m_interpolateZTilt)
                InterpolateZTilt(stage.m_endZTilt, stage.m_zTiltDuration, stage.m_zTiltCurve);

            // Trigger camera shake if desired.
            if (stage.m_shakeEnabled)
                CameraShake(stage.m_shakeAmount, stage.m_shakeSpeed, stage.m_shakeDuration);
            else
            {
                if (m_cameraShakeRoutine != null)
                    StopCoroutine(m_cameraShakeRoutine);
            }

            // Make sure updates are enabled
            m_lateUpdateEnabled = true;
            m_updateEnabled = true;

            yield return new WaitForEndOfFrame();

            if (stage.m_triggerNextMethod == CameraEffectStage.TriggerNextMethod.Duration)
            {
                StartCoroutine(WaitToTriggerNext(stage.m_triggerNextDuration));
            }
            else if (stage.m_triggerNextMethod == CameraEffectStage.TriggerNextMethod.BulletPathPercentage)
            {
                m_currentTriggerNextMethod = stage.m_triggerNextMethod;
                m_currentTriggerNextPercentage = stage.m_triggerNextBulletPathPercent;
                m_checkForNextStage = true;
            }
            else if (stage.m_triggerNextMethod == CameraEffectStage.TriggerNextMethod.BulletTravelDistance)
            {
                m_currentTriggerNextMethod = stage.m_triggerNextMethod;
                m_currentTriggerNextMarkDistance = -1.0f;
                m_currentTravelNextDistance = stage.m_triggerNextDistance;
                m_checkForNextStage = true;
            }
            else
            {
                m_currentTriggerNextMethod = stage.m_triggerNextMethod;
                m_currentTravelNextDistance = stage.m_triggerNextDistance;
                m_checkForNextStage = true;
            }
        }


        private void InterpolatePosition(Vector3 end, float duration, AnimationCurve curve)
        {
            if (m_positionInterpolationRoutine != null)
                StopCoroutine(m_positionInterpolationRoutine);

            m_positionInterpolationRoutine = StartCoroutine(InterpolatePositionRoutine(end, duration, curve));
        }

        private IEnumerator InterpolatePositionRoutine(Vector3 end, float duration, AnimationCurve curve)
        {
            float i = 0.0f;
            Vector3 current = m_currentPositionOffset;

            while (i < 1.0f)
            {
                i += Time.unscaledDeltaTime * 1.0f / duration;
                m_currentPositionOffset = Vector3.Lerp(current, end, curve.Evaluate(i));
                yield return null;
            }
        }

        private void InterpolateLookSpeed(float end, float duration, AnimationCurve curve)
        {
            if (m_lookSpeedInterpolation != null)
                StopCoroutine(m_lookSpeedInterpolation);

            m_lookSpeedInterpolation = StartCoroutine(InterpolateLookSpeedRoutine(end, duration, curve));
        }

        private IEnumerator InterpolateLookSpeedRoutine(float end, float duration, AnimationCurve curve)
        {
            float i = 0.0f;
            float current = m_currentLookSpeed;

            while (i < 1.0f)
            {
                i += Time.unscaledDeltaTime * 1.0f / duration;
                m_currentLookSpeed = Mathf.Lerp(current, end, curve.Evaluate(i));
                yield return null;
            }
        }

        private void InterpolateZTilt(float end, float duration, AnimationCurve curve)
        {
            if (m_zTiltInterpolation != null)
                StopCoroutine(m_zTiltInterpolation);

            m_zTiltInterpolation = StartCoroutine(InterpolateZTiltRoutine(end, duration, curve));
        }

        private IEnumerator InterpolateZTiltRoutine(float end, float duration, AnimationCurve curve)
        {
            float i = 0.0f;
            float current = m_currentZTilt;

            while (i < 1.0f)
            {
                i += Time.unscaledDeltaTime * 1.0f / duration;
                m_currentZTilt = Mathf.Lerp(current, end, curve.Evaluate(i));
                yield return null;
            }
        }
        private void InterpolateTimescale(float end, float duration, AnimationCurve curve)
        {
            if (m_timescaleInterpolation != null)
                StopCoroutine(m_timescaleInterpolation);

            m_timescaleInterpolation = StartCoroutine(InterpolateTimescaleRoutine(end, duration, curve));
        }

        private IEnumerator InterpolateTimescaleRoutine(float end, float duration, AnimationCurve curve)
        {
            float i = 0.0f;
            float current = SniperAndBallisticsSystem.instance.BulletTimeVirtualTimescale;
            float ts = current;

            while (i < 1.0f)
            {
                i += Time.unscaledDeltaTime * 1.0f / duration;
                ts = Mathf.Lerp(current, end, curve.Evaluate(i));
                BulletTimeUtility.SetVirtualTimescale(ts);
                yield return null;
            }
        }

        private void CameraShake(Vector3 amount, Vector3 speed, float duration)
        {
            if (m_cameraShakeRoutine != null)
                StopCoroutine(m_cameraShakeRoutine);

            m_cameraShakeRoutine = StartCoroutine(CameraShakeRoutine(amount, speed, duration));
        }

        private IEnumerator CameraShakeRoutine(Vector3 amount, Vector3 speed, float duration)
        {
            float i = 0.0f;
            float multiplier = 1.5f;
            while (i < 1.0f)
            {
                i += Time.unscaledDeltaTime * 1.0f / duration;
                multiplier = Mathf.Lerp(1.5f, 0.0f, i);
                m_cameraShakeEuler = new Vector3(Mathf.Sin(Time.unscaledTime * speed.x) * amount.x, Mathf.Sin(Time.unscaledTime * speed.y) * amount.y, Mathf.Sin(Time.unscaledTime * speed.z) * amount.z) * multiplier;
                yield return null;
            }

            while (m_cameraShakeEuler != Vector3.zero)
            {
                m_cameraShakeEuler = Vector3.MoveTowards(m_cameraShakeEuler, Vector3.zero, Time.unscaledDeltaTime * 3.0f);
                yield return null;
            }

            m_cameraShakeEuler = Vector3.zero;
        }

        private int GetIndexOfPathPoint(ref List<BulletPoint> path, float percentage)
        {

            int lastIndex = path.IndexOf(path.Find(o => o.m_isPointAfterTargetHit == true)) - 1;
            int targetIndex = lastIndex < 0 ? Mathf.FloorToInt((path.Count - 1) * percentage) : Mathf.FloorToInt(lastIndex * percentage);
            return targetIndex;
        }

        private IEnumerator WaitToTriggerNext(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            TriggerNextStage();
        }

        private void TriggerNextStage()
        {
            if (m_currentStage.m_playAudio && m_currentStage.m_outClip != null)
                m_audioSource.PlayOneShot(m_currentStage.m_outClip);

            m_currentStageIndex++;

            if (m_currentStageIndex >= m_cameraEffects[m_selectedEffect].m_stages.Count)
            {
                // We are done.
                m_currentStageIndex = 0;
            }
            else
            {
                if (m_cameraEffects[m_selectedEffect].m_stages[m_currentStageIndex].m_enabled)
                    StartCoroutine(StartStage());
                else
                    TriggerNextStage();
            }
        }

        protected void CheckForCollisions(Transform target, ref Vector3 desiredPosition)
        {

            Vector3 dir = desiredPosition - target.position;
            RaycastHit[] hits = Physics.RaycastAll(new Ray(target.position, dir), dir.magnitude, m_collisionDetectionMask);

            for (int i = 0; i < hits.Length; i++)
            {
                if (m_dontCheckCollisionsInSameHierarchy)
                {
                    if (hits[i].transform.root == target.root)
                        continue;
                }

                RaycastHit hit = hits[i];
                // If a hit is detected, bump transform's position towards the hit.
                if (hit.transform != target && hit.distance > m_negligibleCollisionDistance)
                {
                    desiredPosition = new Vector3(hit.point.x + hit.normal.x * 0.5f, desiredPosition.y, hit.point.z + hit.normal.z * 0.5f);
                }
            }
        }


        private void ResetClassMembers()
        {
            m_timescaleInterpolation = m_positionInterpolationRoutine = m_lookSpeedInterpolation = m_zTiltInterpolation = m_cameraShakeRoutine = null;
            m_bullet = m_hitTarget = m_currentLookTarget = null;
            m_currentPositionOffset = m_currentPathPosition = m_currentLookOffset = m_cameraShakeEuler = m_lastKnownBulletPosition = Vector3.zero;
            m_lastKnownBulletRotation = m_currentPathRotation = Quaternion.identity;
            m_currentPositionType = CameraEffectStage.PositionType.OnBullet;
            m_currentTriggerNextMethod = CameraEffectStage.TriggerNextMethod.Duration;
            m_previousPositionType = CameraEffectStage.PositionType.OnBullet;
            m_currentStage = null;
            m_currentPath = null;
            m_lateUpdateEnabled = m_updateEnabled = m_checkForNextStage = m_bulletsPathFinished = false;
            m_currentLookSpeed = m_currentZTilt = m_usedZTilt = m_currentZTiltSpeed = 0.0f;
            m_currentTriggerNextPercentage = m_currentTravelNextDistance = m_currentTriggerNextMarkDistance = m_currentPathPercent = 0.0f;
            m_currentHitDistance = 0.0f;
            m_currentStageIndex = 0;
        }

    }

}
