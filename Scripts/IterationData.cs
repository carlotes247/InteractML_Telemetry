using System;
using System.Collections.Generic;
using UnityEngine;


namespace InteractML.Telemetry
{
    /// <summary>
    /// Useful telemetry data per iteration
    /// </summary>
    [System.Serializable]
    public class IterationData
    {
        public string GraphID;

        // Time data
        public DateTime StartTime;
        public DateTime EndTime;
        public double TotalSeconds;

        /// <summary>
        /// Useful telemetry data per model in graph
        /// </summary>
        public ModelIterationData ModelData;

        internal void StartIteration(string graphID, string modelID)
        {
            GraphID = graphID; // which graph
            StartTime = DateTime.UtcNow; // which time iteration started
            
            //ModelData = CreateInstance<ModelIterationData>();
            ModelData = new ModelIterationData();
            ModelData.GraphID = graphID;
            ModelData.ModelID = modelID;            
        }

        internal void EndIteration(string graphID, string modelID)
        {
            if (GraphID != graphID || modelID != ModelData.ModelID)
            {
                Debug.LogError($"Wrong iteration selected to end by telemetry... \n" +
                    $"GraphID passed: {graphID} | GraphID in iteration: {GraphID} \n" +
                    $"ModelID passed: {modelID} | ModelID in iteration: {ModelData.ModelID}");
                return; // do nothing
            }

            EndTime = DateTime.UtcNow;
            TotalSeconds = (EndTime - StartTime).TotalSeconds;

            Debug.Log($"Iteration finished by model node {modelID}");

        }
    }
}