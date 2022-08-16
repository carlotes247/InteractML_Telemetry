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
            if (m_TelemetryReader.CoroutineAsyncRunning)
                loadButtonText = $"Processing in progress... {m_TelemetryReader.FilesProcessedCoroutineAsyncNum}/{m_TelemetryReader.TotalFilesNum} Processed";
            else
                loadButtonText = $"Loading in progress... {m_TelemetryReader.FilesLoadedNum}/{m_TelemetryReader.TotalFilesNum} Loaded";
        }
        if (GUILayout.Button(loadButtonText))
        {
            m_TelemetryReader.LoadAllTelemetryFilesFromPath(m_TelemetryReader.FolderPath, useAsync: m_TelemetryReader.UseAsync);
        }
        GUI.enabled = true;
    }
}
