using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InteractML.Telemetry
{
    /// <summary>
    /// Scriptable object storing ParticipantAccuracyData
    /// </summary>
    [CreateAssetMenu(menuName = "InteractML/Telemetry/AccuracyData")]
    public class ParticipantAccuracyDataSO : ScriptableObject
    {
        /// <summary>
        /// Objective Accuracy Data
        /// </summary>
        [SerializeField]
        public ParticipantAccuracyData AccuracyData;
        /// <summary>
        /// Questionnaire answers
        /// </summary>
        [SerializeField]
        public ParticipantQuestionnaireData QuestionnaireData;

        public bool SavingData { get => m_SavingData; }
        [System.NonSerialized]
        private bool m_SavingData;
        public bool LoadingData { get => m_LoadingData; }
        [System.NonSerialized]
        private bool m_LoadingData;

        public ParticipantAccuracyDataSO()
        {
            if (AccuracyData == null)
            {
                AccuracyData = new ParticipantAccuracyData();
            }
        }

        public void SaveObjectiveAccuracyDataToJSON()
        {
            string ownPath = AssetDatabase.GetAssetPath(this);
            string folderPath = Path.GetDirectoryName(ownPath);
            var savingTask = AccuracyData.SaveToJSONAsync(folderPath, $"{this.name}.json");            
        }

        IEnumerator SaveToJSONAsyncCoroutine(Task savingTask)
        {
            m_SavingData = true;
            while (!savingTask.IsCompleted)
            {
                yield return null;
            }
            m_SavingData = false;
            Debug.Log("Saving data to JSON completed!");
            yield break;
        }

        public void LoadDataFromJSON()
        {
            string ownPath = AssetDatabase.GetAssetPath(this);
            string folderPath = Path.GetDirectoryName(ownPath);
            var accuracyLoadingTask = AccuracyData.LoadFromJSONAsync(folderPath, $"{this.name}.json");
            string questionnaireName = this.name.Replace("Accuracy", "Questionnaire");
            var questionnaireLoadingTask = QuestionnaireData.LoadAnswersFromJSONAsync(folderPath, $"{questionnaireName}.json");
        }


        IEnumerator LoadFromJSONAsyncCoroutine(Task loadingTask)
        {
            m_LoadingData = true;
            while (!loadingTask.IsCompleted)
            {
                yield return null;
            }
            m_LoadingData = false;
            Debug.Log("JSON File Loaded!");
            yield break;
        }
    }
}