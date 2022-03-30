using System.Collections;
using UnityEngine;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Stores telemetry options and details
    /// </summary>
    public class TelemetryData : ScriptableObject
    {
        #region Variables

        public string ProjectName;
        
        public string SceneName;

        public string IMLGraphID;

        /// <summary>
        /// Which model are these details referring to?
        /// </summary>
        public string IMLSystemNodeID;
        /// <summary>
        /// Number of iterations performed in graph (clicked 
        /// </summary>
        public int IMLIterations;

        #endregion


    }
}