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
using UnityEditor;

namespace IE.RSB
{

    [CustomEditor(typeof(EnvironmentProperties))]
    public class EnvironmentPropertiesEditor : Editor
    {
#pragma warning disable

        private SerializedProperty m_gravity;
        private SerializedProperty m_airPressure;
        private SerializedProperty m_temperature;
        private SerializedProperty m_windSpeed;
        private SerializedProperty m_gravityInMetric;
        private SerializedProperty m_airPressureInMetric;
        private SerializedProperty m_temperatureInMetric;
        private SerializedProperty m_windSpeedInMetric;
        private SerializedProperty m_measurementUnit;
        private SerializedProperty m_previousMeasurementUnit;
        private EnvironmentProperties m_data;

        private string[] m_gravityUnits = { "(ft/s2)", "(m/s2)" };
        private string[] m_windSpeedUnits = { "(mph)", "(kmh)" };
        private string[] m_pressureUnits = { "(inch hg)", "(kilopascal)" };
        private string[] m_tempUnits = { "(F)", "(C)" };

#pragma warning enable

        private void OnEnable()
        {
            m_data = (EnvironmentProperties)target;
            m_gravity = serializedObject.FindProperty("m_gravity");
            m_airPressure = serializedObject.FindProperty("m_airPressure");
            m_temperature = serializedObject.FindProperty("m_temperature");
            m_windSpeed = serializedObject.FindProperty("m_windSpeed");
            m_gravityInMetric = serializedObject.FindProperty("m_gravityInMetric");
            m_airPressureInMetric = serializedObject.FindProperty("m_airPressureInMetric");
            m_temperatureInMetric = serializedObject.FindProperty("m_temperatureInMetric");
            m_windSpeedInMetric = serializedObject.FindProperty("m_windSpeedInMetric");
            m_measurementUnit = serializedObject.FindProperty("m_measurementUnit");
            m_previousMeasurementUnit = serializedObject.FindProperty("m_previousMeasurementUnit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(m_measurementUnit, new GUIContent("Unit", "Units of measurement for the calculations."));

            if (m_previousMeasurementUnit.enumValueIndex != m_measurementUnit.enumValueIndex)
            {
                if (m_measurementUnit.enumValueIndex == (int)SniperAndBallisticsSystem.MeasurementUnits.Imperial)
                {
                    m_gravity.floatValue *= ConversionConstants.s_meterToFeet;
                    m_airPressure.floatValue *= ConversionConstants.s_pascalToInchMercury;
                    m_temperature.floatValue = ConversionConstants.CelciusToFahrenheit(m_temperature.floatValue);
                    m_windSpeed.vector3Value *= ConversionConstants.s_kmhToMph;
                }
                else
                {
                    m_gravity.floatValue /= ConversionConstants.s_meterToFeet;
                    m_airPressure.floatValue /= ConversionConstants.s_pascalToInchMercury;
                    m_temperature.floatValue = ConversionConstants.FahrenheitToCelcius(m_temperature.floatValue);
                    m_windSpeed.vector3Value /= ConversionConstants.s_kmhToMph;
                }

                m_previousMeasurementUnit.enumValueIndex = m_measurementUnit.enumValueIndex;
            }

            string usedGravityUnit = m_gravityUnits[m_measurementUnit.intValue];
            string usedDensityUnit = m_pressureUnits[m_measurementUnit.intValue];
            string usedTempUnit = m_tempUnits[m_measurementUnit.intValue];
            string usedWindUnit = m_windSpeedUnits[m_measurementUnit.intValue];

            EditorGUILayout.PropertyField(m_gravity, new GUIContent("Gravity " + usedGravityUnit , "World gravity acceleration in m/s2, default is -9.81 m/s2"));
            EditorGUILayout.PropertyField(m_temperature, new GUIContent("Temperature " + usedTempUnit , "World temperature, default is 25 C degrees."));
            EditorGUILayout.PropertyField(m_airPressure, new GUIContent("Air Pressure " + usedDensityUnit, "World air pressure affecting bullet stability factors, default is 101.325 kilopascals."));
            EditorGUILayout.PropertyField(m_windSpeed, new GUIContent("Wind Speed " + usedWindUnit, "Global wind speed, default is 0,0,0 km/h."));

            m_gravityInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_gravity.floatValue / ConversionConstants.s_meterToFeet : m_gravity.floatValue;
            m_temperatureInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? ConversionConstants.FahrenheitToCelcius(m_temperature.floatValue) : m_temperature.floatValue;
            m_airPressureInMetric.floatValue = m_measurementUnit.enumValueIndex == 0 ? m_airPressure.floatValue / ConversionConstants.s_pascalToInchMercury * 0.001f : m_airPressure.floatValue;
            m_windSpeedInMetric.vector3Value = m_measurementUnit.enumValueIndex == 0 ? m_windSpeed.vector3Value / ConversionConstants.s_kmhToMph : m_windSpeed.vector3Value;

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

    }
}
