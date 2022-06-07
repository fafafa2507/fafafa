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
using UnityEngine.UI;

namespace IE.RSB
{
    /// <summary>
    /// Simple bolt-action sniper rifle controls. Can fire, count ammunition & maganizes, play animations & eject shells, play audio,
    /// aim etc.
    /// </summary>
    public class PlayerWeaponController : MonoBehaviour
    {
        // Exposed properties.

        [Header("General")]
        [SerializeField, Tooltip("Bullet properties asset this weapon uses to fire. This will be sent to SniperAndBallisticsSystem to fire a ballistics bullet based on this properties.")]
        private BulletProperties m_bulletProperties = null;

        [SerializeField, Tooltip("Reference to the motion controller parenting the camera.")]
        private MotionController m_cameraMotionController = null;

        [SerializeField, Tooltip("Reference to the motion controller parenting the weapon.")]
        private MotionController m_weaponMotionController = null;

        [SerializeField, Tooltip("Weapon animator.")]
        private Animator m_animator = null;

        [SerializeField, Tooltip("Camera animator.")]
        private Animator m_cameraAnimator = null;

        [SerializeField, Tooltip("Fire rate of the weapon, the amount of time that has to have passed from last shot in order to fire a new one.")]
        private float m_fireRate = 2.0f;

        [SerializeField, Tooltip("Muzzle flashes and bullet time bullet's will start from this position.")]
        private Transform m_fireReference = null;


        [Header("Dynamic Scope System")]
        [SerializeField, Tooltip("If true, dynamic scope system will be requested to activate/deactivate the scope whenever we aim/deaim.")]
        private bool m_useDynamicScopeSystem = true;

        [Header("Bullet Time")]
        [SerializeField, Tooltip("Bullet gameobject that will be used in bullet time effects.")]
        private Transform m_bulletTimeBullet = null;

        [Header("Shell Ejection")]
        [SerializeField, Tooltip("The reference object that the ejected shell prefabs will be spawned from. ")]
        private Transform m_shellEjectReference = null;

        [SerializeField, Tooltip("Pooler reference that will be used to request a new shell prefab.")]
        private ObjectPooler m_shellPooler = null;

        [Header("Reload & Ammo")]
        [SerializeField, Tooltip("Ammo in each mag. When we reload, our available ammo will be set to this.")]
        private int m_ammoInMagazine = 15;

        [SerializeField, Tooltip("How many magazines do we start with?")]
        private int m_startMagazineCount = 3;

        [SerializeField, Tooltip("Time before being able to fire again after reload is issued.")]
        private float m_reloadTime = 3.0f;

        [SerializeField, Tooltip("Text component displaying the remaining ammo.")]
        private Text m_ammoText = null;

        [Header("Effects")]
        [SerializeField, Tooltip("Pooler reference that will be used to request a new muzzle flash particle.")]
        private ObjectPooler m_muzzleFlashPooler = null;

        [SerializeField, Tooltip("Audio source that'll be used to play weapon sfx.")]
        private AudioSource m_weaponSource = null;

        [SerializeField, Tooltip("Fire clip.")]
        private AudioClip m_fireSFX = null;

        [SerializeField, Tooltip("Clip to play when we are trying to dry-fire (firing with no ammo left).")]
        private AudioClip m_emptySFX = null;

        [SerializeField, Tooltip("Clip to play bolt/reload sound effect.")]
        private AudioClip m_boltSFX = null;

        [SerializeField, Tooltip("Clip to play when magazine is ejected during reload animation.")]
        private AudioClip m_magEjectSFX = null;

        [SerializeField, Tooltip("Clip to play when magazine is inserted during reload animation.")]
        private AudioClip m_magInsertSFX = null;

        // Private class members.
        private int m_boltHash = Animator.StringToHash("Bolt");
        private int m_reloadHash = Animator.StringToHash("Reload");
        private int m_availableAmmoNow = 0;
        private float m_lastFired = 0.0f;
        private bool m_isReloading = false;
        private int m_availableMagazines = 0;
        private bool m_isAiming = false;
        private bool m_wasRunning = false;

        private void Awake()
        {
            // Setup ammo.
            m_availableAmmoNow = m_startMagazineCount > 0 ? m_ammoInMagazine : 0;
            m_availableMagazines = m_startMagazineCount;
            m_lastFired = -m_fireRate;
            UpdateAmmoText();

            // Activate bullet at awake, this will calculate the required angles for zeroing this bullet.
            SniperAndBallisticsSystem.instance.ActivateBullet(m_bulletProperties);
        }

        private void OnEnable()
        {
            PlayerMovementController.EPlayerStateChanged += OnPlayerStateChanged;
            SniperAndBallisticsSystem.EBulletTimeStarted += OnBulletTimeStarted;
        }

        private void OnDisable()
        {
            PlayerMovementController.EPlayerStateChanged -= OnPlayerStateChanged;
            SniperAndBallisticsSystem.EBulletTimeStarted -= OnBulletTimeStarted;
        }

        private void OnPlayerStateChanged(PlayerMovementController.PlayerMotionState state)
        {
            // If we are running, make sure we switch to run position & are not aiming.
            // If we are not running, make sure we go back to hip position.
            if (state == PlayerMovementController.PlayerMotionState.Running)
            {
                m_isAiming = false;
                m_weaponMotionController.ToRunPosition();
                m_wasRunning = true;

                if (m_useDynamicScopeSystem)
                    DynamicScopeSystem.instance.ScopeActivation(false, m_bulletProperties);
            }
            else
            {
                if (m_wasRunning)
                {
                    m_weaponMotionController.ToHipPosition();
                    m_wasRunning = false;
                }
            }
        }


        private void Update()
        {
            // Controls below are only enabled if we are not in a mobile platform.
            // Else the respective methods will be called from outside, from DemoMobileControls script.
            // Of course this requires a canvas with joysticks and DemoMobileControls script running in the scene.
            // Which can be found in the prefabs.
#if !(!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))

            if (Input.GetMouseButtonDown(0))
                FireInput();

            if (Input.GetKeyDown(KeyCode.R))
                ReloadInput();

            if (Input.GetMouseButtonDown(1))
                AimInput(true);
            if (Input.GetMouseButtonUp(1))
                AimInput(false);

            // Cycle through zero distances upon pressing B&V when we are aiming.
            if (m_isAiming)
            {
                if (Input.GetKeyDown(KeyCode.B))
                    SniperAndBallisticsSystem.instance.CycleZeroDistanceUp();
                if (Input.GetKeyDown(KeyCode.V))
                    SniperAndBallisticsSystem.instance.CycleZeroDistanceDown();
            }
#endif
        }

        private void Fire()
        {
            // Cast audio
            m_weaponSource.PlayOneShot(m_fireSFX);

            if (!m_isAiming)
            {
                // Muzzle flash
                GameObject flash = m_muzzleFlashPooler.GetPooledObject();
                flash.transform.position = m_fireReference.position;
                flash.transform.rotation = m_fireReference.rotation;
                flash.SetActive(true);
            }

            // Decrease ammo & fire.
            m_availableAmmoNow--;
            SniperAndBallisticsSystem.instance.FireBallisticsBullet(m_bulletProperties, m_fireReference, m_bulletTimeBullet);

            // Play bolt action animation.
            m_animator.SetTrigger(m_boltHash);
            m_cameraAnimator.SetTrigger(m_boltHash);

            // Recoil
            m_weaponMotionController.Recoil();
            m_cameraMotionController.Recoil();

            // Update ammo
            UpdateAmmoText();

        }


        public void FireInput()
        {
            if (!m_isReloading)
            {
                if (m_availableAmmoNow > 0)
                {
                    if (Time.time > m_fireRate + m_lastFired)
                    {
                        m_lastFired = Time.time;
                        Fire();
                    }
                }
                else
                {
                    // Click sound.
                    m_weaponSource.PlayOneShot(m_emptySFX);
                }
            }
        }

        public void ReloadInput()
        {
            if (!m_isReloading && m_availableMagazines > 0)
            {
                m_isReloading = true;
                m_animator.SetTrigger(m_reloadHash);

                Invoke("EndReload", m_reloadTime);
                if (m_availableAmmoNow == 0)
                    m_animator.SetTrigger(m_boltHash);
            }
        }

        public void AimInput(bool aimIn)
        {
            if (SniperAndBallisticsSystem.instance.BulletTimeRunning) return;

            if (!m_isAiming && aimIn)
            {
                if (PlayerMovementController.PlayerState != PlayerMovementController.PlayerMotionState.Running)
                {
                    // Send the weapon to aim position.
                    m_weaponMotionController.ToAimPosition();

                    // Activate camera breath sway when in aim.
                    m_cameraMotionController.BreathSwayActivation(true);

                    // Activate aim scope.
                    if (m_useDynamicScopeSystem)
                        DynamicScopeSystem.instance.ScopeActivation(true, m_bulletProperties, 0.15f);

                    m_isAiming = true;
                }
            }
            else if(m_isAiming && !aimIn)
            {
                // Send the weapon to hip position.
                m_weaponMotionController.ToHipPosition();

                // Deactivate camera breath sway when we stop aiming.
                m_cameraMotionController.BreathSwayActivation(false);

                // Deactivate aim scope.
                if (m_useDynamicScopeSystem)
                    DynamicScopeSystem.instance.ScopeActivation(false, m_bulletProperties);

                m_isAiming = false;
            }
        }

        public void PlayMagEjectSFX()
        {
            m_weaponSource.PlayOneShot(m_magEjectSFX);
        }

        public void PlayMagInsertSFX()
        {
            m_weaponSource.PlayOneShot(m_magInsertSFX);
        }

        public void PlayBoltSFX()
        {
            m_weaponSource.PlayOneShot(m_boltSFX);
        }

        public void EjectShell()
        {
            GameObject shell = m_shellPooler.GetPooledObject();
            shell.transform.position = m_shellEjectReference.position;
            shell.transform.rotation = m_shellEjectReference.rotation;
            shell.SetActive(true);
        }

        private void EndReload()
        {
            m_isReloading = false;
            m_availableAmmoNow = m_ammoInMagazine;
            m_availableMagazines--;
            UpdateAmmoText();
        }

        private void UpdateAmmoText()
        {
            m_ammoText.text = m_availableAmmoNow.ToString() + " / " + ((m_availableMagazines) * m_ammoInMagazine).ToString();
        }

        private void OnBulletTimeStarted(Transform bullet, Transform hitTarget, ref List<BulletPoint> bulletPath, float totalDistance)
        {
            // normally we dont want muzzle flash to occur when the player is aiming,
            // but if bullet time is started it means we switched to bullet camera
            // so we like to have a muzzle flash for co0Lness.
            if (m_isAiming)
            {
                // Muzzle flash
                GameObject flash = m_muzzleFlashPooler.GetPooledObject();
                flash.transform.position = m_fireReference.position;
                flash.transform.rotation = m_fireReference.rotation;
                flash.transform.parent = m_fireReference;
                flash.SetActive(true);
            }
        }

    }

}
