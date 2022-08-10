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
        if (GUILayout.Button("Load All Telemetry Files"))
        {
            m_TelemetryReader.LoadAllTelemetryFilesFromPath(m_TelemetryReader.FolderPath);
        }

    }
}
