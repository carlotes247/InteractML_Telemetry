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

        public IterationData GetOrStartIteration(string graphID, string modelID)
        {
            if (IMLIterations == null)
            {
                IMLIterations = new List<IterationData>();
            }
            // Check if we have already an iteration started for this model to return
            var existingIteration = GetIteration(graphID, modelID);
            if (existingIteration != null) return existingIteration;
                
            // If not, new model steering iteration started
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
            if (CurrentIteration == null) CurrentIteration = GetIteration(graphID, modelID);
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
            CurrentIteration = GetOrStartIteration(graphID, modelID);
        }

        /// <summary>
        /// Returns the latest available iteration by graphID, or additionally by modelID
        /// </summary>
        /// <param name="graphID"></param>
        /// <param name="searchUnfinished"></param>
        /// <returns></returns>
        public IterationData GetIteration(string graphID, string modelID = "", bool searchUnfinished = true, bool searchOldestMatch = true)
        {
            // search by both graph and model for an iteration
            IterationData data = null;
            // Discriminate for unfinished iterations
            if (IMLIterations != null && IMLIterations.Count > 0)
            {
                var timeToBeat = DateTime.Now;
                bool validEntry = false;
                // Iterate through list
                foreach (var iteration in IMLIterations)
                {
                    if (iteration.GraphID == graphID)
                    {
                        // Discriminate for modelID
                        if (!string.IsNullOrEmpty(modelID))
                        {
                            if (iteration.ModelData.ModelID == modelID) validEntry = true;
                            else validEntry = false;
                        }
                        // It needs to have at least a not null model data
                        else
                        {
                            if (iteration.ModelData != null) validEntry = true;
                            else validEntry = false;
                        }

                        // Discriminate for unfinished iterations
                        if (searchUnfinished)
                        {
                            if (iteration.TotalSeconds == 0) validEntry = true;
                            else validEntry = false;
                        }
                        // Discriminate for finished iterations
                        else
                        {
                            if (iteration.TotalSeconds > 0) validEntry = true;
                            else validEntry = false;
                        }


                        // If all conditions met, this is a valid iteration to return
                        if (validEntry)
                        {
                            // Discriminate for oldest available
                            if (searchOldestMatch)
                            {
                                if (iteration.StartTimeUTC < timeToBeat)
                                {
                                    data = iteration;
                                    timeToBeat = iteration.StartTimeUTC;
                                }
                            }
                            // If not, we return the current one, which is the first available
                            {
                                data = iteration;
                                return data;
                            }
                        }
                    }
                }
            }
            return data;
        }


        ///// <summary>
        ///// Returns iteration data
        ///// </summary>
        ///// <param name="graphID"></param>
        ///// <param name="modelID"></param>
        ///// <returns></returns>
        //public IterationData GetIteration (string graphID, string modelID, bool searchUnfinished = true, bool searchOldestMatch = true)
        //{
        //    // search by both graph and model for an iteration
        //    IterationData data = null;
        //    // Discriminate for unfinished iterations
        //    if (IMLIterations != null && IMLIterations.Count > 0 && searchUnfinished) 
        //        data = IMLIterations.Where(iteration => (iteration.GraphID == graphID) && (iteration.ModelData.ModelID == modelID) && (iteration.TotalSeconds == 0)).FirstOrDefault();
        //    // Discriminate for finished iterations
        //    else
        //        data = IMLIterations.Where(iteration => (iteration.GraphID == graphID) && (iteration.ModelData.ModelID == modelID) && (iteration.TotalSeconds > 0)).FirstOrDefault();

        //    // If we got a default returned, marked data as null to mark that there weren't any suitable elements (First() can lead to an InvalidOperationException)
        //    if (data == default(IterationData)) data = null;
        //    return data;
        //}

        /// <summary>
        /// Saves all possible training features from a training node (to be called everytime a training example is collected)
        /// </summary>
        /// <param name="trainingDataNode"></param>
        public void SaveAllPossibleTrainingFeatures (TrainingExamplesNode trainingDataNode)
        {
            if (CurrentIteration == null || string.IsNullOrEmpty(CurrentIteration.GraphID) || string.IsNullOrEmpty(CurrentIteration.ModelData.ModelID))
            {
                Debug.LogError("Trying to get a new iteration since there was a problem with the current one");
                CurrentIteration = GetIteration((trainingDataNode.graph as IMLGraph).ID);
            }
            if (CurrentIteration != null && trainingDataNode != null && trainingDataNode.InputFeatures != null)
            {
                // Get all GOs from training data node
                var trainingGOs = CurrentIteration.TryGetTrainingGameObjects(trainingDataNode);
                // Extract features from GOs
                SaveAllPossibleFeatures(trainingGOs, ref CurrentIteration.ModelData.AllPossibleTrainingFeaturesData, isTestingData: false);
            }
        }

        /// <summary>
        /// Saves all possible testing features from a model node (to be called everytime a testing example is collected)
        /// </summary>
        /// <param name="modelNode"></param>
        public void SaveAllPossibleTestingFeatures (MLSystem modelNode)
        {
            if (CurrentIteration == null) CurrentIteration = GetIteration((modelNode.graph as IMLGraph).ID, modelNode.id);
            if (CurrentIteration != null && modelNode != null && modelNode.InputFeatures != null)
            {
                // Get all GOs from Testing data node
                var testingGOs = CurrentIteration.TryGetTestingGameObjects(modelNode);
                // Extract features from GOs
                SaveAllPossibleFeatures(testingGOs, ref CurrentIteration.ModelData.AllPossibleTestingFeaturesData, isTestingData: true);
            }

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Saves all possibles movement features supported by InteractML into a list
        /// </summary>
        /// <param name="GOs"></param>
        /// <param name="features"></param>
        private void SaveAllPossibleFeatures(List<GameObject> GOs, ref List<FeatureTelemetry> features, bool isTestingData = false)
        {
            if (GOs == null) return;
            if (CurrentIteration == null) Debug.LogError("Can't save features if current iteration is null!");
            // Extract features from GOs
            foreach (var gameobject in GOs)
            {
                // Make sure list is init
                if (features == null)
                    features = new List<FeatureTelemetry>();
                // Get correct extractors
                Extractors.AllExtractors extractors = null;
                if (isTestingData) extractors = CurrentIteration.TryGetTestingExtractors(gameobject);
                else extractors = CurrentIteration.TryGetTrainingExtractors(gameobject);
                // If extractors are null, abort! something is wrong
                if (extractors == null) return;

                // Extract position
                FeatureTelemetry positionFeature = new FeatureTelemetry();
                positionFeature.AddAsPosition(gameobject);
                features.Add(positionFeature);
                // Extract VELOCITY pos
                var updatedVelocityArray = extractors.VelocityPosition.UpdateFeature(positionFeature); // needed to calculate velocity between frames
                Vector3 velocityVector3 = Vector3.zero;
                if (updatedVelocityArray != null && updatedVelocityArray.Length == 3)
                {
                    velocityVector3 = new Vector3(
                      updatedVelocityArray[0],
                      updatedVelocityArray[1],
                      updatedVelocityArray[2]);
                }
                FeatureTelemetry velocityPositionFeature = new FeatureTelemetry();
                velocityPositionFeature.AddAsVelocity(gameobject, velocityVector3, isRotation: false);
                features.Add(velocityPositionFeature);
                // Extract ACCELERATION pos
                FeatureTelemetry accelerationPositionFeature = new FeatureTelemetry();
                var updatedAccelerationArray = extractors.AccelerationPosition.UpdateFeature(velocityPositionFeature); // needed to calculate accel between frames
                Vector3 accelerationVector3 = Vector3.zero;
                if (updatedAccelerationArray != null && updatedAccelerationArray.Length == 3)
                {
                    accelerationVector3 = new Vector3(
                    updatedAccelerationArray[0],
                    updatedAccelerationArray[1],
                    updatedAccelerationArray[2]);
                }
                accelerationPositionFeature.AddAsAcceleration(gameobject, accelerationVector3, isRotation: false);
                features.Add(accelerationPositionFeature);

                // Extract rotation (Euler)
                FeatureTelemetry rotationFeatureEuler = new FeatureTelemetry();
                rotationFeatureEuler.AddAsRotation(gameobject, isEuler: true);
                features.Add(rotationFeatureEuler);
                // Extract velocity rotation (Euler)
                var updatedVelocityRotEulerArray = extractors.VelocityRotationEuler.UpdateFeature(rotationFeatureEuler); // needed to calculate velocity between frames
                Vector3 velocityV3RotEuler = Vector3.zero;
                if (updatedVelocityRotEulerArray != null && updatedVelocityRotEulerArray.Length == 3)
                {
                    velocityV3RotEuler.x = updatedVelocityRotEulerArray[0];
                    velocityV3RotEuler.y = updatedVelocityRotEulerArray[1];
                    velocityV3RotEuler.z = updatedVelocityRotEulerArray[2];
                }
                FeatureTelemetry velocityRotationFeature = new FeatureTelemetry();
                velocityRotationFeature.AddAsVelocity(gameobject, velocityV3RotEuler, isRotation: true);
                features.Add(velocityRotationFeature);
                // Extract acceleration rotation (Euler)
                var updatedAccelerationRotEulerArray = extractors.AccelerationRotationEuler.UpdateFeature(velocityRotationFeature); // needed to calculate accel between frames
                Vector3 accelV3RotEuler = Vector3.zero;
                if (updatedAccelerationRotEulerArray != null && updatedAccelerationRotEulerArray.Length == 3)
                {
                    accelV3RotEuler.x = updatedAccelerationRotEulerArray[0];
                    accelV3RotEuler.y = updatedAccelerationRotEulerArray[1];
                    accelV3RotEuler.z = updatedAccelerationRotEulerArray[2];
                }
                FeatureTelemetry accelerationRotationFeature = new FeatureTelemetry();
                accelerationRotationFeature.AddAsAcceleration(gameobject, accelV3RotEuler, isRotation: true);
                features.Add(accelerationRotationFeature);
                // Extract rotation (Quaternion)
                FeatureTelemetry rotationFeature = new FeatureTelemetry();
                rotationFeature.AddAsRotation(gameobject);
                features.Add(rotationFeature);
                // Extract velocity rotation (Quaternion)
                var updatedVelocityRotQuatArray = extractors.VelocityRotationQuat.UpdateFeature(rotationFeature); // needed to calculate velocity between frames
                Quaternion auxQuat = Quaternion.identity;
                if (updatedVelocityRotQuatArray != null && updatedVelocityRotQuatArray.Length == 4)
                {
                    auxQuat.x = updatedVelocityRotQuatArray[0];
                    auxQuat.y = updatedVelocityRotQuatArray[1];
                    auxQuat.z = updatedVelocityRotQuatArray[2];
                    auxQuat.w = updatedVelocityRotQuatArray[3];
                }
                FeatureTelemetry velocityRotationQuatFeature = new FeatureTelemetry();
                velocityRotationQuatFeature.AddAsVelocity(gameobject, auxQuat);
                features.Add(velocityRotationQuatFeature);
                // Extract acceleration rotation (Quaternion)
                var updatedAccelerationRotQuatArray = extractors.AccelerationRotationQuat.UpdateFeature(velocityRotationQuatFeature); // needed to calculate accel between frames
                Quaternion auxQuatAccel = Quaternion.identity;
                if (updatedAccelerationRotQuatArray != null && updatedAccelerationRotQuatArray.Length == 4)
                {
                    auxQuatAccel.x = updatedAccelerationRotQuatArray[0];
                    auxQuatAccel.y = updatedAccelerationRotQuatArray[1];
                    auxQuatAccel.z = updatedAccelerationRotQuatArray[2];
                    auxQuatAccel.w = updatedAccelerationRotQuatArray[3];
                }
                FeatureTelemetry accelerationRotationQuatFeature = new FeatureTelemetry();
                accelerationRotationQuatFeature.AddAsAcceleration(gameobject, auxQuatAccel);
                features.Add(accelerationRotationQuatFeature);
            }
        }

        #endregion


    }
}