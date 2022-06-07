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
    /// In an FPS game, there are many motion vectors affecting the camera as well as the weapon.
    /// Both the camera and the weapon might have a sway depending on the mouse & keyboard inputs, they might have procedural recoil interpolations,
    /// shake effects or in the most basic form weapon might change it's local position based on whether the player is running, idling, aiming etc.
    /// This class is used to do all of those effects collectively. When attached to a game object, that object might create a procedural bobbing effect,
    /// input based sways, breath sways (mimicking sniper-scope sway due to character breath), or simply recoil. 
    /// In RSB demo player, it is used in two objects: One as a parent to the camera, to create bobbing, sways, recoil & breathing effects on the camera
    /// and one as a parent to the weapon to create bobbing, sways, recoil and positioning effects on the weapon.
    /// </summary>
    public class MotionController : MonoBehaviour
    {
        [Header("Bob Motion")]
        [SerializeField] private bool m_bobbingEnabled = true;
        [SerializeField] private float m_bobDamping = 0.1f;
        [SerializeField] private Vector3 m_walkBobAmount = new Vector3(0.3f, 0.6f, 0.0f);
        [SerializeField] private Vector3 m_walkBobSpeed = new Vector3(16.0f, 8.0f, 0.0f);
        [SerializeField] private Vector3 m_runBobAmount = new Vector3(0.4f, 0.8f, 0.4f);
        [SerializeField] private Vector3 m_runBobSpeed = new Vector3(16.0f, 9.0f, 9.0f);
        [SerializeField] private float m_resetSpeed = 6.0f;
        private Vector3 m_finalBob = Vector3.zero;
        private Vector3 m_bobVelocity = Vector3.zero;
        private Vector3 m_bobSmoothed = Vector3.zero;
        private PlayerMovementController.PlayerMotionState m_playerState = PlayerMovementController.PlayerMotionState.Idling;

        [Header("Key & Mouse Sway")]
        [SerializeField] private bool m_swayEnabled = true;
        [SerializeField] private Vector3 m_keyboardSway = Vector3.zero;
        [SerializeField] private Vector3 m_mouseSway = Vector3.zero;
        [SerializeField] private float m_swaySmooth = 0.02f;
        private Vector3 m_targetKeyMouseSway;
        private Vector3 m_keyMouseSwaySmoothed;
        private Vector3 m_keyMouseSwayVelocity;

        [Header("Recoil Motion")]
        [SerializeField] private bool m_recoilEnabled = true;
        [SerializeField] private Vector3 m_positionRecoil = Vector3.zero;
        [SerializeField] private float m_positionRandomizationFactor = 0.0f;
        [SerializeField] private float m_positionDuration = 0.5f;
        [SerializeField] private Vector3 m_rotationRecoil = Vector3.zero;
        [SerializeField] private float m_rotationRandomizationFactor = 0.0f;
        [SerializeField] private float m_rotationDuration = 0.5f;
        private Vector3 m_recoilFinalEuler = Vector3.zero;
        private Coroutine m_recoilRoutine = null;
        private Vector3 m_recoilFinalPosition = Vector3.zero;

        [Header("Breath Sway")]
        [SerializeField] private bool m_breathSwayEnabled = true;
        [SerializeField] private AudioSource m_breathSource = null;
        [SerializeField] private AudioClip m_breathInSFX = null;
        [SerializeField] private Vector3 m_breathSwayAmount = Vector3.zero;
        [SerializeField] private Vector3 m_breathSwaySpeed = Vector3.zero;
        [SerializeField] private float m_breathStabilizeSpeed = 2.0f;
        [SerializeField] private float m_breathMaxHoldTime = 3.0f;
        [SerializeField] private float m_catchBreathSpeed = 3.0f;
        [SerializeField] private float m_breathSwaySmoothSpeed = 1.0f;
        private bool m_breathSwayActive = false;
        private Vector3 m_targetBreathSway = Vector3.zero;
        private Vector3 m_targetBreathSwaySmoothed = Vector3.zero;
        private float m_breathSwaySmoothSpeedOriginal = 0.0f;
        private bool m_holdingBreath = false;
        private float m_breathHoldTimer = 0.0f;
        private float m_breathMultiplier = 1.0f;

        [Header("Positioner")]
        [SerializeField] private bool m_positionerEnabled = true;
        [SerializeField] private Vector3 m_aimedPosition = Vector3.zero;
        [SerializeField] private Vector3 m_aimedRotation = Vector3.zero;
        [SerializeField] private Vector3 m_runningPosition = Vector3.zero;
        [SerializeField] private Vector3 m_runningRotation = Vector3.zero;
        [SerializeField] private float m_toAimDuration = 0.25f;
        [SerializeField] private float m_toRunDuration = 0.25f;
        [SerializeField] private float m_toHipDuration = 0.25f;
        [SerializeField] private bool m_hideRenderersOnAim = false;
        [SerializeField] private bool m_enableRenderersBackOnBulletTime = false;
        private Coroutine m_changePositionRoutine = null;
        private Vector3 m_hipPosition = Vector3.zero;
        private Vector3 m_hipEuler = Vector3.zero;
        private Vector3 m_basePosition = Vector3.zero;
        private Vector3 m_baseEuler = Vector3.zero;
        private MeshRenderer[] m_meshRenderers;
        private SkinnedMeshRenderer[] m_skinRenderers;
        private bool m_renderersHidden = false;
        private bool m_wasRenderersHidden = false;

        private void Awake()
        {
            // Get original values.
            m_hipPosition = transform.localPosition;
            m_hipEuler = transform.localEulerAngles;
            m_basePosition = m_hipPosition;
            m_baseEuler = m_hipEuler;
            m_breathSwaySmoothSpeedOriginal = m_breathSwaySmoothSpeed;

            // If we are going to hide weapon renderers on aim, get those renderers in awake.
            if (m_hideRenderersOnAim)
            {
                m_meshRenderers = GetComponentsInChildren<MeshRenderer>();
                m_skinRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }
        }



        private void OnEnable()
        {
            PlayerMovementController.EPlayerStateChanged += OnPlayerStateChanged;
            SniperAndBallisticsSystem.EBulletTimeStarted += OnBulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeEnded += OnBulletTimeEnded;
        }

        private void OnDisable()
        {
            PlayerMovementController.EPlayerStateChanged -= OnPlayerStateChanged;
            SniperAndBallisticsSystem.EBulletTimeStarted += OnBulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeEnded += OnBulletTimeEnded;

        }

        // Save current player state.
        private void OnPlayerStateChanged(PlayerMovementController.PlayerMotionState state)
        {
            m_playerState = state;
        }

        private void Update()
        {
            // Checks bob motion based on player movement state.
            CheckForBob();

            // Checks sway motion based on keyboard & mouse input.
            CheckForKeyboardMouseSway();

            // Checks breath sway based on whether it's activated or not.
            CheckForBreathSway();

            // Apply the combination of all position + euler offsets to the transform.
            transform.localPosition = m_basePosition + m_recoilFinalPosition;
            transform.localRotation = Quaternion.Euler(m_baseEuler + m_bobSmoothed + m_keyMouseSwaySmoothed + m_targetBreathSwaySmoothed + m_recoilFinalEuler);
        }

        #region BobMotion

        private void CheckForBob()
        {
            if (!m_bobbingEnabled) return;

            if (m_playerState == PlayerMovementController.PlayerMotionState.Idling || m_playerState == PlayerMovementController.PlayerMotionState.OnAir)
            {
                // If we are idling or on air, reset bob value to zero.
                m_bobSmoothed = Vector3.Lerp(m_bobSmoothed, Vector3.zero, Time.deltaTime * m_resetSpeed);
            }
            else if (m_playerState == PlayerMovementController.PlayerMotionState.Walking)
            {
                // Create bob euler according to walk variables.
                m_finalBob = new Vector3(
               Mathf.Sin(Time.time * m_walkBobSpeed.x) * m_walkBobAmount.x,
               Mathf.Sin(Time.time * m_walkBobSpeed.y) * m_walkBobAmount.y,
               Mathf.Sin(Time.time * m_walkBobSpeed.z) * m_walkBobAmount.z);

            }
            else if (m_playerState == PlayerMovementController.PlayerMotionState.Running)
            {
                // Create bob euler according to run variables.
                m_finalBob = new Vector3(
                Mathf.Sin(Time.time * m_runBobSpeed.x) * m_runBobAmount.x,
                Mathf.Sin(Time.time * m_runBobSpeed.y) * m_runBobAmount.y,
                Mathf.Sin(Time.time * m_runBobSpeed.z) * m_runBobAmount.z);
            }

            // If player is moving interpolates towards the final bob.
            if (m_playerState != PlayerMovementController.PlayerMotionState.Idling || m_playerState != PlayerMovementController.PlayerMotionState.OnAir)
            {
                m_bobSmoothed = Vector3.SmoothDamp(m_bobSmoothed, m_finalBob, ref m_bobVelocity, m_bobDamping);
            }
        }

        #endregion

        #region KeyMouseSway

        private void CheckForKeyboardMouseSway()
        {
            if (!m_swayEnabled) return;

            // Simply create a target rotation based on key/mouse input & sway amounts/speeds.
            Vector2 moveInput = Vector2.zero;
            Vector2 lookInput = Vector2.zero;

            // If we are not in any mobile platforms set input,
            // Else we're gonna get it from DemoMobileControls
            // Which is supposed to be a joystick canvas in the scene.
#if !(!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
            moveInput = DemoMobileController.MoveInput;
            lookInput = DemoMobileController.LookInput;
#endif

            m_targetKeyMouseSway.x = moveInput.y * m_keyboardSway.x + lookInput.y * m_mouseSway.x;
            m_targetKeyMouseSway.z = -moveInput.x * m_keyboardSway.z + lookInput.x * m_mouseSway.z;
            m_targetKeyMouseSway.y = moveInput.x * m_keyboardSway.y + lookInput.x * m_mouseSway.y;
            m_keyMouseSwaySmoothed = Vector3.SmoothDamp(m_keyMouseSwaySmoothed, m_targetKeyMouseSway, ref m_keyMouseSwayVelocity, m_swaySmooth);
        }

#endregion

#region BreathSway

        private void CheckForBreathSway()
        {
            if (!m_breathSwayEnabled) return;

            if (m_breathSwayActive)
            {
                // First get a sway based on sin waves.
                m_targetBreathSway.x = Mathf.Sin(Time.time * m_breathSwaySpeed.x) * m_breathSwayAmount.x * m_breathMultiplier;
                m_targetBreathSway.y = Mathf.Sin(Time.time * m_breathSwaySpeed.y) * m_breathSwayAmount.y * m_breathMultiplier;
                m_targetBreathSway.z = Mathf.Sin(Time.time * m_breathSwaySpeed.z) * m_breathSwayAmount.z * m_breathMultiplier;

                // Toggle holdingBreath flag upon shift pressed.
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    m_holdingBreath = true;
                    m_breathSource.PlayOneShot(m_breathInSFX);
                }

                // Toggle holdingBreath flag to false upon shift released.
                if (Input.GetKeyUp(KeyCode.LeftShift))
                {
                    m_holdingBreath = false;
                    m_breathHoldTimer = 0.0f;
                }

               
                if (m_holdingBreath)
                {
                    m_breathHoldTimer += Time.deltaTime;

                    // Hold breath only until a maximum hold time is reached.
                    if (m_breathHoldTimer > m_breathMaxHoldTime)
                    {
                        m_breathHoldTimer = 0.0f;
                        m_holdingBreath = false;

                        // Exaggerate multiplier to make the sway more intense, since we've just released all our breath now.
                        m_breathMultiplier = 3.0f;
                        m_breathSwaySmoothSpeed = m_breathSwaySmoothSpeedOriginal;
                    }

                    // If we are holding our breath, interpolate the smooth speed to a very low value, this will make the sway almost disappear,
                    // stabilizing the euler motion.
                    m_breathSwaySmoothSpeed = Mathf.MoveTowards(m_breathSwaySmoothSpeed, 0.05f, Time.deltaTime * m_breathStabilizeSpeed);
                }

                // Meaning if we have just reached maximum breath hold time, released our breath and now in an exaggerated sway state,
                // interpolate the multiplier back to one.
                if (!m_holdingBreath && m_breathMultiplier > 1)
                {
                    m_breathMultiplier = Mathf.MoveTowards(m_breathMultiplier, 1.0f, Time.deltaTime * m_catchBreathSpeed);
                }

                // Smooth target sway.
                m_targetBreathSwaySmoothed = Vector3.Lerp(m_targetBreathSwaySmoothed, m_targetBreathSway, Time.deltaTime * m_breathSwaySmoothSpeed);
            }
            else
            {
                // If not active, make sure target sway stays in zero.
                if (m_targetBreathSway != Vector3.zero)
                {
                    m_targetBreathSway = Vector3.MoveTowards(m_targetBreathSway, Vector3.zero, Time.deltaTime * m_resetSpeed);
                }
            }
        }


        public void BreathSwayActivation(bool activate)
        {
            m_breathSwayActive = activate;

            if (activate)
            {
                m_breathSwaySmoothSpeed = m_breathSwaySmoothSpeedOriginal;
                m_breathMultiplier = 1.0f;
                m_breathHoldTimer = 0.0f;
                m_holdingBreath = false;
            }
        }

#endregion

#region Recoil

        /// <summary>
        /// Call this function to create a recoil effect on the object this script's attached to.
        /// Recoil parameters are set in inspector, this function will simply trigger the effect.
        /// </summary>
        public void Recoil()
        {
            if (!m_recoilEnabled) return;

            if (m_recoilRoutine != null)
                StopCoroutine(m_recoilRoutine);

            m_recoilRoutine = StartCoroutine(RecoilRoutine(true));
        }

        private IEnumerator RecoilRoutine(bool inwards)
        {
            float i = 0.0f;
            float j = 0.0f;
            bool positionRecoil = m_positionRecoil != Vector3.zero;
            bool rotationRecoil = m_rotationRecoil != Vector3.zero;

            Vector3 positionTarget = (positionRecoil && inwards) ? (m_positionRecoil + m_positionRecoil * Random.Range(-m_positionRandomizationFactor, m_positionRandomizationFactor)) : Vector3.zero;
            Vector3 rotationTarget = (rotationRecoil && inwards) ? (m_rotationRecoil + m_rotationRecoil * Random.Range(-m_rotationRandomizationFactor, m_rotationRandomizationFactor)) : Vector3.zero;

            Vector3 startPosition = m_recoilFinalPosition;
            Vector3 startEuler = m_recoilFinalEuler;

            // Interpolate towards the target position & euler.
            while ((positionRecoil && i < 1.0f) || (rotationRecoil && j < 1.0f))
            {
                if (positionRecoil)
                {
                    i += Time.deltaTime * 1.0f / m_positionDuration;
                    m_recoilFinalPosition = Vector3.Lerp(startPosition, positionTarget, i);
                }

                if (rotationRecoil)
                {
                    j += Time.deltaTime * 1.0f / m_rotationDuration;
                    m_recoilFinalEuler = Vector3.Lerp(startEuler, rotationTarget, j);
                }
                yield return null;
            }

            // Recoil is done in two stages, in and out, if this call was for the in stage, now call the out stage so we can get back to our
            // original position & rotation.
            if (inwards)
                m_recoilRoutine = StartCoroutine(RecoilRoutine(false));
        }

#endregion

#region Positioner

        /// <summary>
        /// Will take the object that this script's attached to it's aim position defined on the inspector.
        /// </summary>
        public void ToAimPosition()
        {
            if (!m_positionerEnabled) return;

            if (m_changePositionRoutine != null)
                StopCoroutine(m_changePositionRoutine);

            m_changePositionRoutine = StartCoroutine(ChangePositionRotationRoutine(m_hideRenderersOnAim, m_aimedPosition, m_aimedRotation, m_toAimDuration));

        }

        /// <summary>
        /// Will take the object that this script's attached to it's hip position defined on the inspector.
        /// </summary>
        public void ToHipPosition()
        {
            if (!m_positionerEnabled) return;

            if (m_changePositionRoutine != null)
                StopCoroutine(m_changePositionRoutine);

            m_changePositionRoutine = StartCoroutine(ChangePositionRotationRoutine(false, m_hipPosition, m_hipEuler, m_toHipDuration));


        }

        /// <summary>
        /// Will take the object that this script's attached to it's run position defined on the inspector.
        /// </summary>
        public void ToRunPosition()
        {
            if (!m_positionerEnabled) return;

            if (m_changePositionRoutine != null)
                StopCoroutine(m_changePositionRoutine);

            m_changePositionRoutine = StartCoroutine(ChangePositionRotationRoutine(false, m_runningPosition, m_runningRotation, m_toRunDuration));

        }

        /// <summary>
        /// Positioner might hide renderers e.g when aimed, but if the bullet time effect is triggered
        /// We might want to enable those renderers for the duration of the bullet time since the player, weapon etc. now will be visible from
        /// another camera.
        /// 
        /// This fuction enables those renderers back if thats the case.
        /// </summary>
        /// <param name="bullet"></param>
        /// <param name="hitTarget"></param>
        /// <param name="bulletPath"></param>
        /// <param name="totalDistance"></param>
        private void OnBulletTimeStarted(Transform bullet, Transform hitTarget, ref List<BulletPoint> bulletPath, float totalDistance)
        {
            if (m_renderersHidden && m_enableRenderersBackOnBulletTime)
            {
                m_wasRenderersHidden = true;
                HideRenderers(false);
            }
        }

        /// <summary>
        /// Positioner might hide renderers e.g when aimed, but if the bullet time effect is triggered
        /// We might want to enable those renderers for the duration of the bullet time since the player, weapon etc. now will be visible from
        /// another camera.
        /// 
        /// This fuction disables the renderers back if they were enabled due to bullet time.
        /// </summary>
        private void OnBulletTimeEnded()
        {
            if (m_wasRenderersHidden && m_enableRenderersBackOnBulletTime)
            {
                HideRenderers(true);
                m_wasRenderersHidden = false;
            }
        }

        private void HideRenderers(bool hide)
        {
            if (m_renderersHidden == hide) return;

            for (int i = 0; i < m_meshRenderers.Length; i++)
                m_meshRenderers[i].enabled = !hide;

            for (int i = 0; i < m_skinRenderers.Length; i++)
                m_skinRenderers[i].enabled = !hide;

            m_renderersHidden = hide;
        }
       
        private IEnumerator ChangePositionRotationRoutine(bool hideRenderers, Vector3 targetPosition, Vector3 targetEuler, float duration)
        {
            float i = 0.0f;
            Vector3 position = m_basePosition;
            Vector3 euler = m_baseEuler;


            if (!hideRenderers)
                HideRenderers(false);

            while (i < 1.0f)
            {
                i += Time.deltaTime * 1.0f / duration;
                m_basePosition = Vector3.Lerp(position, targetPosition, i);
                m_baseEuler = Vector3.Lerp(euler, targetEuler, i);
                yield return null;
            }

            if (hideRenderers)
                HideRenderers(true);

        }


#endregion
    }

  
}
