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
using UnityEditor;

namespace IE.RSB
{

    [CustomEditor(typeof(BulletProperties))]
    public class BulletPropertiesEditor : Editor
    {
        // Bullet Properties.
        private SerializedProperty m_ballisticCoef;
        private SerializedProperty m_lifeTime;
        private SerializedProperty m_dragModel;
        private SerializedProperty m_muzzleVelocity;
        private SerializedProperty m_mass;
        private SerializedProperty m_diameter;
        private SerializedProperty m_length;
        private SerializedProperty m_barrelTwist;
        private SerializedProperty m_affectedByWind;
        private SerializedProperty m_affectedByAirResistance;
        private SerializedProperty m_affectedBySpinDrift;

        // Metrics are primarly used.
        private SerializedProperty m_muzzleVelocityInMetric;
        private SerializedProperty m_massInMetric;
        private SerializedProperty m_diameterInMetric;
        private SerializedProperty m_lengthInMetric;
        private SerializedProperty m_barrelTwistInMetric;

        // For editor
        private SerializedProperty m_bulletPreset;
        private SerializedProperty m_previousBulletPreset;
        private SerializedProperty m_measurementUnit;
        private SerializedProperty m_previousMeasurementUnit;
        private SerializedProperty m_ballisticSimulationFoldout;
        private SerializedProperty m_timeShown;
        private SerializedProperty m_horizontalRange;
        private SerializedProperty m_verticalRange;
        private SerializedProperty m_shootingAngle;

        private BulletProperties m_data;

        // Constants for drawing.
        private const float LIFETIME_MIN = 0.1f;
        private const float STEPTIME_MIN = 0.01f;
        private const float STEPTIME_MAX = 0.1f;
        private string[] m_velocityUnits = { "(ft/s)", "(m/s)" };
        private string[] m_massUnits = { "(grains)", "(grams)" };
        private string[] m_lengthUnits = { "(in)", "(mm)" };
        private string[] m_rangeUnits = { "(yards)", "(meters)" };
        private static Color m_graphAxesColorFree = new Color(0.24f, 0.49f, 0.9f, 1.0f);
        private static Color m_graphAxesColorPro = new Color(0.24f, 0.49f, 0.9f, 1.0f);
        private static Color m_graphBackgroundColorFree = new Color(0.875f, 0.875f, 0.875f, 1.0f);
        private static Color m_graphBackgroundColorPro = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        private static Color m_graphGridColorFree = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        private static Color m_graphGridColorPro = new Color(0.15f, 0.15f, 0.15f, 1.0f);
        private static Color m_infoTextColorFree = Color.black;
        private static Color m_infoTextColorPro = Color.white;

        private GUIStyle m_labelStyle;

        struct GraphPointInfo
        {
            public Vector2 m_position;
            public float m_kineticEnergy;
            public float m_time;
            public float m_velocity;
        }

        private void OnEnable()
        {
            m_data = (BulletProperties)target;
            m_bulletPreset = serializedObject.FindProperty("m_bulletPreset");
            m_previousBulletPreset = serializedObject.FindProperty("m_previousBulletPreset");
            m_measurementUnit = serializedObject.FindProperty("m_measurementUnit");
            m_previousMeasurementUnit = serializedObject.FindProperty("m_previousMeasurementUnit");
            m_dragModel = serializedObject.FindProperty("m_dragModel");
            m_muzzleVelocity = serializedObject.FindProperty("m_muzzleVelocity");
            m_mass = serializedObject.FindProperty("m_mass");
            m_diameter = serializedObject.FindProperty("m_diameter");
            m_length = serializedObject.FindProperty("m_length");
            m_barrelTwist = serializedObject.FindProperty("m_barrelTwist");
            m_muzzleVelocityInMetric = serializedObject.FindProperty("m_muzzleVelocityInMetric");
            m_massInMetric = serializedObject.FindProperty("m_massInMetric");
            m_diameterInMetric = serializedObject.FindProperty("m_diameterInMetric");
            m_lengthInMetric = serializedObject.FindProperty("m_lengthInMetric");
            m_barrelTwistInMetric = serializedObject.FindProperty("m_barrelTwistInMetric");
            m_ballisticCoef = serializedObject.FindProperty("m_ballisticCoef");
            m_lifeTime = serializedObject.FindProperty("m_lifeTime");
            m_ballisticSimulationFoldout = serializedObject.FindProperty("m_ballisticSimulationFoldout");
            m_timeShown = serializedObject.FindProperty("m_timeShown");
            m_horizontalRange = serializedObject.FindProperty("m_horizontalRange");
            m_verticalRange = serializedObject.FindProperty("m_verticalRange");
            m_shootingAngle = serializedObject.FindProperty("m_shootingAngle");
            m_affectedByWind = serializedObject.FindProperty("m_affectedByWind");
            m_affectedByAirResistance = serializedObject.FindProperty("m_affectedByAirResistance");
            m_affectedBySpinDrift = serializedObject.FindProperty("m_affectedBySpinDrift");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_labelStyle = new GUIStyle("Label");

            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(m_bulletPreset, new GUIContent("Bullet Preset", "You can choose a bullet preset from a predetermined list or input your custom bullet properties."));

            string effectiveRange = "";
            string bulletPresetInfoString = GetBulletPresetInfo(m_bulletPreset.enumValueIndex, ref effectiveRange);

            if (m_bulletPreset.enumValueIndex != m_previousBulletPreset.enumValueIndex)
            {
                m_previousBulletPreset.enumValueIndex = m_bulletPreset.enumValueIndex;
                SetBulletPreset(m_bulletPreset.enumValueIndex);
            }

            if (bulletPresetInfoString.CompareTo("") != 0)
                EditorGUILayout.HelpBox(bulletPresetInfoString, MessageType.Info);

            EditorGUILayout.HelpBox("Effective Range: " + effectiveRange + "\nEffective range is an arbitrary concept, it depends a lot on the shooter, on the type of the" +
                " ammunition as well as the weapon used. However, the numbers here might act as a guide in the means of understanding the ammunition's power.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            // Unit selection & property conversion.
            EditorGUILayout.PropertyField(m_measurementUnit, new GUIContent("Unit", "Units of measurement for the calculations."));

            if (m_previousMeasurementUnit.enumValueIndex != m_measurementUnit.enumValueIndex)
            {
                if (m_measurementUnit.enumValueIndex == (int)SniperAndBallisticsSystem.MeasurementUnits.Imperial)
                {
                    m_muzzleVelocity.floatValue *= ConversionConstants.s_meterToFeet;
                    m_mass.floatValue *= ConversionConstants.s_gramToGrain;
                    m_diameter.floatValue *= ConversionConstants.s_mmToInch;
                    m_length.floatValue *= ConversionConstants.s_mmToInch;
                    m_barrelTwist.floatValue *= ConversionConstants.s_mmToInch;
                }
                else
                {
                    m_muzzleVelocity.floatValue /= ConversionConstants.s_meterToFeet;
                    m_mass.floatValue /= ConversionConstants.s_gramToGrain;
                    m_diameter.floatValue /= ConversionConstants.s_mmToInch;
                    m_length.floatValue /= ConversionConstants.s_mmToInch;
                    m_barrelTwist.floatValue /= ConversionConstants.s_mmToInch;
                }

                m_previousMeasurementUnit.enumValueIndex = m_measurementUnit.enumValueIndex;
            }

            // Bullet properties.
            string usedVelocityUnit = m_velocityUnits[m_measurementUnit.intValue];
            string usedMassUnit = m_massUnits[m_measurementUnit.intValue];
            string usedLengthUnit = m_lengthUnits[m_measurementUnit.intValue];

            if (m_bulletPreset.enumValueIndex != (int)BulletProperties.BulletPresets.Custom)
                GUI.enabled = false;

            EditorGUILayout.PropertyField(m_muzzleVelocity, new GUIContent("Muzzle Velocity " + usedVelocityUnit, "Initial muzzle velocity at the time of firing, default is 500 m/s."));
            EditorGUILayout.PropertyField(m_mass, new GUIContent("Mass " + usedMassUnit, "The mass of the bullet, default is 12 grams."));
            EditorGUILayout.PropertyField(m_diameter, new GUIContent("Diameter " + usedLengthUnit, "Diameter of the bullet, default is 9 millimeters."));
            EditorGUILayout.PropertyField(m_length, new GUIContent("Length " + usedLengthUnit, "Length of the bullet, default is 30 millimeters."));
            EditorGUILayout.PropertyField(m_barrelTwist, new GUIContent("Barrel Twist " + usedLengthUnit, "Barrel twist rate for the rifling of the barrel, default is 254 millimeters."));
            m_ballisticCoef.floatValue = EditorGUILayout.Slider(new GUIContent("Ballistic Coefficient", "Higher ballistic coefficient means more power to punch through air resistance, default is 0.295"), m_ballisticCoef.floatValue, 0.1f, 1.0f);
            EditorGUILayout.PropertyField(m_dragModel, new GUIContent("Drag Model", "Ballistic drag model for different types of bullets. Default is G1, which is the most common ballistic model."));

            if (m_mass.floatValue <= 0.0f)
                m_mass.floatValue = 0.1f;

            GUI.enabled = true;

            EditorGUILayout.PropertyField(m_affectedByWind, new GUIContent("Affected By Wind?", "Determines whether the bullet will be affected by wind speed found in environment properties. "));
            EditorGUILayout.PropertyField(m_affectedBySpinDrift, new GUIContent("Affected By Spin Drift?", "Determines whether the spin drift, amount of rotations that the bullet takes before leaving the rifling, will be calculated. Causes the bullets to drift towards positive x axis relative to the shooting axis."));
            EditorGUILayout.PropertyField(m_affectedByAirResistance, new GUIContent("Affected By Air Resistance?", "Whether the bullet will be affected by drag factors throughout its flight."));

            // Constantly update metric values.
            m_muzzleVelocityInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_muzzleVelocity.floatValue / ConversionConstants.s_meterToFeet : m_muzzleVelocity.floatValue;
            m_massInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_mass.floatValue / ConversionConstants.s_gramToGrain : m_mass.floatValue;
            m_diameterInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_diameter.floatValue / ConversionConstants.s_mmToInch : m_diameter.floatValue;
            m_lengthInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_length.floatValue / ConversionConstants.s_mmToInch : m_length.floatValue;
            m_barrelTwistInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_barrelTwist.floatValue / ConversionConstants.s_mmToInch : m_barrelTwist.floatValue;

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            // Step & life times.
            EditorGUILayout.PropertyField(m_lifeTime, new GUIContent("Life Time", "The time the bullet will stay alive for, default is 3.0 seconds."));

            if (m_lifeTime.floatValue < LIFETIME_MIN)
                m_lifeTime.floatValue = LIFETIME_MIN;

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel++;
            m_ballisticSimulationFoldout.boolValue = EditorGUILayout.Foldout(m_ballisticSimulationFoldout.boolValue, new GUIContent("Ballistics Simulation"));
            EditorGUI.indentLevel--;

            if (m_ballisticSimulationFoldout.boolValue)
            {

                Color graphBackgroundColor = EditorGUIUtility.isProSkin ? m_graphBackgroundColorPro : m_graphBackgroundColorFree;
                Color axesColor = EditorGUIUtility.isProSkin ? m_graphAxesColorPro : m_graphAxesColorFree;
                Color gridLineColor = EditorGUIUtility.isProSkin ? m_graphGridColorPro : m_graphGridColorFree;
                float graphRectYIndentation = 25;
                float outlineRectWidth = 2;
                float graphRectWidth = GUILayoutUtility.GetLastRect().width;
                float graphRectHeight = 350.0f;
                float graphRectX = GUILayoutUtility.GetLastRect().x;
                float graphRectY = GUILayoutUtility.GetLastRect().y + graphRectYIndentation;

                // Graph outer rectangle & outline.
                EditorGUI.DrawRect(new Rect(graphRectX - outlineRectWidth, graphRectY - outlineRectWidth, graphRectWidth + outlineRectWidth, graphRectHeight + outlineRectWidth), Color.black);
                EditorGUI.DrawRect(new Rect(graphRectX, graphRectY, graphRectWidth - outlineRectWidth, graphRectHeight - outlineRectWidth), graphBackgroundColor);

                // Vertical axis.
                Rect verticalAxisRect = new Rect(graphRectX + 50, graphRectY + 10, 2, graphRectHeight - 40);
                EditorGUI.DrawRect(verticalAxisRect, axesColor);

                // Horizontal axis.
                Rect horizontalAxisRect = new Rect(verticalAxisRect.x, verticalAxisRect.y + verticalAxisRect.height, graphRectWidth - 80, 2);
                EditorGUI.DrawRect(horizontalAxisRect, axesColor);

                int numberOfLabels = 10;
                Vector2 labelSize = new Vector2(45, 16);

                // Ranges.
                for (int i = 0; i < numberOfLabels + 1; i++)
                {
                    float labelRangeX = Mathf.Lerp(0, (float)m_horizontalRange.intValue, (float)i / (float)numberOfLabels);
                    float labelRangeY = Mathf.Lerp(0, (float)m_verticalRange.intValue, (float)i / (float)numberOfLabels);
                    float verticalRangeRectY = labelRangeY.Remap(0, (float)m_verticalRange.intValue, verticalAxisRect.y, verticalAxisRect.y + verticalAxisRect.height);
                    float horizontalRangeRectX = labelRangeX.Remap(0, (float)m_horizontalRange.intValue, horizontalAxisRect.x, horizontalAxisRect.x + horizontalAxisRect.width);
                    Rect labelRectX = new Rect(verticalAxisRect.x - 45, verticalRangeRectY - labelSize.y / 2.0f, labelSize.x, labelSize.y);
                    Rect labelRectY = new Rect(horizontalRangeRectX - labelSize.x / 2.0f, verticalAxisRect.y + verticalAxisRect.height + 5, labelSize.x, labelSize.y);

                    // Vertical labels.
                    string significant = "F0";
                    string unit = m_measurementUnit.enumValueIndex == 0 ? "y" : "m";
                    if (labelRangeX >= 1000.0f)
                    {
                        labelRangeX /= 1000.0f;
                        unit = m_measurementUnit.enumValueIndex == 0 ? "mi" : "km";
                        significant = "F1";
                    }
                    else if (labelRangeX < 100.0f)
                    {
                        significant = "F1";
                    }
                    else if (labelRangeX < 10.0f)
                    {
                        significant = "F2";
                    }

                    if (i != 0)
                        EditorGUI.LabelField(labelRectY, labelRangeX.ToString(significant) + unit);

                    // Horizontal labels.
                    significant = "F0";
                    unit = m_measurementUnit.enumValueIndex == 0 ? "y" : "m";

                    labelRangeY -= (float)m_verticalRange.intValue / 2.0f;
                    labelRangeY *= -1;

                    if (Mathf.Abs(labelRangeY) >= 1000.0f)
                    {
                        labelRangeY /= 1000.0f;
                        unit = m_measurementUnit.enumValueIndex == 0 ? "mi" : "km";
                        significant = "F1";
                    }
                    else if (Mathf.Abs(labelRangeY) < 100.0f)
                    {
                        significant = "F0";
                    }
                    else if (Mathf.Abs(labelRangeY) < 10.0f)
                    {
                        significant = "F2";
                    }


                    EditorGUI.LabelField(labelRectX, labelRangeY.ToString(significant) + unit);

                    // Grids.
                    Rect gridRectHorizontal = new Rect(verticalAxisRect.x - 45, verticalRangeRectY, labelSize.x, labelSize.y);

                    // X Grids.
                    if (i < numberOfLabels)
                        EditorGUI.DrawRect(new Rect(verticalAxisRect.x, gridRectHorizontal.y, horizontalAxisRect.width, horizontalAxisRect.height), gridLineColor);

                    // Y Grids.
                    if (i > 0)
                        EditorGUI.DrawRect(new Rect(horizontalRangeRectX, verticalAxisRect.y, verticalAxisRect.width, verticalAxisRect.height), gridLineColor);
                }

                GUILayout.Space(verticalAxisRect.height + 50);

                // Simulation properties.
                EditorGUILayout.BeginVertical("GroupBox");

                string usedRangeUnit = m_rangeUnits[m_measurementUnit.enumValueIndex];

                m_timeShown.floatValue = EditorGUILayout.Slider(new GUIContent("Time Shown", "Percentage of Lifetime that shows in the graph."), m_timeShown.floatValue, 0.0f, 1.0f);
                m_horizontalRange.intValue = EditorGUILayout.IntSlider(new GUIContent("Horizontal Range " + usedRangeUnit, "Simulation graph's horizontal axis range."), m_horizontalRange.intValue, 10, 3000);
                m_verticalRange.intValue = EditorGUILayout.IntSlider(new GUIContent("Vertical Range " + usedRangeUnit, "Simulation graph's vertical axis range."), m_verticalRange.intValue, 10, 3000);
                m_shootingAngle.intValue = EditorGUILayout.IntSlider(new GUIContent("Angle (deg)", "Shooting angle towards vertical axis for the simulation."), m_shootingAngle.intValue, 0, 90);
                m_data.m_environmentProperties = EditorGUILayout.ObjectField("Environment Properties", m_data.m_environmentProperties, typeof(EnvironmentProperties), true) as EnvironmentProperties;

                if (m_data.m_environmentProperties != null)
                {
                    // Time, velocity & stability.
                    float remainingTime = m_lifeTime.floatValue;
                    float stepTime;
                    float travelTime = 0.0f;
                    float v0x = m_muzzleVelocityInMetric.floatValue * Mathf.Cos((float)-m_shootingAngle.intValue * Mathf.Deg2Rad);
                    float v0y = m_muzzleVelocityInMetric.floatValue * Mathf.Sin((float)m_shootingAngle.intValue * Mathf.Deg2Rad);
                    double stability = BallisticsUtility.GetStability(m_lengthInMetric.floatValue, m_diameterInMetric.floatValue, m_barrelTwistInMetric.floatValue,
                        m_massInMetric.floatValue, m_data.m_environmentProperties.m_airPressureInMetric, m_muzzleVelocityInMetric.floatValue, m_data.m_environmentProperties.m_temperatureInMetric);

                    // Vars needed for iterations & records.
                    Vector2 previousPos = new Vector2(verticalAxisRect.x, verticalAxisRect.y + verticalAxisRect.height / 2.0f);
                    Vector3 position = new Vector2(0.0f, 0.0f);
                    Vector3 velocity = new Vector3(0.0f, v0y, v0x);
                    Vector3 finalVelocity = Vector3.zero;
                    Vector3 previousDrift = Vector3.zero;
                    bool firstIteration = true;

                    // Initial kinetic energy.
                    float kineticEnergyInMetric = velocity.magnitude * velocity.magnitude * 0.5f * m_massInMetric.floatValue * 0.001f;

                    // Create list for storing points throughout the graph curve & add the initial point.
                    List<GraphPointInfo> graphPositions = new List<GraphPointInfo>();
                    GraphPointInfo initialPoint = new GraphPointInfo();
                    initialPoint.m_position = new Vector2(verticalAxisRect.x, verticalAxisRect.y + verticalAxisRect.height / 2.0f);
                    initialPoint.m_kineticEnergy = kineticEnergyInMetric;
                    initialPoint.m_velocity = m_muzzleVelocity.floatValue;
                    graphPositions.Add(initialPoint);

                    // Predetermine wind vector
                    Vector3 windVector = m_affectedByWind.boolValue ? BallisticsUtility.GetWindVector(m_data.m_environmentProperties.m_windSpeedInMetric, m_data.StepTime) : Vector3.zero;
                    Vector3 gravityVector = BallisticsUtility.GetGravity(m_data.m_environmentProperties.m_gravityInMetric, m_data.StepTime);

                    while (remainingTime > 0.0f)
                    {
                        // Advance time.
                        stepTime = remainingTime > m_data.StepTime ? m_data.StepTime : remainingTime;
                        travelTime += stepTime;

                        if (stepTime != m_data.StepTime)
                        {
                            // Re-calculate some vectors depending on new steptime.
                            windVector = m_affectedByWind.boolValue ? BallisticsUtility.GetWindVector(m_data.m_environmentProperties.m_windSpeedInMetric, stepTime) : Vector3.zero;
                            gravityVector = BallisticsUtility.GetGravity(m_data.m_environmentProperties.m_gravityInMetric, stepTime);
                        }

                        // Gravity.
                        velocity += gravityVector;

                        // Drag vector.
                        if (m_affectedByAirResistance.boolValue)
                            velocity -= BallisticsUtility.GetDragVector(velocity, (BallisticsUtility.DragGModel)m_dragModel.enumValueIndex, m_ballisticCoef.floatValue, stepTime);

                        // Convert to true velocity after gravity & drag vectors if affected by the wind.
                        if (m_affectedByWind.boolValue)
                            velocity += windVector;

                        // Kinetic Energy
                        kineticEnergyInMetric = velocity.magnitude * velocity.magnitude * 0.5f * m_massInMetric.floatValue * 0.001f;

                        // Final velocity depending on whether the results are requested in metrics or imperials.
                        finalVelocity = m_measurementUnit.enumValueIndex == 0 ? velocity * ConversionConstants.s_meterToYard : velocity;
                        position.x += finalVelocity.x * stepTime;
                        position.z += finalVelocity.z * stepTime;
                        position.y += finalVelocity.y * stepTime;

                        // Add spin drift to position if desired.
                        if (m_affectedBySpinDrift.boolValue)
                        {
                            Vector3 spinDrift = BallisticsUtility.GetSpinDrift(ref previousDrift, stability, stepTime, travelTime);
                            position += spinDrift * stepTime;
                        }

                        // X & Y positions for the graph.
                        float xpos = position.z.Remap(0, (float)m_horizontalRange.intValue, horizontalAxisRect.x, horizontalAxisRect.x + horizontalAxisRect.width);
                        float ypos = (-position.y).Remap(0, (float)m_verticalRange.intValue, verticalAxisRect.y, verticalAxisRect.y + verticalAxisRect.height);
                        ypos += verticalAxisRect.height / 2.0f;

                        // Whether the previous position was & current position is within the graph's rectangle.
                        bool isPreviousInBoundary = IsWithinBoundary(previousPos, horizontalAxisRect, verticalAxisRect);
                        bool isInBoundary = IsWithinBoundary(new Vector2(xpos, ypos), horizontalAxisRect, verticalAxisRect);
                        bool willDraw = false;

                        // Clamp & issue a draw order depending on whether the points are within rectangle or not.
                        if (!firstIteration)
                        {
                            if (isPreviousInBoundary && !isInBoundary)
                            {
                                xpos = Mathf.Clamp(xpos, horizontalAxisRect.x, horizontalAxisRect.x + horizontalAxisRect.width);
                                ypos = Mathf.Clamp(ypos, verticalAxisRect.y, verticalAxisRect.y + verticalAxisRect.height);
                                willDraw = true;
                            }
                            else if (isPreviousInBoundary && isInBoundary)
                                willDraw = true;
                        }
                        else
                        {
                            if (!isInBoundary)
                            {
                                xpos = Mathf.Clamp(xpos, horizontalAxisRect.x, horizontalAxisRect.x + horizontalAxisRect.width);
                                ypos = Mathf.Clamp(ypos, verticalAxisRect.y, verticalAxisRect.y + verticalAxisRect.height);
                            }
                            willDraw = true;
                        }


                        // Draw current point if desired.
                        if (willDraw)
                        {
                            // Handles setup.
                            Handles.color = axesColor;
                            Handles.DrawAAPolyLine(3.0f, new Vector3[] { new Vector3(previousPos.x, previousPos.y, 0), new Vector3(xpos, ypos, 0) });

                            // Save the point information to a list for the purposes of displaying information.
                            GraphPointInfo pInfo = new GraphPointInfo();
                            pInfo.m_position = new Vector2(xpos, ypos);
                            pInfo.m_kineticEnergy = kineticEnergyInMetric;
                            pInfo.m_time = travelTime;
                            pInfo.m_velocity = finalVelocity.magnitude;
                            graphPositions.Add(pInfo);

                        }

                        previousPos = new Vector2(xpos, ypos);
                        remainingTime -= m_data.StepTime;
                        if (firstIteration)
                            firstIteration = false;
                    }

                    // Determine which graph point to show information from.
                    int listItem = Mathf.FloorToInt(m_timeShown.floatValue.Remap(0.0f, 1.0f, 0, graphPositions.Count));
                    if (listItem > graphPositions.Count - 1)
                        listItem = graphPositions.Count - 1;
                    else if (listItem < 0)
                        listItem = 0;

                    // Draw an indicator rectangle on the point to show information from.
                    Rect timePointRect = new Rect(graphPositions[listItem].m_position.x, graphPositions[listItem].m_position.y, 4, 4);
                    EditorGUI.DrawRect(timePointRect, Color.red);

                    // Show point information.
                    Rect infoLabelRect = new Rect(horizontalAxisRect.x + 10, horizontalAxisRect.y - 85, 300, 25);
                    m_labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? m_infoTextColorPro : m_infoTextColorFree;

                    EditorGUI.LabelField(infoLabelRect, "Time: " + graphPositions[listItem].m_time + " (s)", m_labelStyle);
                    string usedInfoLengthUnit = m_measurementUnit.enumValueIndex == 0 ? " (y)" : " (m)";
                    string usedInfoEnergyUnit = m_measurementUnit.enumValueIndex == 0 ? " (ft-lbf)" : " (J)";

                    infoLabelRect.y += 13;
                    EditorGUI.LabelField(infoLabelRect, "Horizontal: " + graphPositions[listItem].m_position.x.Remap(horizontalAxisRect.x, horizontalAxisRect.x + horizontalAxisRect.width, 0, m_horizontalRange.intValue) + usedInfoLengthUnit, m_labelStyle);

                    infoLabelRect.y += 13;
                    EditorGUI.LabelField(infoLabelRect, "Vertical: " + -(graphPositions[listItem].m_position.y.Remap(verticalAxisRect.y + verticalAxisRect.height / 2.0f, verticalAxisRect.y + verticalAxisRect.height, 0, (float)m_verticalRange.intValue / 2.0f)) + usedInfoLengthUnit, m_labelStyle);

                    infoLabelRect.y += 13;
                    float ke = m_measurementUnit.enumValueIndex == 0 ? graphPositions[listItem].m_kineticEnergy * ConversionConstants.s_joulesToFootPound : graphPositions[listItem].m_kineticEnergy;
                    EditorGUI.LabelField(infoLabelRect, "Energy: " + ke + usedInfoEnergyUnit, m_labelStyle);

                    infoLabelRect.y += 13;
                    EditorGUI.LabelField(infoLabelRect, "Velocity: " + graphPositions[listItem].m_velocity + usedVelocityUnit, m_labelStyle);

                    // Reset the list.
                    graphPositions.Clear();
                }
                else
                {
                    Rect warningLabelRect = new Rect(horizontalAxisRect.x + 15, horizontalAxisRect.y - 20, 600, 25);
                    m_labelStyle.normal.textColor = Color.red;
                    EditorGUI.LabelField(warningLabelRect, "Please assign an environment property asset to show the results of the simulation.", m_labelStyle);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.HelpBox("Ballistic Simulation shows only an approximation, in the actual game values might be off by a small percent.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private bool IsWithinBoundary(Vector2 pos, Rect horizontalAxisRect, Rect verticalAxisRect)
        {
            return pos.x < horizontalAxisRect.x + horizontalAxisRect.width && pos.x > horizontalAxisRect.x && pos.y < verticalAxisRect.y + verticalAxisRect.height && pos.y > verticalAxisRect.y;
        }

        private void SetBulletPreset(int index)
        {

            if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.LongRifle22)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 1200.0f : 370.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 40.0f : 2.6f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.223f : 5.7f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.387f : 9.82f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 16.0f : 406.0f;
                m_ballisticCoef.floatValue = 0.084f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG38ACP)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 1150.0f : 350.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 115.0f : 7.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.356f : 9.0f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.380f : 10.1f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 16.0f : 406.0f;
                m_ballisticCoef.floatValue = 0.1f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG40SW)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 1130.0f : 340.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 165.0f : 10.69f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.400f : 10.2f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.285f : 7.24f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 16.0f : 406.0f;
                m_ballisticCoef.floatValue = 0.137f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG45ACP)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 835.0f : 255.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 230.0f : 15.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.452f : 11.5f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.377f : 9.6f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 16.0f : 406.0f;
                m_ballisticCoef.floatValue = 0.195f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG9x19mm)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 1180.0f : 360.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 115.0f : 7.45f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.355f : 9.01f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.415f : 10.5f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 10.0f : 254.0f;
                m_ballisticCoef.floatValue = 0.145f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Revolver357Magnum)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 1450.0f : 440.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 125.0f : 8.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.357f : 9.1f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.3f : 7.6f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 18.74f : 076.0f;
                m_ballisticCoef.floatValue = 0.169f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Revolver38Special)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 900.0f : 270.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 147.0f : 9.53f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.357f : 9.1f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.395f : 10.1f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 14.0f : 355.0f;
                m_ballisticCoef.floatValue = 0.151f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G1;
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Rifle223Rem)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 3750.0f : 1140.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 36.0f : 2.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.224f : 5.7f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.5f : 12.6f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 12.0f : 304.0f;
                m_ballisticCoef.floatValue = 0.257f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Rifle556NATO)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 3260.0f : 993.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 55.0f : 3.56f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.21f : 5.56f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.5f : 12.6f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 7.0f : 178.0f;
                m_ballisticCoef.floatValue = 0.151f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Rifle762x39Soviet)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 2396.0f : 730.3f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 122.0f : 7.9f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.3f : 7.62f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.681f : 17.3f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 9.45f : 240.0f;
                m_ballisticCoef.floatValue = 0.304f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR338Magnum)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 3360.0f : 1023.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 200.0f : 12.96f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.339f : 8.61f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.957f : 24.3f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 10.0f : 254.0f;
                m_ballisticCoef.floatValue = 0.405f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR50BMG)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 3044.0f : 928.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 647.0f : 42.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.510f : 13.0f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 1.54f : 39.0f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 15.0f : 380.0f;
                m_ballisticCoef.floatValue = 0.720f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR762x51NATO)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 2800.0f : 850.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 147.0f : 10.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.308f : 7.82f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.785f : 19.9f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 12.0f : 304.8f;
                m_ballisticCoef.floatValue = 0.397f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;

            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR7mmRemMagnum)
            {
                m_muzzleVelocity.floatValue = m_measurementUnit.enumValueIndex == 0 ? 3500.0f : 1100.0f;
                m_mass.floatValue = m_measurementUnit.enumValueIndex == 0 ? 110.0f : 8.0f;
                m_diameter.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.284f : 7.2f;
                m_length.floatValue = m_measurementUnit.enumValueIndex == 0 ? 0.79f : 20.0f;
                m_barrelTwist.floatValue = m_measurementUnit.enumValueIndex == 0 ? 9.0f : 228.0f;
                m_ballisticCoef.floatValue = 0.486f;
                m_dragModel.enumValueIndex = (int)BallisticsUtility.DragGModel.G7;
            }
            else
            {

            }

        }

        private string GetBulletPresetInfo(int index, ref string effectiveRange)
        {
            string presetInfo = "";

            if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.LongRifle22)
            {
                presetInfo = ".22 LR is used in rifles, pistols, revolvers and submachine guns. One of the most common hunting and shooting sports ammo. Effective at short ranges.";
                effectiveRange = "140 m / 150 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG38ACP)
            {
                presetInfo = ".38 ACP, also known as 9x23mmSR, is a powerful pistol round used in pistols such as Colt M1900 & M1911.";
                effectiveRange = "110 m / 120 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG40SW)
            {
                presetInfo = "A rimless pistol cartridge popular with law enforcement agencies in US, Canada & Australia. Used in pistols, semi-automatic pistols and some SMGs such as HK MP5 or UMP.";
                effectiveRange = "115 m / 125 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG45ACP)
            {
                presetInfo = "A straight-walled handgun ammo initially designed for Colt semi-automatic pistol. It's adopted by militaries around the world, most commonly used in M1911 pistols. 45 ACP is an effective combat pistol cartridge that combines accuracy & stoppoing power.";
                effectiveRange = "120 m / 130 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.PistolSMG9x19mm)
            {
                presetInfo = "One of the world's most popular and widely used military handgun and SMG cartridge, 9 mm is an highly effective round with a flat trajectory and moderate recoil.";
                effectiveRange = "100 m / 110 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Revolver357Magnum)
            {
                presetInfo = ".357 Magnum has an extremely effective terminal ballistics. It is considered to be an excellent hunting & self-defense round effective against variety of targets. Commonly used in hunting rifles as well as revolvers.";
                effectiveRange = "120 m / 130 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Revolver38Special)
            {
                presetInfo = "Known for its accuracy and manageable recoil, .38 special remains one of the most popuılar revolver cartridges in the world more than a century after its introduction. It is most commonly used for target shooting & competitions, personal defence and for hunting small game.";
                effectiveRange = "100 m / 110 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Rifle223Rem)
            {
                presetInfo = "Originally developed in 1957, a commercial hunting bullet for varmint hunting. Small bore cartridge that is the source for more powerful cartridges such ax 5.56 NATO. Mostly used for hunting purposes.";
                effectiveRange = "500 m / 550 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Rifle556NATO)
            {
                presetInfo = "556x45mm is a standardized second standard service rifle cartridge for NATO, as well as many non-NATO countries. Despite the criticism about it's lethality, stopping power and range, 556 is one of the world's most commonly used rifle ammunition for military purposes.";
                effectiveRange = "400 m / 440 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.Rifle762x39Soviet)
            {
                presetInfo = "Used mostly in SKS and AK-47 pattern rifles, as well as RPD & RPK light machine guns, 7.62 is possible the world's most popular ammunition, both for military and civilians. It is known for it's high accuracy and lethality in mid-long range distances.";
                effectiveRange = "400 m / 440 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR338Magnum)
            {
                presetInfo = "Initialy developed for long-range military snipers, .338 Lapua Magnum is a high-powered cardtridge that has proven it's effectiveness and power in multiple battles throghout the years.";
                effectiveRange = "640 m / 700 yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR50BMG)
            {
                presetInfo = "50 caliber cartridge developed for M2 Browning Machine gun, 50 BMG is also used in long-range sniper rifles as well as anti-materiel guns. 50 BMG is known to be an extremely powerful cartridge causing deep penetration & extreme lethality.";
                effectiveRange = "2000+ m / 2180+ yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR762x51NATO)
            {
                presetInfo = "Developed as a standard for small arms among NATO countries, used most commonly in marskman rifles & medium machine guns. Many versions of the cartridge is used throughout a number of countries for mainly long-range military purposes.";
                effectiveRange = "2000+ m / 2180+ yd ~";
            }
            else if ((BulletProperties.BulletPresets)index == BulletProperties.BulletPresets.SR7mmRemMagnum)
            {
                presetInfo = "Due to its flat shooting nature and moderate recoil, 7mm Remington Magnum is popular for big game hunting in Canada and US. It has also been chambered in sniper rifles used by the US Secret Service counter-sniper teams.";
                effectiveRange = "500 m / 550 yd ~";
            }
            else
            {
                presetInfo = "";
                effectiveRange = "";
            }

            return presetInfo;
        }
    }

}
