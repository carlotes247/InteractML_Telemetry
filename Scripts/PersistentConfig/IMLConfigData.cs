using System.Collections;
using UnityEngine;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Contains persistent config info about InteractML
    /// </summary>
    public class IMLConfigData 
    {
        /// <summary>
        /// Unique ID per InteractML installation. Different from UnityEditor.PlayerSettings.productGUID
        /// Used to identify different computers
        /// </summary>
        public string ProjectID;
    }
}