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
    [CustomEditor(typeof(BallisticSurface)), CanEditMultipleObjects]
    public class BallisticSurfaceEditor : Editor
    {

        private BallisticSurface m_data;
        private SerializedProperty m_preset;
        private SerializedProperty m_previousPreset;
        private SerializedProperty m_penetrationEnabled;
        private SerializedProperty m_penetrationEnergyConsumptionPercent;
        private SerializedProperty m_minEnergyToPenetrate;
        private SerializedProperty m_penetrationDeflectionAngles;
        private SerializedProperty m_ricochetEnabled;
        private SerializedProperty m_ricochetEnergyConsumptionPercent;
        private SerializedProperty m_minEnergyToRicochet;
        private SerializedProperty m_ricochetDeflectionAngles;

        // For editor.
        private SerializedProperty m_measurementUnit;
        private SerializedProperty m_previousMeasurementUnit;


        private void OnEnable()
        {
            m_data = (BallisticSurface)target;
            m_preset = serializedObject.FindProperty("m_preset");
            m_previousPreset = serializedObject.FindProperty("m_previousPreset");
            m_measurementUnit = serializedObject.FindProperty("m_measurementUnit");
            m_previousMeasurementUnit = serializedObject.FindProperty("m_previousMeasurementUnit");
            m_penetrationEnabled = serializedObject.FindProperty("m_penetrationEnabled");
            m_penetrationEnergyConsumptionPercent = serializedObject.FindProperty("m_penetrationEnergyConsumptionPercent");
            m_minEnergyToPenetrate = serializedObject.FindProperty("m_minEnergyToPenetrateInMetrics");
            m_penetrationDeflectionAngles = serializedObject.FindProperty("m_penetrationDeflectionAngles");
            m_ricochetEnabled = serializedObject.FindProperty("m_ricochetEnabled");
            m_ricochetEnergyConsumptionPercent = serializedObject.FindProperty("m_ricochetEnergyConsumptionPercent");
            m_minEnergyToRicochet = serializedObject.FindProperty("m_minEnergyToRicochetInMetrics");
            m_ricochetDeflectionAngles = serializedObject.FindProperty("m_ricochetDeflectionAngles");

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(m_preset, new GUIContent("Preset", "You can select a surface preset, or use a custom one of your own to mimic a particular surface."));

            if (m_preset.enumValueIndex != m_previousPreset.enumValueIndex)
            {
                PresetChanged();
                m_previousPreset.enumValueIndex = m_preset.enumValueIndex;
            }



            // Unit selection & property conversion.
            EditorGUILayout.PropertyField(m_measurementUnit, new GUIContent("Unit", "Units of measurement for the calculations."));

            if (m_previousMeasurementUnit.enumValueIndex != m_measurementUnit.enumValueIndex)
            {
                if (m_measurementUnit.enumValueIndex == (int)SniperAndBallisticsSystem.MeasurementUnits.Imperial)
                {
                    m_minEnergyToPenetrate.floatValue *= ConversionConstants.s_joulesToFootPound;
                    m_minEnergyToRicochet.floatValue *= ConversionConstants.s_joulesToFootPound;
                }
                else
                {
                    m_minEnergyToPenetrate.floatValue /= ConversionConstants.s_joulesToFootPound;
                    m_minEnergyToRicochet.floatValue /= ConversionConstants.s_joulesToFootPound;
                }

                m_previousMeasurementUnit.enumValueIndex = m_measurementUnit.enumValueIndex;
            }



            EditorGUILayout.EndVertical();

            string usedEnergyUnit = m_measurementUnit.enumValueIndex == 1 ? "(J)" : "(ft-lb)";
            // Penetration.
            EditorGUILayout.BeginVertical("GroupBox");
            if (m_preset.enumValueIndex != 0)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(m_penetrationEnabled, new GUIContent("Penetration Enabled", "Whether this surface is penetratable or not."));



            if (m_penetrationEnabled.boolValue)
            {
                m_penetrationEnergyConsumptionPercent.floatValue = EditorGUILayout.Slider(new GUIContent("Energy Consumption", "A bullet penetrating the object will lose this percent(1.0f = 100%) of it's energy each iteration during penetration. The longer the bullet stays inside the object, e.g the thicker the surface, more energy will be consumed. Higher consumption means higher chance of bullet being stuck within the object."), m_penetrationEnergyConsumptionPercent.floatValue, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(m_minEnergyToPenetrate, new GUIContent("Min Energy " + usedEnergyUnit, "A bullet whose kinetic energy is lower than this amount will not penetrate this object."));
                EditorGUILayout.PropertyField(m_penetrationDeflectionAngles, new GUIContent("Deflection Angles", "After the bullet penetrates the surface, it's direction will be deflected on all 3 axes depending on this minimum and maximum angle range."));

            }


            // Ricochet.

            EditorGUILayout.PropertyField(m_ricochetEnabled, new GUIContent("Ricochet Enabled", "Whether bullets can ricochet off this surface or not."));

            if (m_ricochetEnabled.boolValue)
            {
                m_ricochetEnergyConsumptionPercent.floatValue = EditorGUILayout.Slider(new GUIContent("Energy Consumption", "A bullet ricocheting off the object will lose this percent(1.0f = 100%) of it's velocity as soon as it hits the object."), m_ricochetEnergyConsumptionPercent.floatValue, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(m_minEnergyToRicochet, new GUIContent("Min Energy " + usedEnergyUnit, "A bullet whose kinetic energy is lower than this amount will not ricochet off this object."));
                EditorGUILayout.PropertyField(m_ricochetDeflectionAngles, new GUIContent("Deflection Angles", "After the bullet ricochets off the surface, it's direction will be deflected on all 3 axes depending on this minimum and maximum angle range."));
            }


            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_data);
        }

        private void PresetChanged()
        {

            if (m_preset.enumValueIndex != (int)BallisticSurface.SurfacePreset.Custom)
                m_measurementUnit.enumValueIndex = m_previousMeasurementUnit.enumValueIndex = (int)SniperAndBallisticsSystem.MeasurementUnits.Metric;

            if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.Cloth)
            {
                m_ricochetEnabled.boolValue = false;
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0;
                m_penetrationDeflectionAngles.vector2Value = Vector2.zero;
                m_minEnergyToPenetrate.floatValue = 0;
            }
            else if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.HardWood)
            {
                m_ricochetEnabled.boolValue = false;
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0.45f;
                m_minEnergyToPenetrate.floatValue = 700;
                m_penetrationDeflectionAngles.vector2Value = new Vector2(15, 35);
            }
            else if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.Wood)
            {
                m_ricochetEnabled.boolValue = false;
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0.3f;
                m_penetrationDeflectionAngles.vector2Value = new Vector2(10, 25);
                m_minEnergyToPenetrate.floatValue = 200;
            }
            else if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.LightSteel)
            {
                m_ricochetEnabled.boolValue = false;
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0.75f;
                m_minEnergyToPenetrate.floatValue = 1000;
                m_penetrationDeflectionAngles.vector2Value = new Vector2(0, 8);

            }
            else if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.Steel)
            {
                m_ricochetEnabled.boolValue = false;
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0.9f;
                m_minEnergyToPenetrate.floatValue = 2000;
                m_penetrationDeflectionAngles.vector2Value = new Vector2(0, 10);
            }
            else if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.Metal)
            {
                m_ricochetEnabled.boolValue = true;
                m_ricochetEnergyConsumptionPercent.floatValue = 0.6f;
                m_ricochetDeflectionAngles.vector2Value = new Vector2(10, 25);
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0.82f;
                m_minEnergyToPenetrate.floatValue = 1500;
                m_penetrationDeflectionAngles.vector2Value = new Vector2(7, 15);

            }
            else if (m_preset.enumValueIndex == (int)BallisticSurface.SurfacePreset.Plastic)
            {
                m_ricochetEnabled.boolValue = false;
                m_penetrationEnabled.boolValue = true;
                m_penetrationEnergyConsumptionPercent.floatValue = 0.1f;
                m_minEnergyToPenetrate.floatValue = 25;
                m_penetrationDeflectionAngles.vector2Value = Vector2.zero;

            }
        }

    }
}

