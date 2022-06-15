using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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

        public string GraphID;

        /// <summary>
        /// How many iterations performed?
        /// </summary>
        public int NumIterations;
        /// <summary>
        /// Current Iteration that the telemetry controller is dealing with
        /// </summary>
        public IterationData CurrentIteration;
        /// <summary>
        /// Number of iterations performed in graph 
        /// </summary>
        public List<IterationData> IMLIterations;



        #endregion

        #region Public Methods

        public IterationData StartIteration(string graphID, string modelID)
        {
            if (IMLIterations == null)
            {
                IMLIterations = new List<IterationData>();
            }

            // new model steering iteration started
            //var iterationData =  CreateInstance<IterationData>();
            var iterationData =  new IterationData();
            iterationData.StartIteration(graphID, modelID);
            IMLIterations.Add(iterationData);
            return iterationData;
        }

        public void EndIteration(string graphID, string modelID, MLSystem modelNode = null)
        {
            if (IMLIterations == null) IMLIterations = new List<IterationData>();

            // Get the iteration we are trying to end
            if (CurrentIteration == null || string.IsNullOrEmpty(CurrentIteration.GraphID)) CurrentIteration = GetIteration(graphID, modelID);
            if (CurrentIteration == null || string.IsNullOrEmpty(CurrentIteration.GraphID)) 
            {
                Debug.LogError($"Telemetry trying to end an iteration that doesn't exists or is invalid! Graph: {graphID}, Model: {modelID}");
                return;
            }

            if (modelNode != null)
            {
                CurrentIteration.SaveLiveFeatures(modelNode);
                CurrentIteration.SaveTrainingData(modelNode);
                CurrentIteration.SaveTestingData(modelNode);
            }

            // End iteration
            CurrentIteration.EndIteration(graphID, modelID);
            NumIterations++;        
            
            // Start a new iteration!
            CurrentIteration = StartIteration(graphID, modelID);
        }

        /// <summary>
        /// Returns iteration data
        /// </summary>
        /// <param name="graphID"></param>
        /// <param name="modelID"></param>
        /// <returns></returns>
        public IterationData GetIteration (string graphID, string modelID, bool searchUnfinished = true)
        {
            // search by both graph and model for an iteration
            IterationData data = null;
            // Discriminate for unfinished iterations
            if (IMLIterations != null && IMLIterations.Count > 0 && searchUnfinished) 
                data = IMLIterations.Where(iteration => (iteration.GraphID == graphID) && (iteration.ModelData.ModelID == modelID) && (iteration.TotalSeconds == 0)).FirstOrDefault();
            // Discriminate for finished iterations
            else
                data = IMLIterations.Where(iteration => (iteration.GraphID == graphID) && (iteration.ModelData.ModelID == modelID) && (iteration.TotalSeconds > 0)).FirstOrDefault();

            // If we got a default returned, marked data as null to mark that there weren't any suitable elements (First() can lead to an InvalidOperationException)
            if (data == default(IterationData)) data = null;
            return data;
        }

        #endregion

        #region Private Methods


        #endregion


    }
}