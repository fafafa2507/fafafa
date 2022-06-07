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
    /// Simple first person movement controller. Is able to walk, jump & sprint.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        /// <summary>
        /// When the player's motion state has changed, we can notify any interested script by using this event.
        /// </summary>
        /// <param name="state"></param>
        public delegate void PlayerStateActions(PlayerMovementController.PlayerMotionState state);
        public static event PlayerStateActions EPlayerStateChanged;

        public static PlayerMotionState PlayerState { get { return s_motionState; } }
        public enum PlayerMotionState
        {
            Idling,
            Walking,
            Running,
            OnAir
        }

        // Exposed properties.
        [Header("General")]
        [Tooltip("Minimum velocity to consider player as moving.")]
        [SerializeField] private float m_movementVelocityThreshold = 1.0f;

        [Header("Walking")]
        [Tooltip("Forward walking speed.")]
        [SerializeField] private float m_walkSpeedFwd = 4.0f;

        [Tooltip("Backwards walking speed.")]
        [SerializeField] private float m_walkSpeedBwd = 2.0f;

        [Tooltip("Horizontal walking speed.")]
        [SerializeField] private float m_walkSpeedHr = 3.0f;

        [Header("Running")]

        [Tooltip("Forward running speed.")]
        [SerializeField] private float m_runSpeedFwd = 12.0f;

        [Tooltip("Backwards running speed.")]
        [SerializeField] private float m_runSpeedBwd = 8.0f;

        [Tooltip("Horizontal running speed.")]
        [SerializeField] private float m_runSpeedHr = 11.0f;

        [Tooltip("Acceleration smooth that will be used to interpolate from walking to running speed.")]
        [SerializeField] private float m_accelerationSmooth = 0.5f;

        [Header("Jumping")]

        [Tooltip("Jump power.")]
        [SerializeField] private float m_jumpHeight = 5.0f;

        [Tooltip("Gravity applying to the controller.")]
        [SerializeField] private float m_gravity = -25.0f;

        [Header("Audio")]

        [Tooltip("All footstep sfx to be randomized while moving.")]
        [SerializeField] private AudioClip[] m_footstepSFX = new AudioClip[] { };

        [Tooltip("How fast footstep sfx will be played while walking?")]
        [SerializeField] private float m_footstepsRate = 0.5f;

        [Tooltip("How fast footstep sfx will be played while running?")]
        [SerializeField] private float m_footstepsRunRate = 0.3f;

        // Private class members.
        private AudioSource m_footstepSource = null;
        private CharacterController m_cc = null;
        private float m_jumpVelocity = 0.0f;
        private bool m_isGrounded = false;
        private bool m_isWalking = false;
        private bool m_isRunning = false;
        private float m_usedSpeed = 0.0f;
        private float m_accelerationVelocity = 0.0f;
        private bool m_sprintInput = false;
        private float m_velocityMagnitude = 0.0f;
        private float m_lastFootstep = 0.0f;
        private static PlayerMotionState s_motionState;
        private Vector2 m_movementInput = Vector2.zero;

        void Awake()
        {
            m_footstepSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            m_cc = gameObject.GetComponent<CharacterController>();
            m_usedSpeed = m_walkSpeedFwd;

            // Invoke state change.
            s_motionState = m_cc.isGrounded ? PlayerMotionState.Idling : PlayerMotionState.OnAir;
            if (EPlayerStateChanged != null)
                EPlayerStateChanged.Invoke(s_motionState);
        }


        void Update()
        {    
            CheckInputs();
            UpdateMotionState();
            PlayFootstepSFX();


            // If we are not in any mobile platforms set input,
            // Else we're gonna get it from DemoMobileControls
            // Which is supposed to be a joystick canvas in the scene.
#if !(!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
            m_movementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#else
            m_movementInput = DemoMobileController.MoveInput;
#endif

            // Move
            Vector3 move = new Vector3(m_movementInput.x, 0.0f, m_movementInput.y);

            // Interpolate towards the target speed.
            float targetSpeed = GetTargetSpeed(move);
            m_usedSpeed = Mathf.SmoothDamp(m_usedSpeed, targetSpeed, ref m_accelerationVelocity, m_accelerationSmooth);

            // Horizontal & vertical move.
            move = transform.TransformDirection(move);
            m_cc.Move(move * Time.deltaTime * m_usedSpeed);

            // Jump move.
            m_jumpVelocity += m_gravity * Time.deltaTime;

            // Update velocity magnitude.
            m_velocityMagnitude = m_cc.velocity.magnitude;

            m_cc.Move(new Vector3(0, m_jumpVelocity, 0) * Time.deltaTime);

        }


        private void CheckInputs()
        {
            // Jump & sprint input checks below.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (m_isGrounded)
                    m_jumpVelocity += Mathf.Sqrt(m_jumpHeight * -3.0f * m_gravity);
            }

            m_sprintInput = Input.GetKey(KeyCode.LeftShift);
        }

        private void UpdateMotionState()
        {
            // Determine whether we're grounded, moving, walking, running etc.
            m_isGrounded = m_cc.isGrounded;
            if (m_isGrounded && m_jumpVelocity < 0)
                m_jumpVelocity = -10.0f;
         
            bool isMoving = m_velocityMagnitude > m_movementVelocityThreshold;
            m_isRunning = m_isGrounded && m_sprintInput && isMoving;
            m_isWalking = m_isGrounded && !m_isRunning && isMoving;

            // Set states & call events accordingly.
            if (m_isRunning)
            {
                if (s_motionState != PlayerMotionState.Running)
                {
                    s_motionState = PlayerMotionState.Running;
                    if (EPlayerStateChanged != null)
                        EPlayerStateChanged.Invoke(s_motionState);
                }
            }
            else if (m_isWalking)
            {
                if (s_motionState != PlayerMotionState.Walking)
                {
                    s_motionState = PlayerMotionState.Walking;
                    if (EPlayerStateChanged != null)
                        EPlayerStateChanged.Invoke(s_motionState);
                }
            }
            else if (!m_isGrounded)
            {
                if (s_motionState != PlayerMotionState.OnAir)
                {
                    s_motionState = PlayerMotionState.OnAir;
                    if (EPlayerStateChanged != null)
                        EPlayerStateChanged.Invoke(s_motionState);
                }
            }
            else if (m_isGrounded)
            {
                if (s_motionState != PlayerMotionState.Idling)
                {
                    s_motionState = PlayerMotionState.Idling;
                    if (EPlayerStateChanged != null)
                        EPlayerStateChanged.Invoke(s_motionState);
                }
            }
        }
        private void PlayFootstepSFX()
        {
            if (m_isWalking)
            {
                if (Time.time > m_lastFootstep + m_footstepsRate)
                {
                    m_lastFootstep = Time.time;
                    m_footstepSource.PlayOneShot(m_footstepSFX[Random.Range(0, m_footstepSFX.Length)]);
                }
            }
            else if (m_isRunning)
            {
                if (Time.time > m_lastFootstep + m_footstepsRunRate)
                {
                    m_lastFootstep = Time.time;
                    m_footstepSource.PlayOneShot(m_footstepSFX[Random.Range(0, m_footstepSFX.Length)]);
                }
            }
        }

        /// <summary>
        /// Change target speed according to which direction are we trying to move.
        /// Speeds for forward walking direction vs backward walking directions are separate.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private float GetTargetSpeed(Vector3 move)
        {
            float targetSpeed = 0.0f;

            if (move.x != 0.0f && move.z == 0.0f)
            {
                // Horizontal
                if (m_sprintInput)
                    targetSpeed = m_runSpeedHr;
                else
                    targetSpeed = m_walkSpeedHr;
            }
            else if (move.x == 0.0f && move.z > 0)
            {

                // forward
                if (m_sprintInput)
                    targetSpeed = m_runSpeedFwd;
                else
                    targetSpeed = m_walkSpeedFwd;
            }
            else if (move.x == 0.0f && move.z < 0)
            {
                // Backward
                if (m_sprintInput)
                    targetSpeed = m_runSpeedBwd;
                else
                    targetSpeed = m_walkSpeedBwd;
            }
            else if (move.x != 0.0f && move.z > 0.0f)
            {

                // Diagonal Fw
                if (m_sprintInput)
                    targetSpeed = (m_runSpeedFwd + m_runSpeedHr) / 2.0f;
                else
                    targetSpeed = (m_walkSpeedFwd + m_walkSpeedHr) / 2.0f;
            }
            else if (move.x != 0.0f && move.z < 0.0f)
            {

                // Diagonal Bw
                if (m_sprintInput)
                    targetSpeed = (m_runSpeedBwd + m_runSpeedHr) / 2.0f;
                else
                    targetSpeed = (m_walkSpeedBwd + m_walkSpeedHr) / 2.0f;
            }

            return targetSpeed;
        }

    }
}
