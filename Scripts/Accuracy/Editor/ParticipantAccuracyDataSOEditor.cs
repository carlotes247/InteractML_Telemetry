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

        bool m_ModelsAccuraciesHistoriesOpen;
        bool[] m_ModelAccuracyHistoryOpen;
        bool[] m_ModelAccuracyOverTimeOpen;
        bool[] m_SingleAccuracyOpen;

        bool m_QuestionnaireDataOpen;
        bool[] m_QuestionnaireAnswersOpened;

        public override void OnInspectorGUI()
        {
            m_AccuracyFile = target as ParticipantAccuracyDataSO;

            base.OnInspectorGUI();

            #region Custom Foldout to see Timestamps

            // Models Accuracies Over Time Redrawing
            m_ModelsAccuraciesHistoriesOpen = EditorGUILayout.Foldout(m_ModelsAccuraciesHistoriesOpen, "[Custom Drawing] Models Accuracy Histories");
            if (m_ModelsAccuraciesHistoriesOpen && m_AccuracyFile.AccuracyData != null && m_AccuracyFile.AccuracyData.ModelsAccuracyHistories != null)
            {
                EditorGUI.indentLevel++;

                // are the foldout arrays matching num models?
                if (m_ModelAccuracyHistoryOpen == null || m_ModelAccuracyHistoryOpen.Length != m_AccuracyFile.AccuracyData.ModelsAccuracyHistories.Count)
                {
                    m_ModelAccuracyHistoryOpen = new bool[m_AccuracyFile.AccuracyData.ModelsAccuracyHistories.Count];
                }                
                for (int i = 0; i < m_ModelAccuracyHistoryOpen.Length; i++)
                {

                    var modelHistory = m_AccuracyFile.AccuracyData.ModelsAccuracyHistories[i];
                    
                    // foldout individual model
                    m_ModelAccuracyHistoryOpen[i] = EditorGUILayout.Foldout(m_ModelAccuracyHistoryOpen[i], $"{modelHistory.ModelID}");
                    if (m_ModelAccuracyHistoryOpen[i])
                    {
                        EditorGUI.indentLevel++;

                        // model details
                        EditorGUILayout.LabelField($"ModelID: {modelHistory.ModelID}");
                        EditorGUILayout.LabelField($"GraphID: {modelHistory.GraphID}");
                        EditorGUILayout.LabelField($"Scene: {modelHistory.SceneName}");

                        // are the foldout arrays matching num models?
                        if (m_ModelAccuracyOverTimeOpen == null || m_ModelAccuracyOverTimeOpen.Length != m_ModelAccuracyHistoryOpen.Length)
                        {
                            m_ModelAccuracyOverTimeOpen = new bool[m_ModelAccuracyHistoryOpen.Length];
                        }
                        // foldout history of individual model
                        m_ModelAccuracyOverTimeOpen[i] = EditorGUILayout.Foldout(m_ModelAccuracyOverTimeOpen[i], $"Accuracy Over Time: {modelHistory.AccuracyOverTime.Count} entries");
                        if (m_ModelAccuracyOverTimeOpen[i])
                        {
                            EditorGUI.indentLevel++;

                            // are the foldout arrays matching num accuracy entries?
                            if (m_SingleAccuracyOpen == null || m_SingleAccuracyOpen.Length != modelHistory.AccuracyOverTime.Count)
                            {
                                m_SingleAccuracyOpen = new bool[modelHistory.AccuracyOverTime.Count];
                            }
                            for (int j = 0; j < m_SingleAccuracyOpen.Length; j++)
                            {
                                var historyEntry = modelHistory.AccuracyOverTime[j];

                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField($"Accuracy: {historyEntry.Accuracy}");
                                EditorGUILayout.LabelField($"Timestamp: {historyEntry.TimeStamp.ToString()}");
                                EditorGUILayout.EndHorizontal();

                                //// foldout single accuracy
                                //m_SingleAccuracyOpen[j] = EditorGUILayout.Foldout(m_SingleAccuracyOpen[j], $"Entry: {j}");
                                //if (m_SingleAccuracyOpen[j])
                                //{
                                //    EditorGUI.indentLevel++;

                                //    EditorGUILayout.LabelField($"Accuracy: {historyEntry.Accuracy}");
                                //    EditorGUILayout.LabelField($"Timestamp: {historyEntry.TimeStamp.ToString()}");

                                //    EditorGUI.indentLevel--;
                                //}
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10f);

            #endregion

            #region Custom Foldout to see Questionnaire Answers

            // Models Accuracies Over Time Redrawing
            m_QuestionnaireDataOpen = EditorGUILayout.Foldout(m_QuestionnaireDataOpen, "[Custom Drawing] Questionnaire Data");
            if (m_QuestionnaireDataOpen && m_AccuracyFile.QuestionnaireData != null && m_AccuracyFile.QuestionnaireData.QuestionnaireAnswers != null)
            {
                EditorGUI.indentLevel++;

                // are the foldout arrays matching num answers?
                if (m_QuestionnaireAnswersOpened == null || m_QuestionnaireAnswersOpened.Length != m_AccuracyFile.QuestionnaireData.QuestionnaireAnswers.Count)
                {
                    m_QuestionnaireAnswersOpened = new bool[m_AccuracyFile.QuestionnaireData.QuestionnaireAnswers.Count];
                }
                for (int i = 0; i < m_QuestionnaireAnswersOpened.Length; i++)
                {
                    var answer = m_AccuracyFile.QuestionnaireData.QuestionnaireAnswers[i];
                    // Individual answer foldout
                    m_QuestionnaireAnswersOpened[i] = EditorGUILayout.Foldout(m_QuestionnaireAnswersOpened[i], $"{answer.Timestamp}");
                    if (m_QuestionnaireAnswersOpened[i])
                    {
                        EditorGUI.indentLevel++;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Accuracy: {answer.SubjectiveAccuracy}");
                        GUILayout.Label($"Enjoyment: {answer.Enjoyment}");
                        GUILayout.Label($"GameFeel: {answer.GameFeel}");
                        GUILayout.Label($"Controllability: {answer.Controllability}");     
                        GUILayout.EndHorizontal();


                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
            }


            #endregion

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

            // Load
            if (GUILayout.Button("Load from JSON (from same folder)"))
            {
                m_AccuracyFile.LoadDataFromJSON();
            }

            GUILayout.Space(10f);

            // Save
            if (GUILayout.Button("Save objective accuracy into JSON (in same folder)"))
            {
                m_AccuracyFile.SaveObjectiveAccuracyDataToJSON();
            }

        }
    }

}