namespace InteractML.Telemetry
{
    /// <summary>
    /// Holds data linked with the accuracy of interation
    /// </summary>
    [System.Serializable]
    public struct IterationAccuracy 
    {
        /// <summary>
        /// Accuracy of model this iteration
        /// </summary>
        public float Accuracy;
        /// <summary>
        /// How many training examples at the end this iteration
        /// </summary>
        public int NumTrainingData;
        /// <summary>
        /// How many classes in trainingData
        /// </summary>
        public int NumUniqueClasses;
        /// <summary>
        /// How many features used in iteration
        /// </summary>
        public int NumFeatures;
        /// <summary>
        /// Which features used
        /// </summary>
        public string FeaturesNames;
        /// <summary>
        /// When did this iteration finished?
        /// </summary>
        public System.DateTime TimeStamp;
        
    }
}