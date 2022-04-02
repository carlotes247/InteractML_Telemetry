using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractML;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace InteractML.Telemetry
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class IMLEditorTelemetryManager 
    {

        static IMLEditorTelemetryManager()
        {
#if UNITY_EDITOR

            // Adds a Telemetry Controller to the IMLSystem created through the editor Menu
            IMLEditorManager.IMLSystemCreatedCallback += AddTelemetryToIMLSystem;



#endif
        }

        private static void UpdateLogic()
        {
#if UNITY_EDITOR

            // Only run update logic when the app is not running (outside playmode or paused)
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {

            }

#endif

        }

#if UNITY_EDITOR

        /// <summary>
        /// Adds a Telemetry Controller to the IMLSystem created through the editor Menu
        /// </summary>
        /// <param name="imlComponent"></param>
        public static void AddTelemetryToIMLSystem(IMLComponent imlComponent)
        {
            // If we don't have telemetry together with the IMLComponent
            var telemetry = imlComponent.GetComponent<TelemetryController>();
            if (telemetry == null)
            {
                telemetry = imlComponent.gameObject.AddComponent<TelemetryController>();
            }
        }



#endif
    }

}
