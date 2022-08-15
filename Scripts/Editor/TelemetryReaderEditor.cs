using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InteractML.Telemetry;

[CustomEditor(typeof(TelemetryReader))]
public class TelemetryReaderEditor : Editor
{
    TelemetryReader m_TelemetryReader;    

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        m_TelemetryReader = target as TelemetryReader;

        GUILayout.Space(10f);        

        // BUTTONS
        // Load all telemetry
        string loadButtonText = "Load All Telemetry Files";
        if (m_TelemetryReader.LoadingStarted)
        {
            GUI.enabled = false;
            loadButtonText = $"Loading in progress... {m_TelemetryReader.FilesLoadedNum}/{m_TelemetryReader.TotalFilesNum} Files loaded...";
        }
        if (GUILayout.Button(loadButtonText))
        {
            m_TelemetryReader.LoadAllTelemetryFilesFromPath(m_TelemetryReader.FolderPath, useAsync: m_TelemetryReader.UseAsync);
        }
        GUI.enabled = true;

        // Calculate accuracy of one file
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Which File To Calculate Accuracy:");
        m_TelemetryReader.WhichFileToProcess = EditorGUILayout.IntField(m_TelemetryReader.WhichFileToProcess);
        GUILayout.EndHorizontal();
        if (GUILayout.Button($"Calculate Accuracy of File {m_TelemetryReader.WhichFileToProcess}"))
        {
            m_TelemetryReader.CalculateAccuracyOfTelemetryFile(m_TelemetryReader.WhichFileToProcess);
        }
    }
}
