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
        [SerializeField]
        public ParticipantAccuracyData AccuracyData;
        
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

        public void SaveData()
        {
            string ownPath = AssetDatabase.GetAssetPath(this);
            string fileName = Path.GetFileName(ownPath);
            string folderPath = Path.GetDirectoryName(ownPath);
            var task = AccuracyData.SaveToJSONAsync(folderPath, $"{this.name}.json");
            
        }

        public void LoadData()
        {
            string ownPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var result = AccuracyData.LoadFromJSONAsync(ownPath, $"{this.name}.json");
        }

    }
}