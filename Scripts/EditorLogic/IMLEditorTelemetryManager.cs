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
//        private static List<TelemetryController> m_IMLTelemetryCtrlrs;
//        public bool modeHasChanged { get; private set; }

//#if UNITY_EDITOR
//        // CALLBACKS FOR OTHER EDITOR SCRIPTS
//        /// <summary>
//        /// Public external editor callbacks for scene opened
//        /// </summary>
//        public static EditorSceneManager.SceneOpenedCallback SceneOpenedCallbacks;
//        /// <summary>
//        /// Public external editor callbacks for update
//        /// </summary>
//        public static EditorApplication.CallbackFunction UpdateCallbacks;
//        /// <summary>
//        /// Public external editor callbacks for playmodeStateChanged
//        /// </summary>
//        public static System.Action<PlayModeStateChange> PlayModeStateChangedCallbacks;
//#endif

        static IMLEditorTelemetryManager()
        {
#if UNITY_EDITOR

            // Adds a Telemetry Controller to the IMLSystem created through the editor Menu
            IMLEditorManager.IMLSystemCreatedCallback += AddTelemetryToIMLSystem;



            //// Subscribe this manager to the editor update loop
            //EditorApplication.update += UpdateLogic;
            //EditorApplication.update += UpdateCallbacks; // External

            //// Make sure the list is init
            //if (m_IMLTelemetryCtrlrs == null)
            //    m_IMLTelemetryCtrlrs = new List<TelemetryController>();

            //// When the project starts for the first time, we find the iml components present in that scene
            //FindTelemetryCtrlrs();

            ////Debug.Log("New IMLEditorManager created in scene " + EditorSceneManager.GetActiveScene().name);

            //// Subscribe manager event to the sceneOpened event
            //EditorSceneManager.sceneOpened += SceneOpenedLogic;
            //EditorSceneManager.sceneOpened += SceneOpenedCallbacks; // External

            //// Subscribe manager event to the playModeStateChanged event
            //EditorApplication.playModeStateChanged += PlayModeStateChangedLogic;
            //EditorApplication.playModeStateChanged += PlayModeStateChangedCallbacks; // External
#endif
        }

        private static void UpdateLogic()
        {
#if UNITY_EDITOR

            // Only run update logic when the app is not running (outside playmode or paused)
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                //if (m_IMLTelemetryCtrlrs != null && m_IMLTelemetryCtrlrs.Count > 0)
                //{
                //    //Debug.Log("IML Components number: " + m_IMLComponents.Count);

                //    // Repair list of known iml components if any of them is null
                //    if (NullTelemetryCtrlr()) RepairTelemetryCtrlrs();

                //    // Run each of the updates in the iml components
                //    foreach (var TelemetryCtrlr in m_IMLTelemetryCtrlrs)
                //    {
                //        //Debug.Log("**EDITOR**");
                //        if (TelemetryCtrlr != null)
                //            TelemetryCtrlr.UpdateLogic();
                //        else
                //            Debug.LogWarning("There is a null reference to a Telemetry Controller in IMLTelemetryEditorManager.UpdateLogic()");
                //    }
                //}

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


        //        /// <summary>
        //        /// When the scene opens we will clear and find all the imlComponents
        //        /// </summary>
        //        /// <param name="scene"></param>
        //        /// <param name="mode"></param>
        //        private static void SceneOpenedLogic(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        //        {

        //            ClearTelemetryCtrlrs();
        //            FindTelemetryCtrlrs();
        //            // Reload all models (if we can) when we enter playmode or when we come back to the editor
        //            foreach (var TelemetryCtrlr in m_IMLTelemetryCtrlrs)
        //            {
        //                if (TelemetryCtrlr != null)
        //                {
        //                }
        //                else
        //                {
        //                    Debug.LogWarning("There is a null reference to a Telemetry Controller in IMLTelemetryEditorManager.SceneOpenedLogic()");
        //                }
        //            }

        //        }


        //        /// <summary>
        //        /// When we change playmode, we make sure to reset all iml models
        //        /// </summary>
        //        /// <param name="playModeStatus"></param>
        //        private static void PlayModeStateChangedLogic(PlayModeStateChange playModeStatus)
        //        {
        //            // Repair list of known iml components if any of them is null
        //            if (NullTelemetryCtrlr()) RepairTelemetryCtrlrs();

        //            foreach (TelemetryController TelemetryCtrlr in m_IMLTelemetryCtrlrs)
        //            {

        //            }
        //            #region Enter Events

        //            // We load models if we are entering a playmode (not required when leaving playmode)
        //            if (playModeStatus == PlayModeStateChange.EnteredPlayMode)
        //            {

        //            }

        //            if (playModeStatus == PlayModeStateChange.EnteredEditMode)
        //            {
        //            }

        //            #endregion

        //            #region Exit Events

        //            // Remove any scriptNodes added during playtime when leaving playMode
        //            if (playModeStatus == PlayModeStateChange.ExitingPlayMode)
        //            {
        //            }

        //            // We stop models if we are leaving a playmode or editormode
        //            if (playModeStatus == PlayModeStateChange.ExitingEditMode || playModeStatus == PlayModeStateChange.ExitingPlayMode)
        //            {
        //            }

        //            #endregion

        //        }

        //#endif

        //        /// <summary>
        //        /// Clears the entire list of iml components (to be called when a new scene loads for example)
        //        /// </summary>
        //        private static void ClearTelemetryCtrlrs()
        //        {
        //            // Clear private list
        //            m_IMLTelemetryCtrlrs.Clear();
        //        }

        //        /// <summary>
        //        /// Finds all the iml components already present in the scene (to be called after 
        //        /// </summary>
        //        private static void FindTelemetryCtrlrs()
        //        {
        //            // Get all iml components in scene
        //            var componentsFound = Object.FindObjectsOfType<TelemetryController>();

        //            // If we found any components, try to subscribe them to the list
        //            if (componentsFound != null)
        //            {
        //                foreach (var component in componentsFound)
        //                {
        //                    SubscribeIMLComponent(component);
        //                }

        //            }
        //        }

        //        /// <summary>
        //        /// Repairs the list of known IML Components
        //        /// </summary>
        //        private static void RepairTelemetryCtrlrs()
        //        {
        //            ClearTelemetryCtrlrs();
        //            FindTelemetryCtrlrs();
        //        }

        //        /// <summary>
        //        /// Is any of the IMLComponents null?
        //        /// </summary>
        //        /// <returns></returns>
        //        private static bool NullTelemetryCtrlr()
        //        {
        //            return m_IMLTelemetryCtrlrs.Any(x => x == null) ? true : false;
        //        }

        //        /// <summary>
        //        /// Subscribes an imlcomponent to the list (avoiding duplicates)
        //        /// </summary>
        //        /// <param name="newComponentToAdd"></param>
        //        public static void SubscribeIMLComponent(TelemetryController newComponentToAdd)
        //        {
        //            // Make sure the list is initialised
        //            if (m_IMLTelemetryCtrlrs == null)
        //            {
        //                m_IMLTelemetryCtrlrs = new List<TelemetryController>();
        //            }

        //            // Make sure the list doesn't contain already the component we want to add
        //            if (!m_IMLTelemetryCtrlrs.Contains(newComponentToAdd))
        //            {
        //                // We add the component if it it is not in the list already
        //                m_IMLTelemetryCtrlrs.Add(newComponentToAdd);
        //            }
        //        }

        //        /// <summary>
        //        /// Unsubscribes an iml component from the list
        //        /// </summary>
        //        /// <param name="componentToRemove"></param>
        //        public static void UnsubscribeIMLComponent(TelemetryController componentToRemove)
        //        {
        //            // Make sure the list is initialised
        //            if (m_IMLTelemetryCtrlrs == null)
        //            {
        //                m_IMLTelemetryCtrlrs = new List<TelemetryController>();
        //            }

        //            // Make sure the list contains already the component we want to remove
        //            if (m_IMLTelemetryCtrlrs.Contains(componentToRemove))
        //            {
        //                // We remove the component from the list
        //                m_IMLTelemetryCtrlrs.Remove(componentToRemove);
        //            }


        //        }

#endif
    }

}
