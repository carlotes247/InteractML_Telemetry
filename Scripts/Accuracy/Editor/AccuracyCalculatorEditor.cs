using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace InteractML.Telemetry
{
    [CustomEditor(typeof(AccuracyCalculator))]
    public class AccuracyCalculatorEditor : Editor
    {
        AccuracyCalculator m_AccuracyCalc;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            m_AccuracyCalc = target as AccuracyCalculator;

            GUILayout.Space(10f);

            // BUTTONS
            // Calculate accuracy of one file
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Which Telemetry File To Calculate Accuracy:");
            m_AccuracyCalc.WhichFileToProcess = EditorGUILayout.IntField(m_AccuracyCalc.WhichFileToProcess);
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Calculate Accuracy of Single Telemetry File {m_AccuracyCalc.WhichFileToProcess}"))
            {
                m_AccuracyCalc.CalculateAccuracyOfTelemetryFile(m_AccuracyCalc.WhichFileToProcess, m_AccuracyCalc.TelemetryFileReader.TelemetryFiles);
            }

            GUILayout.Space(5f);

            // Calculate accuracy of all files            
            if (GUILayout.Button($"Calculate Accuracy of ALL Telemetry Files"))
            {
                m_AccuracyCalc.CalculateAccuracyOfAllTelemetryFiles(m_AccuracyCalc.TelemetryFileReader.TelemetryFiles, clearPreviousData: true);
            }

        }

    }

}
