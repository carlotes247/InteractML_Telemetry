using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using InteractML.Addons; // this will be an addon
using System;
using System.IO;
using System.Reflection;
using ReusableMethods;
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

        /// <summary>
        /// Should start collecting all possible TRAINING features from a TTM node
        /// </summary>
        private bool m_CollectAllPossibleTrainingFeatures;
        /// <summary>
        /// Should start collecting all possible TESTING features from a MLS node
        /// </summary>
        private bool m_CollectAllPossibleTestingFeatures;

        /// <summary>
        /// Training example nodes collecting data at the moment
        /// </summary>
        private List<string> m_TTMsCollectingTrainingData;
        /// <summary>
        /// Model nodes collecting data at the moment
        /// </summary>
        private List<string> m_MLSCollectingTestingData;

        /// <summary>
        /// Variables for setting delay in time for collecting data
        /// </summary>
        [HideInInspector]
        public float StartDelay = 0.0f;
        [HideInInspector]
        public float CaptureRate = 10.0f;
        [HideInInspector]
        public float RecordTime = -1.0f;
        protected float m_TimeToNextCapture = 0.0f;
        protected float m_TimeToStopCapture = 0.0f;
        /// <summary>
        /// Timer used to collect examples at the same time that a collecting examples node
        /// </summary>
        protected TimerRecorder m_TimerTraining;

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


        # region Public Methods

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
            // We don't have a project ID, throw error 
            if (String.IsNullOrEmpty(m_ProjectID))
            {
                Debug.LogError("Telemetry requires a project ID!");
            }
            else
            {
                // Attempt to load or create telemetry data
                LoadOrCreateData();       

                // Get reference to uploader
                if (m_Uploader == null) m_Uploader = FindObjectOfType<UploadController>();

                // Get ref to ml component
                if (m_MLComponent == null) m_MLComponent = GetComponent<IMLComponent>();

                // init lists
                if (m_TTMsCollectingTrainingData == null) m_TTMsCollectingTrainingData = new List<string>();
                if (m_MLSCollectingTestingData == null) m_MLSCollectingTestingData = new List<string>();

                // Unsubscribe telemetry first, then subscribe. To avoid duplicate calls
                UnsubscribeFromIMLEventDispatcher();
                SubscribeToIMLEventDispatcher();

                m_IsInit = true;

            }
        }

        public void UpdateLogic()
        {
            if (m_Data == null || m_MLComponent == null) return;
            if (m_TimerTraining == null) m_TimerTraining = new TimerRecorder();
            if (m_CollectAllPossibleTrainingFeatures && m_TimerTraining.RecorderCountdown(1f, CaptureRate))
            {
                if (m_TTMsCollectingTrainingData == null) m_TTMsCollectingTrainingData = new List<string>();
                foreach (var ttmNodeID in m_TTMsCollectingTrainingData)
                {
                    TrainingExamplesNode ttmNode = m_MLComponent.TrainingExamplesNodesList.Where(node => node.id == ttmNodeID).FirstOrDefault();
                    m_Data.SaveAllPossibleTrainingFeatures(ttmNode);
                }
            }
            if (m_CollectAllPossibleTestingFeatures)
            {
                if (m_MLSCollectingTestingData == null) m_MLSCollectingTestingData = new List<string>();
                foreach (var mlsNodeID in m_TTMsCollectingTrainingData)
                {
                    MLSystem modelNode = m_MLComponent.MLSystemNodeList.Where(node => node.id == mlsNodeID).FirstOrDefault();
                    m_Data.SaveAllPossibleTestingFeatures(modelNode);
                }
            }
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

        #region Load/Save data

        private void SaveData()
        {
            // We don't have a project ID, throw error 
            if (String.IsNullOrEmpty(m_ProjectID))
            {
                Debug.LogError("Telemetry requires a project ID!");
            }
            else
            {
                // Save dataFilePath
                if (string.IsNullOrEmpty(m_DataPath) || string.IsNullOrEmpty(m_DataFileName) || string.IsNullOrEmpty(m_DataFilePath))
                {
                    // Where data is going to be stored
                    m_DataPath = Path.Combine(IMLDataSerialization.GetDataPath(), "Telemetry");
                    m_DataFileName = $"{m_ProjectID}_Telemetry";
                    m_DataFilePath = Path.Combine(m_DataPath, m_DataFileName);
                }
                // Make sure directory exists
                if (!Directory.Exists(m_DataPath))
                {
                    Directory.CreateDirectory(m_DataPath);
                }
                // There is not a file yet, create a new file
                if (m_Data == null)
                {
                    m_Data = ScriptableObject.CreateInstance<TelemetryData>();
                }
                // Save
                IMLDataSerialization.SaveObjectToDisk(m_Data, m_DataPath, m_DataFileName);
            }
        }

        private bool LoadData()
        {
            // We don't have a project ID, throw error 
            if (String.IsNullOrEmpty(m_ProjectID))
            {
                Debug.LogError("Telemetry requires a project ID!");
            }
            else
            {
                // Load dataFilePath
                if (string.IsNullOrEmpty(m_DataPath) || string.IsNullOrEmpty(m_DataFileName) || string.IsNullOrEmpty(m_DataFilePath))
                {
                    // Where data is going to be stored
                    m_DataPath = Path.Combine(IMLDataSerialization.GetDataPath(), "Telemetry");
                    m_DataFileName = $"{m_ProjectID}_Telemetry";
                    m_DataFilePath = Path.Combine(m_DataPath, m_DataFileName);
                }
                // Make sure directory exists
                if (!Directory.Exists(m_DataPath))
                {
                    Directory.CreateDirectory(m_DataPath);
                }
                // Load
                m_Data = IMLDataSerialization.LoadObjectFromDisk<TelemetryData>(m_Data, m_DataPath, m_DataFileName);
                Debug.Log($"Loaded telemetry data with values {m_Data}");
            }

            // true if loaded, false if failed
            return m_Data != null ? true : false;

        }

        private bool LoadOrCreateData()
        {
            // Attempt to load data
            bool dataFound = LoadData();
            // If failed to load, create a new file
            if (!dataFound)
            {
                m_Data = ScriptableObject.CreateInstance<TelemetryData>();
                dataFound = true;
            }
            return dataFound;
        }

        #endregion

        #region Subscriptions

        public void SubscribeToIMLEventDispatcher()
        {
            Debug.Log("subscribing telemetry events");
            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback += StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback += StopTrainingDataSetTelemetry;
            IMLEventDispatcher.ToggleRecordCallback += ToggleRecordingTrainingDataTelemetry;
            // Model telemetry
            // TO DO

            // Iteration started/finished
            IMLEventDispatcher.ModelSteeringIterationStarted += IterationStarted;
            IMLEventDispatcher.ModelSteeringIterationFinished += IterationFinished;

        }

        private void UnsubscribeFromIMLEventDispatcher()
        {
            Debug.Log("unsubscribing telemetry events");
            Debug.Log("Before subscription");

            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback -= StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback -= StopTrainingDataSetTelemetry;
            IMLEventDispatcher.ToggleRecordCallback -= ToggleRecordingTrainingDataTelemetry;
            // Model telemetry
            // TO DO

            // Iteration started/finished
            IMLEventDispatcher.ModelSteeringIterationStarted -= IterationStarted;
            IMLEventDispatcher.ModelSteeringIterationFinished -= IterationFinished;


        }

        #endregion

        #region Telemetry Collection

        private bool ToggleRecordingTrainingDataTelemetry(string nodeID) 
        {
            Debug.Log("ToggleRecordingTelemetry called!");
            // Make sure list is init
            if (m_TTMsCollectingTrainingData == null) m_TTMsCollectingTrainingData = new List<string>();
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
            if (!string.IsNullOrEmpty(nodeID) && m_MLComponent.TrainingExamplesNodesList.Where(tNode => tNode.id == nodeID).Any())
            {
                Debug.Log($"Starting training telemetry for node {nodeID}");
                // Make sure list is init
                if (m_TTMsCollectingTrainingData == null) m_TTMsCollectingTrainingData = new List<string>();
                // Add node to list to pull data from it in update
                if (!m_TTMsCollectingTrainingData.Contains(nodeID)) m_TTMsCollectingTrainingData.Add(nodeID);
                // Update flag to start pulling data in update
                if (m_TTMsCollectingTrainingData.Count > 0)
                {
                    m_CollectAllPossibleTrainingFeatures = true;
                    if (m_TimerTraining == null) m_TimerTraining = new TimerRecorder();
                    m_TimerTraining.PrepareTimer(StartDelay, RecordTime);
                }

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
            if (!string.IsNullOrEmpty(nodeID) && m_MLComponent.TrainingExamplesNodesList.Where(tNode => tNode.id == nodeID).Any())
            {
                Debug.Log($"Stopping training telemetry for node {nodeID}");
                // Make sure list is init
                if (m_TTMsCollectingTrainingData == null) m_TTMsCollectingTrainingData = new List<string>();
                // Remove node from list to stop pulling data from it in update
                if (m_TTMsCollectingTrainingData.Contains(nodeID))
                {
                    // TO DO: Clear all temporal internal lists from iteration data? (Maybe not needed)
                    m_TTMsCollectingTrainingData.Remove(nodeID);
                }
                // Update flag to stop pulling data in update
                if (m_TTMsCollectingTrainingData.Count == 0) m_CollectAllPossibleTrainingFeatures = false;
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Starts collecting telemetry from a Testing examples node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private bool StartTestingDataSetTelemetry(string nodeID)
        {
            Debug.Log("Start Testing telemetry called!");
            // Is there any Testing examples node with that ID?
            if (!string.IsNullOrEmpty(nodeID) && m_MLComponent.MLSystemNodeList.Where(mNode => mNode.id == nodeID).Any())
            {
                Debug.Log($"Starting Testing telemetry for node {nodeID}");
                // Make sure list is init
                if (m_MLSCollectingTestingData == null) m_MLSCollectingTestingData = new List<string>();
                // Add node to list to pull data from it in update
                if (!m_MLSCollectingTestingData.Contains(nodeID)) m_MLSCollectingTestingData.Add(nodeID);
                // Update flag to start pulling data in update
                if (m_MLSCollectingTestingData.Count > 0) m_CollectAllPossibleTestingFeatures = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Stops collecting telemetry from a Testing examples node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private bool StopTestingDataSetTelemetry(string nodeID)
        {
            Debug.Log("Stop Testing telemetry called!");
            // Is there any Testing examples node with that ID?
            if (!string.IsNullOrEmpty(nodeID) && m_MLComponent.MLSystemNodeList.Where(mNode => mNode.id == nodeID).Any())
            {
                Debug.Log($"Stopping Testing telemetry for node {nodeID}");
                // Make sure list is init
                if (m_MLSCollectingTestingData == null) m_MLSCollectingTestingData = new List<string>();
                // Remove node from list to stop pulling data from it in update
                if (m_MLSCollectingTestingData.Contains(nodeID))
                {
                    // TO DO: Clear all temporal internal lists from iteration data? (Maybe not needed)
                    m_MLSCollectingTestingData.Remove(nodeID);
                }
                // Update flag to stop pulling data in update
                if (m_MLSCollectingTestingData.Count == 0) m_CollectAllPossibleTestingFeatures = true;
                return true;
            }
            else
            {
                return false;
            }

        }

        #region Iteration telemetry

        /// <summary>
        /// Records when a model steering iteration has started
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        private bool IterationStarted(string modelID)
        {
            bool success = false;
            if (m_MLComponent != null)
            {

                // Is there any element with that ID?
                if (m_MLComponent.MLSystemNodeList.Where(tNode => tNode.id == modelID).Any())
                {
                    if (m_Data == null) LoadOrCreateData();

                    bool canStart = true;
                    // List null?
                    if (m_Data.IMLIterations == null) m_Data.IMLIterations = new List<IterationData>();

                    // Make sure we don't have an iteration started (and unfinished) for this model
                    if (m_Data.IMLIterations.Where(iteration => 
                    iteration != null 
                    && (iteration.ModelData != null) 
                    && (iteration.ModelData.ModelID != null && iteration.ModelData.ModelID == modelID) 
                    && (iteration.TotalSeconds == 0)).Any())
                        canStart = false;

                    if (canStart)
                    {
                        Debug.Log($"Iteration started by node {modelID}");
                        // Lets start an iteration!
                        m_Data.StartIteration(m_MLComponent.graph.ID, modelID);
                        success = true;
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Records when a model steering iteration has been completed
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        private bool IterationFinished(string modelID)
        {
            bool success = false;
            if (m_MLComponent != null)
            {
                // Is there any model node with that ID?
                if (m_MLComponent.MLSystemNodeList.Where(tNode => tNode.id == modelID).Any())
                {
                    if (m_Data != null) 
                    {
                        var modelNode = m_MLComponent.MLSystemNodeList.Where(tNode => tNode.id == modelID).First();


                        // Attempt to draw a box on top of MLS node?
#if UNITY_EDITOR
                        // First, before going into options below, I need to have an IML EDITOR GUI EVENT DISPATCHER
                        // I can extend IMLEditorManager with a set of events 

                        // Call here an editor event that calls the logic of drawing a box from the MLSEditor 
                        // That will require writing lots of code in MLSEditor that will only be called from TelemetryCtrler
                        // This is a bit dirty and can introduce problems
                        // i.e. where is the data stored? Is MLSEditor aware of the dataset in telemetry? 
                        // I would want to avoid creating a dataset inside the MLS node

                        // OR write a bunch of generic methods from MLSEditor that can be 'assembled' from here
                        // to form any form of UI. My guess is that will take longer
                        // I would also need to have a reference to the MLSEditor instance here... Which is not desirable                    


                        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                        var editorAttributes = assembly.CreateInstance("UnityEditor.CustomEditorAttributes");

                        var type = editorAttributes.GetType();
                        BindingFlags bf = BindingFlags.Static | BindingFlags.NonPublic;

                        MethodInfo findCustomEditorType = type.GetMethod("FindCustomEditorType", bf);
                        var customEditor = (Type)findCustomEditorType.Invoke(editorAttributes, new object[] { modelNode, false });
                        
#endif


                        // End iteration
                        m_Data.EndIteration(m_MLComponent.graph.ID, modelID, modelNode);
                    }


                    // Save data after an iteration
                    SaveData();
                    success = true;
                }                
            }
            return success;
        }




        #endregion

        #endregion

        #endregion


    }

}
