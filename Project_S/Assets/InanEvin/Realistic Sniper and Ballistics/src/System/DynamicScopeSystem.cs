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
using UnityEngine.UI;

namespace IE.RSB
{
    /// <summary>
    /// DSS is a feature you can use to create a dynamic scope that shows where the bullet will drop or how it will be affected by the wind vector.
    /// This feature might be performance costy, so not recommended on low-end mobile devices. Completely OK for PC though.
    /// Basically the idea is that you'd have this Dynamic Scope System prefab in your scene, and from your own weapon controller ScopeActivation method whenever you want
    /// to activate/deactivate the dynamic scope system. It uses Canvases to draw a scope rectangle over the screen, and it also uses images to display where the bullet will
    /// land, how it will drop etc.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class DynamicScopeSystem : Singleton<DynamicScopeSystem>
    {
        /// <summary>
        /// Might have multiple zoom levels, a list holds these and can be edited via the inspector.
        /// </summary>
        [System.Serializable]
        public class ZoomLevel
        {
            [Tooltip("Just for editor display purposes.")]
            public string m_level = "4x";

            [Tooltip("Field of View of the camera when the scope is zoomed to this level.")]
            public float m_cameraFOV = 15.0f;

            [Tooltip("Sensitivity mutliplier of this zoom level. The public static property called 'ScopeSensitivityMultiplier' in dynamic scope system will be set to this whenever we switch to this level. E.g example camera controller PlayerCameraController.cs uses this to multiply camera's rotation speed.")]
            public float m_sensitivityMultiplier = 0.5f;
        }

        /// <summary>
        /// Each scope drop indicator will show where the bullet will land. For each zero range set in SniperAndBallisticsSystem, a new indicator will be created.
        /// </summary>

        [System.Serializable]
        private class ScopeDropIndicator
        {
            public float m_distance = 0.0f;
            public float m_windIndicatorMaxWidth = 0.0f;
            public bool m_isHidden = false;

            public RectTransform m_dropIndicator = null;
            public RectTransform m_windIndicator = null;
            public Coroutine m_positionChangeRoutine = null;
            public const float POSITION_DURATION = 0.08f;

            public void Hide(bool hide, bool hideWindIndicator)
            {
                m_isHidden = hide && hideWindIndicator;
                m_dropIndicator.gameObject.SetActive(!hide);

                if (m_windIndicator)
                    m_windIndicator.gameObject.SetActive(!hideWindIndicator);
            }

            // Fire up the coroutine to start changing the local position y towards the target y.
            public void ChangeLocalPositionY(float y, MonoBehaviour routineObject)
            {
                if (routineObject == null)
                    m_dropIndicator.anchoredPosition = new Vector2(m_dropIndicator.anchoredPosition.x, y);
                else
                {
                    if (m_positionChangeRoutine != null)
                        routineObject.StopCoroutine(m_positionChangeRoutine);

                    m_positionChangeRoutine = routineObject.StartCoroutine(ChangePositionYRoutine(y));
                }
            }

            private IEnumerator ChangePositionYRoutine(float targetY)
            {
                float i = 0.0f;
                Vector3 current = m_dropIndicator.anchoredPosition;
                Vector2 target = new Vector3(current.x, targetY);
                while (i < 1.0f)
                {
                    i += Time.deltaTime * 1.0f / POSITION_DURATION;
                    m_dropIndicator.anchoredPosition = Vector2.Lerp(current, target, i);
                    yield return null;
                }
            }
        }

        // Exposed properties.

        [Header("General")]
        [SerializeField, Tooltip("Camera that will be used to adjust drop indicators, e.g main camera you are aiming with.")]
        private Camera m_mainCamera = null;

        [SerializeField, Tooltip("A text component displaying the current zero distance set in SniperAndBallisticsSystem")]
        private Text m_zeroDistanceText = null;

        [SerializeField, Tooltip("How fast will the scope image be faded in/out?")]
        private float m_fadeDuration = 0.1f;

        [Header("Drop Indicators")]
        [SerializeField, Tooltip("Maximum distance that a drop indicator will be generated for. 1000 is ideally optimal, even an overkill if you are not making huge maps.")]
        [Range(100, 2000)]
        private float m_maxScopeDropDistance = 1000;

        [SerializeField, Tooltip("A drop indicator has two parts, left & right, color of the left part.")]
        private Color m_indicatorLeftColor = new Color(1.0f, 0.27f, 0.0f);

        [SerializeField, Tooltip("A drop indicator has two parts, left & right, color of the right part.")]
        private Color m_indicatorRightColor = Color.black;

        [SerializeField, Tooltip("Height of the indicator's image component.")]
        private float m_indicatorHeight = 1.0f;

        [SerializeField, Tooltip("Width of the right indicator's image component.")]
        private float m_indicatorRightWidth = 70.0f;

        [SerializeField, Tooltip("Width of the left indicator's image component.")]
        private float m_indicatorLeftWidth = 40.0f;

        [SerializeField, Tooltip("If the drop amount of the bullet is really low, indicators might stack-up vertically one after another. To prevent that, we can set the min distance they can have in-between.")]
        private float m_minDistanceToPreviousIndicator = 18;

        [SerializeField, Tooltip("If true, any indicator that has a target distance that is lower than the current zero distance will be hidden.")]
        private bool m_hideDistancesBelowZeroDistance = false;

        [Header("Drop Indicator Texts")]

        [SerializeField, Tooltip("Drop Indicator's distance text will use this font.")]
        private Font m_indicatorFont = null;

        [SerializeField, Tooltip("Fontsize of the drop indicator distance text.")]
        private int m_indicatorFontSize = 15;

        [SerializeField, Tooltip("Indicator's show corresponding distances with a text component. This is the offset of that component.")]
        private float m_indicatorTextOffset = 15.0f;

        [Header("Zoom Levels")]
        [SerializeField, Tooltip("Set how many levels of zoom this scope has here.")]
        private List<ZoomLevel> m_zoomLevels = new List<ZoomLevel>();

        [SerializeField, Tooltip("A text component displaying the current zoom level.")]
        private Text m_zoomLevelText = null;

        // Wind indicator.
        [Header("Wind Direction Indicator")]
        [SerializeField, Tooltip("If true, a UI on top of the screen will show the wind speed & wind direction.")]
        private bool m_windDirectionIndicatorEnabled = true;

        [SerializeField, Tooltip("Rect transform of the wind direction indicator. Used to change the angle of the component so that it shows wind's direction in screen space.")]
        private RectTransform m_windDirectionIndicator = null;

        [SerializeField, Tooltip("Canvas group parenting all wind direction indicator UI's. (indicator itself, speed text, y velocity text etc.)")]
        private CanvasGroup m_windDirectionIndicatorCG = null;

        [SerializeField, Tooltip("A text component to display the wind speed.")]
        private Text m_windSpeedText = null;

        [SerializeField, Tooltip("A text component to display the wind's up speed.")]
        private Text m_windUpSpeedText = null;

        [Header("Wind Point Indicator")]
        [SerializeField, Tooltip("When enabled, the left drop indicators will change their horizontal position in the screen to display where the bullet will land according to the wind.")]
        private bool m_windPointIndicatorEnabled = true;

        [SerializeField, Tooltip("For the zero distance 0, an extra wind point indicator will be spawned and put in the middle of the screen. This is the width of that indicator's image component.")]
        private float m_extraIndicatorWidth = 5.0f;

        [SerializeField, Tooltip("If the wind is too strong, the bullet might land on somewhere off-screen, e.g way too much to the right/left. In that case wind point indicators will not be able to accurately show where the bullet lands, so if true, we'll display a warning text.")]
        private bool m_extremeWindTextEnabled = false;

        [SerializeField, Tooltip("Warning text to enable if the wind is too strong to disrupt the wind point indicator prediction.")]
        private GameObject m_extremeWindText = null;

        // Used in example camera controller script PlayerCameraController.cs. The sensitivity is set according to the zoom levels, and camera controller uses this sensitivity
        // while rotating the camera.
        public static float ScopeSensitivityMultiplier
        {
            get { return s_sensitivityMultiplier; }
        }

        // Private class members.
        private bool m_isActive = false;
        private bool m_wasActiveWhenBulletTimeStarted = false;
        private float m_defaultFOV = 0.0f;
        private int m_currentZoomLevel = 0;
        private RectTransform m_canvasRT = null;
        private BulletProperties m_currentBulletProperties = null;
        private RectTransform m_extraWindPointIndicator;
        private Coroutine m_activationRoutine = null;
        private List<ScopeDropIndicator> m_dropIndicators = new List<ScopeDropIndicator>();
        private static float s_sensitivityMultiplier = 1.0f;
        private const float DROP_DISTANCE_INTERVAL = 100.0f;
        private const float MIN_MUZZLEVELOCTIY_TOCALCULATE = 100.0f;
        private CanvasGroup m_canvasGroup = null;

        protected override void Awake()
        {
            base.Awake();

            // Initial setup.
            m_canvasGroup = GetComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0.0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
            m_defaultFOV = m_mainCamera.fieldOfView;
            s_sensitivityMultiplier = 1.0f;
            m_windDirectionIndicatorCG.alpha = m_windDirectionIndicatorEnabled ? 1.0f : 0.0f;
            m_canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

            if (m_extremeWindText != null)
                m_extremeWindText.SetActive(false);

            // Create the drop distance list.
            int count = Mathf.FloorToInt(m_maxScopeDropDistance / DROP_DISTANCE_INTERVAL);
            for (int i = 0; i < count; i++)
            {
                ScopeDropIndicator indicator = new ScopeDropIndicator();
                indicator.m_distance = DROP_DISTANCE_INTERVAL * (i + 1);
                m_dropIndicators.Add(indicator);
            }

            // Add a new range indicator for each zero range defined in SniperAndBallisticsSystem singleton object.
            // Skip first element, which is mandatory to be 0 meters.
            for (int i = 0; i < m_dropIndicators.Count; i++)
            {

                // Create Indicators
                GameObject indicator = new GameObject("Indicator " + m_dropIndicators[i].m_distance.ToString("f0"));
                GameObject indicatorL = new GameObject("Indicator L");
                GameObject indicatorR = new GameObject("Indicator R");
                indicator.transform.parent = transform;
                indicator.transform.localPosition = Vector3.zero;
                indicatorL.transform.parent = indicatorR.transform.parent = indicator.transform;

                // Add rect transform components.
                indicator.AddComponent<RectTransform>();
                RectTransform indicatorRightRT = indicatorR.AddComponent<RectTransform>();

                // Position & size.
                float rWidth = m_indicatorRightWidth * ((float)(i + 1) * 1 / 4);
                indicatorRightRT.sizeDelta = new Vector2(rWidth, m_indicatorHeight);
                indicatorRightRT.anchoredPosition = new Vector2(rWidth / 2.0f + 2, 0.0f);

                // Add Image components
                Image indicatorRImg = indicatorR.AddComponent<Image>();

                // Set colors
                indicatorRImg.color = m_indicatorRightColor;

                // Add texts to right indicators.
                GameObject textObject = new GameObject("Distance Text");
                textObject.transform.parent = indicatorRightRT.transform;
                textObject.transform.localPosition = Vector3.zero;

                // Rect transform & text component
                RectTransform textRT = textObject.AddComponent<RectTransform>();
                Text text = textObject.AddComponent<Text>();

                // Position & setup text.
                // textRT.sizeDelta = new Vector2(50, 50);
                textRT.anchoredPosition = new Vector2(rWidth / 2.0f + m_indicatorTextOffset, 0.0f);
                text.text = m_dropIndicators[i].m_distance.ToString("F0");
                text.alignment = TextAnchor.MiddleCenter;
                text.color = m_indicatorLeftColor;
                text.font = m_indicatorFont;
                text.fontSize = m_indicatorFontSize;

                // Add the rt to the list.
                m_dropIndicators[i].m_dropIndicator = indicatorRightRT;

                if (m_windPointIndicatorEnabled)
                {
                    float lWidth = m_indicatorLeftWidth * ((float)(i + 1) * 1 / 4);
                    RectTransform indicatorLeftRT = indicatorL.AddComponent<RectTransform>();
                    indicatorLeftRT.anchoredPosition = new Vector2(-lWidth / 2.0f - 2, 0.0f);
                    indicatorLeftRT.sizeDelta = new Vector2(lWidth, m_indicatorHeight);
                    Image indicatorLImg = indicatorL.AddComponent<Image>();
                    indicatorLImg.color = m_indicatorLeftColor;
                    m_dropIndicators[i].m_windIndicator = indicatorLeftRT;
                    m_dropIndicators[i].m_windIndicatorMaxWidth = m_dropIndicators[i].m_dropIndicator.sizeDelta.x;
                }

            }

            // If point indicator is enabled, a new point indicator would have been created for each zero distance in the list except 0.
            // Create an extra one to handle the case of zero distance being set to 0.
            if (m_windPointIndicatorEnabled)
            {
                GameObject extraIndicator = new GameObject("Extra Indicator");
                extraIndicator.transform.parent = transform;
                m_extraWindPointIndicator = extraIndicator.AddComponent<RectTransform>();
                Image extraIndicatorImg = extraIndicator.AddComponent<Image>();
                extraIndicatorImg.color = m_indicatorLeftColor;
                m_extraWindPointIndicator.anchoredPosition = Vector2.zero;
                m_extraWindPointIndicator.sizeDelta = new Vector2(m_extraIndicatorWidth, m_indicatorHeight * 2);
            }
        }

        private void OnEnable()
        {
            SniperAndBallisticsSystem.EBulletActivated += OnBulletActivated;
            SniperAndBallisticsSystem.EZeroDistanceChanged += OnZeroDistanceChanged;
            SniperAndBallisticsSystem.EBulletTimeStarted += OnBulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeEnded += OnBulletTimeEnded;
        }
        private void OnDisable()
        {
            SniperAndBallisticsSystem.EBulletActivated -= OnBulletActivated;
            SniperAndBallisticsSystem.EZeroDistanceChanged -= OnZeroDistanceChanged;
            SniperAndBallisticsSystem.EBulletTimeStarted -= OnBulletTimeStarted;
            SniperAndBallisticsSystem.EBulletTimeEnded -= OnBulletTimeEnded;
        }


        private void Update()
        {

            if (m_isActive)
            {

                // Controls below are only enabled if we are not in a mobile platform.
                // Else the respective methods will be called from outside, from DemoMobileControls script.
                // Of course this requires a canvas with joysticks and DemoMobileControls script running in the scene.
                // Which can be found in the prefabs.
#if !(!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))

                // Mouse scroll will change zoom levels.
                if (Input.mouseScrollDelta.y > 0)
                    ZoomIn();
                else if (Input.mouseScrollDelta.y < 0)
                    ZoomOut();

#endif

                // If wind direction indicator is enabled & the current bullet property uses wind
                // Then display the environment wind vector in the UI, relative to the main camera position.
                if (m_windDirectionIndicatorEnabled && m_currentBulletProperties.UseWind)
                {
                    // Get wind vector, exclude y axis as we'll display it seperately.
                    Vector3 windVector = new Vector3(SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric.x, 0.0f, SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric.z);

                    // Get x & z magnitude as well as y magnitude.
                    float windMagnitude = windVector.magnitude;
                    float windUpMagnitude = SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric.y;

                    // Set up info texts.
                    if (SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.MeasurementUnit == SniperAndBallisticsSystem.MeasurementUnits.Metric)
                    {
                        m_windSpeedText.text = (windMagnitude * ConversionConstants.s_kmhToMS).ToString("F2") + " m/s";
                        m_windUpSpeedText.text = (windUpMagnitude * ConversionConstants.s_kmhToMS).ToString("F2") + " m/s";
                    }
                    else
                    {
                        m_windSpeedText.text = (windMagnitude * ConversionConstants.s_kmhToFts).ToString("F2") + " ft/s";
                        m_windUpSpeedText.text = (windUpMagnitude * ConversionConstants.s_kmhToFts).ToString("F2") + " ft/s";
                    }

                    // Flatten the forward director.
                    Vector3 flatForward = m_mainCamera.transform.forward;
                    flatForward.y = 0.0f;

                    // Determine the angle of wind relative to the camera's forward.
                    float angle = Vector3.Angle(windVector, flatForward);
                    Vector3 cross = Vector3.Cross(windVector, flatForward);
                    if (cross.y < 0) angle = -angle;

                    // Rotate indicator.
                    m_windDirectionIndicator.localEulerAngles = new Vector3(m_windDirectionIndicator.localEulerAngles.x, m_windDirectionIndicator.localEulerAngles.y, angle);

                }

                // If wind point indicators are enabled & the current bullet uses wind, as well as the bullet has high enough velocity to be affected considerable by the wind
                // Then constantly predict the trajectory of the bullet depending on the current wind value and display where it would hit with point indicators.
                if (m_windPointIndicatorEnabled && m_currentBulletProperties.UseWind && m_currentBulletProperties.MuzzleVelocity >= MIN_MUZZLEVELOCTIY_TOCALCULATE)
                {
                    bool displayExtremeWindText = false;

                    for (int i = 0; i < m_dropIndicators.Count + 1; i++)
                    {
                        // Dont calculate if hidden.
                        if (i < m_dropIndicators.Count)
                            if (m_dropIndicators[i].m_isHidden) continue;

                        float usedDistance = i < m_dropIndicators.Count ? m_dropIndicators[i].m_distance : SniperAndBallisticsSystem.instance.CurrentZeroDistance;

                        // Get the trajectory info according to the current zero distance
                        // This will give us how much the bullet will be affected by the wind.
                        BulletProperties.BulletTrajectoryInfo info = m_currentBulletProperties.CalculateTrajectoryForDistance(usedDistance, m_mainCamera.transform.forward, SniperAndBallisticsSystem.instance.CurrentZeroDistance);

                        // Calculate wind increment.
                        Vector3 windIncrement = Vector3.zero;
                        Vector3 flatFireDirection = m_mainCamera.transform.forward;
                        flatFireDirection.y = 0.0f;
                        float t = 0.0f;
                        while (t < info.m_time)
                        {
                            windIncrement += BallisticsUtility.GetWindVector(SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric, 0.01f) * t;
                            t += 0.01f;
                        }

                        // Calculate angle & finalize wind increment.
                        float angle = Vector3.Angle(windIncrement, flatFireDirection);
                        Vector3 cross = Vector3.Cross(windIncrement, flatFireDirection);
                        if (cross.y > 0) angle = -angle;
                        angle *= Mathf.Deg2Rad;

                        windIncrement.x = Mathf.Sin(angle) * windIncrement.x * (windIncrement.x < 0 ? -1.0f : 1.0f) + Mathf.Sin(angle) * windIncrement.z * (windIncrement.z < 0 ? -1.0f : 1.0f);
                        windIncrement.z = Mathf.Cos(angle) * windIncrement.x * (windIncrement.x < 0 ? -1.0f : 1.0f) + Mathf.Cos(angle) * windIncrement.z * (windIncrement.z < 0 ? -1.0f : 1.0f);


                        // Get the world point of the bullet relative to the camera if it was to travel by CurrentZeroDistance and by the wind displacement.
                        Vector3 offsetPosition = m_mainCamera.transform.right * windIncrement.x + m_mainCamera.transform.forward * (windIncrement.z + usedDistance)
                        + m_mainCamera.transform.up * windIncrement.y;

                        // Convert the world position to screen, and from screen to canvas position & set the wind point indicator's localtion.
                        Vector3 screen = m_mainCamera.WorldToScreenPoint(m_mainCamera.transform.position + offsetPosition);
                        Vector2 canvasPos = Vector2.zero;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvasRT, screen, null, out canvasPos);

                        // Set the horizontal positions of the drop indicators accordingly.
                        // If there is so much wind that the bullet will land somewhere that can not be seen by the camera right now, then display
                        // extreme wind text to notify the users that the wind indications they see on the screen are not enough.
                        if (i < m_dropIndicators.Count)
                        {
                            if (canvasPos.x <= -m_dropIndicators[i].m_windIndicatorMaxWidth)
                            {
                                canvasPos.x = -m_dropIndicators[i].m_windIndicatorMaxWidth;

                                if (m_extremeWindTextEnabled)
                                    displayExtremeWindText = true;
                            }
                            else if (canvasPos.x >= m_dropIndicators[i].m_windIndicatorMaxWidth)
                            {
                                canvasPos.x = m_dropIndicators[i].m_windIndicatorMaxWidth;

                                if (m_extremeWindTextEnabled)
                                    displayExtremeWindText = true;
                            }
                        }

                        canvasPos.x += canvasPos.x < 0 ? -1.0f : 1.0f;

                        // Set final positions.
                        if (i < m_dropIndicators.Count)
                        {
                            m_dropIndicators[i].m_windIndicator.anchoredPosition = new Vector2(Mathf.Lerp(m_dropIndicators[i].m_windIndicator.anchoredPosition.x, canvasPos.x / 2.0f, Time.deltaTime * 28.0f), m_dropIndicators[i].m_dropIndicator.localPosition.y);
                            m_dropIndicators[i].m_windIndicator.sizeDelta = new Vector2(Mathf.Lerp(m_dropIndicators[i].m_windIndicator.sizeDelta.x, Mathf.Abs(canvasPos.x), Time.deltaTime * 28.0f), m_dropIndicators[i].m_dropIndicator.sizeDelta.y);
                        }
                        else
                        {
                            if (SniperAndBallisticsSystem.instance.CurrentZeroDistance != 0.0f && SniperAndBallisticsSystem.instance.GlobalEnvironmentProperties.m_windSpeedInMetric != Vector3.zero)
                            {
                                if (!m_extraWindPointIndicator.gameObject.activeInHierarchy)
                                    m_extraWindPointIndicator.gameObject.SetActive(true);
                                m_extraWindPointIndicator.anchoredPosition = new Vector2(canvasPos.x, 0.0f);
                            }
                            else
                            {
                                if (m_extraWindPointIndicator.gameObject.activeInHierarchy)
                                    m_extraWindPointIndicator.gameObject.SetActive(false);
                            }
                        }

                    }

                    // Toggle extreme wind text according to whether it was to be activated or not.
                    if (displayExtremeWindText && !m_extremeWindText.activeInHierarchy)
                        m_extremeWindText.SetActive(true);
                    else if (!displayExtremeWindText && m_extremeWindText.activeInHierarchy)
                        m_extremeWindText.SetActive(false);

                }


            }
        }

        public void ZoomIn()
        {
            // Increment zoom level.
            m_currentZoomLevel++;
            if (m_currentZoomLevel >= m_zoomLevels.Count)
                m_currentZoomLevel = m_zoomLevels.Count - 1;

            // Set dynamic variables.
            m_zoomLevelText.text = m_zoomLevels[m_currentZoomLevel].m_level;
            m_mainCamera.fieldOfView = m_zoomLevels[m_currentZoomLevel].m_cameraFOV;
            s_sensitivityMultiplier = m_zoomLevels[m_currentZoomLevel].m_sensitivityMultiplier;
            SetDropDistances();
        }

        public void ZoomOut()
        {
            // Decrement zoom levle.
            m_currentZoomLevel--;
            if (m_currentZoomLevel <= 0)
                m_currentZoomLevel = 0;

            // Set dynamic variables.
            m_zoomLevelText.text = m_zoomLevels[m_currentZoomLevel].m_level;
            m_mainCamera.fieldOfView = m_zoomLevels[m_currentZoomLevel].m_cameraFOV;
            s_sensitivityMultiplier = m_zoomLevels[m_currentZoomLevel].m_sensitivityMultiplier;
            SetDropDistances();
        }


        public void ScopeActivation(bool activate, BulletProperties properties, float delay = 0.0f, bool instantActivation = false)
        {
            if (m_isActive == activate) return;

            // Flags.
            m_isActive = activate;
            m_currentBulletProperties = properties;

            // Make sure coroutine runs only once.
            if (m_activationRoutine != null)
                StopCoroutine(m_activationRoutine);

            // If it's not instant activation, fire up the routine which will interpolate the scope image.
            if (!instantActivation)
                m_activationRoutine = StartCoroutine(ActivationRoutine(activate, delay));

            if (activate)
            {
                // Set texts.
                m_zoomLevelText.text = m_zoomLevels[m_currentZoomLevel].m_level;
                s_sensitivityMultiplier = m_zoomLevels[m_currentZoomLevel].m_sensitivityMultiplier;
                m_zeroDistanceText.text = SniperAndBallisticsSystem.instance.CurrentZeroDistance.ToString("000");

                if (instantActivation)
                {
                    m_canvasGroup.alpha = 1.0f;
                    m_mainCamera.fieldOfView = m_zoomLevels[m_currentZoomLevel].m_cameraFOV;
                }

                SetDropDistances();
            }
            else
            {
                s_sensitivityMultiplier = 1.0f;

                if (m_extremeWindTextEnabled)
                    m_extremeWindText.SetActive(false);


                if (instantActivation)
                {
                    m_canvasGroup.alpha = 0.0f;
                    m_mainCamera.fieldOfView = m_defaultFOV;
                }

            }

        }

        private void SetDropDistances()
        {
            // Hide all indicators if the bullet's velocity is too low, lower velocity means more steps, means harder calculations so we skip due to optimization.
            if (m_currentBulletProperties.MuzzleVelocity < MIN_MUZZLEVELOCTIY_TOCALCULATE)
            {
                for (int i = 0; i < m_dropIndicators.Count; i++)
                    m_dropIndicators[i].Hide(true, true);
                return;
            }

            float previousCanvasY = 0.0f;
            for (int i = 0; i < m_dropIndicators.Count; i++)
            {
                // Decide which indicators shall be enabled, which ones shall be deactivated.
                bool wasHiddenByZero = false;
                if (m_dropIndicators[i].m_distance == SniperAndBallisticsSystem.instance.CurrentZeroDistance)
                {
                    m_dropIndicators[i].Hide(true, true);
                    wasHiddenByZero = true;
                }
                else
                {
                    if (m_hideDistancesBelowZeroDistance && m_dropIndicators[i].m_distance < SniperAndBallisticsSystem.instance.CurrentZeroDistance)
                    {
                        m_dropIndicators[i].Hide(true, true);
                        wasHiddenByZero = true;
                    }
                    else
                    {
                        m_dropIndicators[i].Hide(false, false);
                    }
                }

                // Get the trajectory info at the target distance.
                BulletProperties.BulletTrajectoryInfo info = m_currentBulletProperties.CalculateTrajectoryForDistance(m_dropIndicators[i].m_distance, m_mainCamera.transform.forward, SniperAndBallisticsSystem.instance.CurrentZeroDistance);

                // Calculate the bullet's position relative to the camera & convert it into the screen space.
                Vector3 offsetPosition = m_mainCamera.transform.up * info.m_position.y + m_mainCamera.transform.forward * (m_dropIndicators[i].m_distance);
                Vector3 screen = m_mainCamera.WorldToScreenPoint(m_mainCamera.transform.position + offsetPosition);
                Vector2 canvasPos = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvasRT, screen, null, out canvasPos);

                // Set the position as the local y position of the indicator.
                if (!m_dropIndicators[i].m_isHidden)
                    m_dropIndicators[i].ChangeLocalPositionY(canvasPos.y, this);
                else
                    m_dropIndicators[i].ChangeLocalPositionY(canvasPos.y, null);

                // If the difference to the previous indicator is too low, hide this one.
                float diff = canvasPos.y - previousCanvasY;
                if (Mathf.Abs(diff) < m_minDistanceToPreviousIndicator)
                    m_dropIndicators[i].Hide(true, m_dropIndicators[i].m_distance != SniperAndBallisticsSystem.instance.CurrentZeroDistance);
                else
                {
                    if (!wasHiddenByZero)
                        m_dropIndicators[i].Hide(false, false);
                }

                if (!m_dropIndicators[i].m_isHidden) previousCanvasY = canvasPos.y;
            }

        }

        /// <summary>
        /// When a new bullet is calculated, e.g meaning that the bullet will have stored it's possible trajectories according to the zero ranges set in SniperAndBallisticsSystem
        /// Also make bullet store possible trajectories according to the distances of the drop indicators set here. This way our updates will be a little bit faster.
        /// </summary>
        /// <param name="properties"></param>
        private void OnBulletActivated(BulletProperties properties)
        {
            for (int i = 0; i < m_dropIndicators.Count; i++)
                properties.CalculateTrajectoryForDistance(m_dropIndicators[i].m_distance, Vector3.one);
        }

        /// <summary>
        /// Update drop distances according to the current zero distance once it's changed.
        /// </summary>
        private void OnZeroDistanceChanged()
        {
            if (m_isActive)
            {
                SetDropDistances();
                m_zeroDistanceText.text = SniperAndBallisticsSystem.instance.CurrentZeroDistance.ToString("000");
            }
        }

        /// <summary>
        /// If the scope is activate when bullet time started, deactivate it.
        /// </summary>
        /// <param name="bullet"></param>
        /// <param name="hitTarget"></param>
        /// <param name="bulletPath"></param>
        /// <param name="totalDistance"></param>
        private void OnBulletTimeStarted(Transform bullet, Transform hitTarget, ref List<BulletPoint> bulletPath, float totalDistance)
        {
            if (m_isActive)
            {
                m_wasActiveWhenBulletTimeStarted = true;
                ScopeActivation(false, m_currentBulletProperties, 0.0f, true);
            }
        }

        /// <summary>
        /// If the scope was active when bullet time has started reactivate it.
        /// </summary>
        private void OnBulletTimeEnded()
        {
            if (m_wasActiveWhenBulletTimeStarted)
            {
                m_wasActiveWhenBulletTimeStarted = false;
                ScopeActivation(true, m_currentBulletProperties, 0.0f, true);
            }
        }

        /// <summary>
        /// Interpolate whole scope's alpha along with the camera's field of view once the scope is activated or deactivated.
        /// </summary>
        /// <param name="activate"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator ActivationRoutine(bool activate, float delay)
        {
            if (delay != 0.0f)
                yield return new WaitForSeconds(delay);

            float i = 0.0f;
            float current = m_canvasGroup.alpha;
            float start = m_canvasGroup.alpha;
            float end = activate ? 1.0f : 0.0f;
            float startFOV = m_mainCamera.fieldOfView;
            float endFOV = activate ? m_zoomLevels[m_currentZoomLevel].m_cameraFOV : m_defaultFOV;

            while (i < 1.0f)
            {
                i += Time.deltaTime * 1.0f / m_fadeDuration;
                m_canvasGroup.alpha = Mathf.Lerp(start, end, i);
                m_mainCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, i);
                yield return null;
            }

            SetDropDistances();
            m_isActive = activate;
        }

    }

}
