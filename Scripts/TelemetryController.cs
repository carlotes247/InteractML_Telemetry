using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using InteractML.Addons; // this will be an addon
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
        private List<TelemetryData> m_Data;

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
            // Init will be called also on scene open, playmode enter, editmode enter
            Initialize();
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
        [InitializeOnLoadMethod]
#endif
        public void Initialize()
        {          
            Debug.Log("Init telemetry called!");
            // Where data is going to be stored
            m_DataPath = IMLDataSerialization.GetDataPath() + "/Telemetry";

            // Load UserID (used in graph for data storage and identification of training set)
            if (m_Data == null)
            {
                m_Data = new List<TelemetryData>();
            }

            // Get reference to uploader
            if (m_Uploader == null) m_Uploader = FindObjectOfType<UploadController>();

            // Get ref to ml component
            if (m_MLComponent == null) m_MLComponent = GetComponent<IMLComponent>();
            else Debug.LogError("Telemetry requires an IML Component to function!");

            // Unsubscribe telemetry first, then subscribe. To avoid duplicate calls
            UbsubscribeFromIMLEvents();
            SubscribeToIMLEvents();

            m_IsInit = true;
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

        private void SubscribeToIMLEvents()
        {
            Debug.Log("subscribing telemetry events");
            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback += StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback += StopTrainingDataSetTelemetry;
            // Model telemetry
            // TO DO

        }

        private void UbsubscribeFromIMLEvents()
        {
            Debug.Log("unsubscribing telemetry events");
            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback -= StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback -= StopTrainingDataSetTelemetry;
            // Model telemetry
            // TO DO

        }

        #endregion

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

        #endregion


    }

}
