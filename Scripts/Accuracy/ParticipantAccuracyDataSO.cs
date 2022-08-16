using UnityEngine;
using System.Collections.Generic;
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

        public ParticipantAccuracyDataSO()
        {
            if (AccuracyData == null)
            {
                AccuracyData = new ParticipantAccuracyData();
            }
        }
    }
}