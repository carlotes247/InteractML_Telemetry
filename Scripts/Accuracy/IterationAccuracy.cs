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
        /// When did this iteration finished?
        /// </summary>
        public System.DateTime TimeStamp;
        
    }
}