using System.Collections.Generic;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Holds the history of accuracies of a model through iterations
    /// </summary>
    [System.Serializable]
    public struct ModelAccuracyHistory 
    {
        public string ModelID;
        public string GraphID;
        public string SceneName;
        public List<IterationAccuracy> AccuracyOverTime;
    }
}