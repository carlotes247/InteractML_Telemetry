using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace InteractML.Telemetry
{
    [CustomEditor(typeof(ParticipantAccuracyDataSO))]
    public class ParticipantAccuracyDataSOEditor : Editor
    {
        ParticipantAccuracyDataSO m_AccuracyFile;

        public override void OnInspectorGUI()
        {
            m_AccuracyFile = target as ParticipantAccuracyDataSO;

            base.OnInspectorGUI();

            // BUTTONS
            // Clear history
            if (GUILayout.Button("Clear All History"))
            {
                m_AccuracyFile.AccuracyData.ClearAllHistory();
            }

            // Order history by time
            if (GUILayout.Button("Sort history: earliest first"))
            {
                m_AccuracyFile.AccuracyData.SortHistoryByTime();
            }

            GUILayout.Space(10f);

            // Save
            if (GUILayout.Button("Save into JSON (in same folder)"))
            {
                m_AccuracyFile.SaveData();
            }

            // Load
            if (GUILayout.Button("Load from JSON (from same folder)"))
            {
                m_AccuracyFile.LoadData();
            }
        }
    }

}