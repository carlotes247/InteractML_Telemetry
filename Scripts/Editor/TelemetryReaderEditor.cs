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
        GUI.enabled = false;
    }
}
