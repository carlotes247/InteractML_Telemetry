﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using InteractML.Addons; // this will be an addon
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace InteractML.Telemetry
{
    /// <summary>
    /// Controls when data starts being collected and when it stops being collected
    /// </summary>
    [RequireComponent(typeof(IMLComponent))]
    public class TelemetryController : MonoBehaviour, IAddonIML
    {

        #region Variables

        /// <summary>
        /// Initialized?
        /// </summary>
        [System.NonSerialized]
        private bool m_IsInit;

        /// <summary>
        /// Is the class collecting data?
        /// </summary>
        public bool CollectingData;

        /// <summary>
        /// Which ML component are we monitoring?
        /// </summary>
        [SerializeField]
        private IMLComponent m_MLComponent;

        /// <summary>
        /// Files containing the telemetry
        /// </summary>
        [SerializeField]
        private TelemetryData m_Data;

        /// <summary>
        /// User ID that we are collecting data from
        /// </summary>
        [Header("Saving Options"), SerializeField]
        private string m_ProjectID;

        /// <summary>
        /// Contains the ID + Scene as a directory where to store data
        /// </summary>
        [SerializeField]
        private string m_DataPath;
        /// <summary>
        /// Complete path to dataFile
        /// </summary>
        private string m_DataFilePath;
        /// <summary>
        /// Name of telemetry data file
        /// </summary>
        private string m_DataFileName;

        /// <summary>
        /// The SO containing telemetry data
        /// </summary>
        private TelemetryData m_TelemetryDataSO;

        /// <summary>
        /// Handles uploads to firebase server
        /// </summary>
        [SerializeField]
        private UploadController m_Uploader;

        /// <summary>
        /// Upload data after collecting?
        /// </summary>
        [SerializeField, Header("Upload Options")]
        private bool m_UploadData = false;
        [SerializeField]
        private bool m_UseTasksOnUpload = true;


        #endregion

        #region Unity Messages

        // Called before start
        void Awake()
        {
            Debug.Log("Awake Called");
        }

        private void OnEnable()
        {
            Debug.Log("OnEnable Called");
#if UNITY_EDITOR
            // Subscribe to the editor manager so that our update loop gets called
            // Subscription also calls initialize
            IMLEditorManager.SubscribeIMLAddon(this);
#else
            // Init will be called also on scene open, playmode enter, editmode enter
            Initialize();
#endif

        }

        private void Start()
        {
            Debug.Log("Start Called");
#if UNITY_EDITOR
            // In case the addon didn't subscribe...
            // Subscribe to the editor manager so that our update loop gets called
            // Subscription also calls initialize
            if (!IMLEditorManager.IsRegistered(this))            
                IMLEditorManager.SubscribeIMLAddon(this);
#endif
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (IMLEditorManager.IsRegistered(this))
            {
                IMLEditorManager.UnsubscribeIMLAddon(this);
            }
#endif
        }
        #endregion

        #region IMLAddon Events
        public void EditorUpdateLogic()
        {
            UpdateLogic();
        }

        public void EditorSceneOpened()
        {
            // Make sure to init
            Initialize();
        }

        public void EditorEnteredPlayMode()
        {
            Initialize();
        }

        public void EditorEnteredEditMode()
        {
            Initialize();
        }

        public void EditorExitingPlayMode()
        {
            // Do nothing
        }

        public void EditorExitingEditMode()
        {
            // Do nothing
        }

        public void AddAddonToGameObject(GameObject GO)
        {
            // Don't add telemetry controller if it is already there
            var telemetry = GO.GetComponent<TelemetryController>();
            if (telemetry == null)
            {
                telemetry = GO.AddComponent<TelemetryController>();
            }
        }

#endregion


#region Public Methods

        /// <summary>
        /// Is telemetry initialized?
        /// </summary>
        /// <returns></returns>
        public bool IsInit()
        {
            Debug.Log($"telemetry init? {m_IsInit}");
            return m_IsInit;
        }


        /// <summary>
        /// Initializes the class. Called both on editor time and runtime (through awake in the latter)
        /// </summary>
#if UNITY_EDITOR
        //[InitializeOnLoadMethod]
#endif
        public void Initialize()
        {          
            Debug.Log("Init telemetry called!");
            // Where data is going to be stored
            m_DataPath = IMLDataSerialization.GetDataPath() + "/Telemetry";

            // We don't have a project ID, throw error 
            if (String.IsNullOrEmpty(m_ProjectID))
            {
                Debug.LogError("Telemetry requires a project ID!");
            }
            else
            {
                // Save dataFilePath
                m_DataFileName = $"{m_ProjectID}_Telemetry";
                m_DataFilePath = Path.Combine(m_DataPath, m_DataFileName);


                // Load UserID (used in graph for data storage and identification of training set)
                if (m_Data == null)
                {
                    bool dataFound = false;
                    // Make sure directory exists
                    if (!Directory.Exists(m_DataPath))
                    {
                        Directory.CreateDirectory(m_DataPath);
                    }

                    // Try to load data file from telemetry data folder
#if UNITY_EDITOR
                    var assets = AssetDatabase.FindAssets("t:TelemetryData", new string[] { m_DataPath });
                    if (assets.Length > 0)
                    {
                        foreach (var assetGUID in assets)
                        {
                            var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                            if (assetPath.Contains(m_ProjectID))
                            {
                                // We found our telemetryData!
                                m_Data = AssetDatabase.LoadAssetAtPath<TelemetryData>(assetPath);
                                dataFound = true;
                                break; // no need to search anymore
                            }
                        }
                    }
#endif

                    // There is not a file yet, create a new file
                    if (m_Data == null && !dataFound)
                    {
                        m_Data = ScriptableObject.CreateInstance<TelemetryData>();
                        string assetPath = m_DataPath.Substring(m_DataFilePath.IndexOf("InteractML"));
                        Debug.Log($"Attempting to create TelemetryData file in: {assetPath}");
                        IMLDataSerialization.SaveObjectToDisk(m_Data, m_DataPath, m_DataFileName);
#if UNITY_EDITOR
                        //AssetDatabase.CreateAsset(m_Data, assetPath);
#endif
                        dataFound = true;

                    }                  
                }

                // Get reference to uploader
                if (m_Uploader == null) m_Uploader = FindObjectOfType<UploadController>();

                // Get ref to ml component
                if (m_MLComponent == null) m_MLComponent = GetComponent<IMLComponent>();

                // Unsubscribe telemetry first, then subscribe. To avoid duplicate calls
                UbsubscribeFromIMLEventDispatcher();
                SubscribeToIMLEventDispatcher();

                m_IsInit = true;

            }
        }

        public void UpdateLogic()
        {

        }

        /// <summary>
        /// Uploads data to server
        /// </summary>
        public void UploadData()
        {
            // If we have stopped collecting data and we need to upload data...
            if (!CollectingData && m_UploadData)
            {
                string userDataSetPath = IMLDataSerialization.GetTrainingExamplesDataPath() + "/" + m_DataPath;
                // Upload files from our IDString directory to firebase server
                m_Uploader.UploadAsync(userDataSetPath, m_DataPath + "/", useTasks: m_UseTasksOnUpload);
            }
        }

        #endregion

        #region Private Methods

        #region Subscriptions

        public void SubscribeToIMLEventDispatcher()
        {
            Debug.Log("subscribing telemetry events");
            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback += StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback += StopTrainingDataSetTelemetry;
            IMLEventDispatcher.ToggleRecordCallback += ToggleRecordingTelemetry;
            // Model telemetry
            // TO DO

            // Iteration finished
            IMLEventDispatcher.ModelSteeringIterationFinished += IterationFinished;

        }

        private void UbsubscribeFromIMLEventDispatcher()
        {
            Debug.Log("unsubscribing telemetry events");
            Debug.Log("Before subscription");

            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback -= StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback -= StopTrainingDataSetTelemetry;
            IMLEventDispatcher.ToggleRecordCallback -= ToggleRecordingTelemetry;
            // Model telemetry
            // TO DO

            // Iteration finished
            IMLEventDispatcher.ModelSteeringIterationFinished -= IterationFinished;


        }

        #endregion

        private bool ToggleRecordingTelemetry(string nodeID) 
        {
            Debug.Log("ToggleRecordingTelemetry called!");
            return true;
        }

        /// <summary>
        /// Starts collecting telemetry from a training examples node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private bool StartTrainingDataSetTelemetry(string nodeID)
        {
            Debug.Log("Start Training telemetry called!");
            // Is there any training examples node with that ID?
            if (m_MLComponent.TrainingExamplesNodesList.Where(tNode => tNode.id == nodeID).Any())
            {
                Debug.Log($"Starting training telemetry for node {nodeID}");
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Stops collecting telemetry from a training examples node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private bool StopTrainingDataSetTelemetry(string nodeID)
        {
            Debug.Log("Stop training telemetry called!");
            // Is there any training examples node with that ID?
            if (m_MLComponent.TrainingExamplesNodesList.Where(tNode => tNode.id == nodeID).Any())
            {
                Debug.Log($"Stopping training telemetry for node {nodeID}");
                return true;
            }
            else
            {
                return false;
            }

        }

        #region Iteration telemetry

        /// <summary>
        /// Records when a model steering iteration has been completed
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private bool IterationFinished(string nodeID)
        {
            bool success = false;
            if (m_MLComponent != null)
            {
                // Is there any model node with that ID?
                if (m_MLComponent.MLSystemNodeList.Where(tNode => tNode.id == nodeID).Any())
                {
                    Debug.Log($"Iteration finished by model node {nodeID}");
                    // Increase iterations by one
                    if (m_Data != null) m_Data.IMLIterations++;
                    success = true;
                }                
            }
            return success;
        }

        #endregion

        #endregion


    }

}
