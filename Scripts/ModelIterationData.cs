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
        // Live features
        public List<string> FeaturesInUse;
        public List<string> GameObjectsInUse;
    }
}