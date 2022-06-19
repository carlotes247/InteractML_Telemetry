using System.Collections.Generic;
using UnityEngine;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Useful telemetry data per model per iteration
    /// </summary>
    [System.Serializable]
    public class ModelIterationData
    {
        /// <summary>
        /// Which graph does this model belong to?
        /// </summary>
        public string GraphID;
        /// <summary>
        /// Whic model?
        /// </summary>
        public string ModelID;
        // Training data
        public List<IMLTrainingExample> TrainingData;
        public List<string> TrainingFeatures;
        public List<string> TrainingGameObjects;
        // Live features
        public List<string> FeaturesInUse;
        public List<string> GameObjectsInUse;
        // Testing data
        public List<List<IMLTrainingExample>> TestingData;

        /// <summary>
        /// All possible training features (only gathered if training data collected in this iteration)
        /// </summary>
        public List<FeatureTelemetry> AllPossibleTrainingFeaturesData;
        // All possible testing features (only gathered if testing data collected in this iteration)
        /// <summary>
        /// Which ML System nodes are collecting testing features?
        /// </summary>
        public List<FeatureTelemetry> AllPossibleTestingFeaturesData;


    }
}