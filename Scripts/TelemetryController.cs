using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace InteractML.Telemetry
{
    /// <summary>
    /// Controls when data starts being collected and when it stops being collected
    /// </summary>
    [RequireComponent(typeof(IMLComponent))]
    public class TelemetryController : MonoBehaviour
    {

        #region Variables

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
            // Where data is going to be stored
            m_DataPath = IMLDataSerialization.GetDataPath() + "/Telemetry";

            // Load UserID (used in graph for data storage and identification of training set)
            if (m_Data == null)
            {
                m_Data = new List<TelemetryData>();
            }

            // Get reference to uploader
            if(m_Uploader == null) m_Uploader = FindObjectOfType<UploadController>();

            // Get ref to ml component
            if (m_MLComponent == null) m_MLComponent = GetComponent<IMLComponent>();
            else Debug.LogError("Telemetry requires an IML Component to function!");

            // Training Examples telemetry
            IMLEventDispatcher.StartRecordCallback += StartTrainingDataSetTelemetry;
            IMLEventDispatcher.StopRecordCallback += StopTrainingDataSetTelemetry;
            // Model telemetry
            // TO DO

        }
        #endregion

        #region Public Methods

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

        /// <summary>
        /// Starts collecting telemetry from a training examples node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private bool StartTrainingDataSetTelemetry(string nodeID)
        {
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
