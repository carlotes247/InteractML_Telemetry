using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Copy of telemetry data that can be loaded asynchronously as a JSON
    /// </summary>
    [Serializable]
    public class TelemetryDataJSONAsync
    {
        #region Variables

        public string SceneName;
        public string ProjectID;

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

        public void Initialize(string projectID, string sceneName)
        {
            ProjectID = projectID;
            SceneName = sceneName;
            if (IMLIterations == null) IMLIterations = new List<IterationData>();
        }

        public IterationData GetOrStartIteration(string graphID, string modelID)
        {
            if (IMLIterations == null)
            {
                IMLIterations = new List<IterationData>();
            }

            // Is the currentIteration containing any data?
            if (CurrentIteration != null && CurrentIteration.HasData())
            {
                // make sure to store current iteration to avoid data loss if there is any data
                if (!IMLIterations.Contains(CurrentIteration))
                {
                    IMLIterations.Add(CurrentIteration);
                }
            }

            // Check if we have already an iteration started for this model to return
            var existingIteration = GetIteration(graphID, modelID);
            if (existingIteration != null)
            {
                var timeDifference = (existingIteration.StartTimeUTC - DateTime.UtcNow).TotalHours;
                // Only return this iteration if it has been less than 4h since it was created. Any more time is considered as an obsolete iteration
                if (timeDifference < 4)
                    return existingIteration;
            }

            // If not, new model steering iteration started
            //var iterationData =  CreateInstance<IterationData>();
            var iterationData = new IterationData();
            iterationData.StartIteration(graphID, modelID);
            IMLIterations.Add(iterationData);
            return iterationData;
        }

        public void EndIteration(string graphID, string modelID, MLSystem modelNode = null)
        {
            if (IMLIterations == null) IMLIterations = new List<IterationData>();

            // Is the currentIteration containing any data?
            if (CurrentIteration != null && CurrentIteration.HasData())
            {
                // make sure to store current iteration to avoid data loss if there is any data
                if (!IMLIterations.Contains(CurrentIteration))
                {
                    IMLIterations.Add(CurrentIteration);
                }
            }

            CurrentIteration = null;
            // Get the iteration we are trying to end (if there isn't an iteration we start one)
            if (CurrentIteration == null) CurrentIteration = GetOrStartIteration(graphID, modelID);
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
            if (!IMLIterations.Contains(CurrentIteration)) IMLIterations.Add(CurrentIteration);
            NumIterations++;
            CurrentIteration = null;

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
                            if (iteration.ModelData != null) validEntry = true; // maybe this is a good place to 'claim' an empty iteration with all possible training features data in?
                            else validEntry = false;
                        }

                        // If all conditions met, this is a valid iteration to return
                        if (validEntry)
                        {
                            // Discriminate for unfinished iterations
                            if (searchUnfinished)
                            {
                                if (!iteration.IsFinished) validEntry = true;
                                else validEntry = false;
                            }
                            // Discriminate for finished iterations
                            else
                            {
                                if (iteration.IsFinished) validEntry = true;
                                else validEntry = false;
                            }

                            // Discriminate for oldest available
                            if (validEntry && searchOldestMatch)
                            {
                                if (iteration.StartTimeUTC < timeToBeat)
                                {
                                    data = iteration;
                                    timeToBeat = iteration.StartTimeUTC;
                                }
                            }
                            // If not, we return the current one, which is the first available
                            else if (validEntry && !searchOldestMatch)
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
        public void SaveAllPossibleTrainingFeatures(TrainingExamplesNode trainingDataNode)
        {
            if (CurrentIteration == null) CurrentIteration = GetOrStartIteration((trainingDataNode.graph as IMLGraph).ID, trainingDataNode.id);
            if (CurrentIteration == null || string.IsNullOrEmpty(CurrentIteration.GraphID) || string.IsNullOrEmpty(CurrentIteration.ModelData.ModelID))
            {
                //Debug.LogError("Trying to get a new iteration since there was a problem with the current one");

                // If the problem is that we don't have a model ID, or the modelID is the same as the training examples ID (this is an unclaimed iteration)...
                string modelID = CurrentIteration.ModelData.ModelID;
                if (CurrentIteration != null && !string.IsNullOrEmpty(CurrentIteration.GraphID) && (string.IsNullOrEmpty(modelID) // no modelID
                    || modelID == trainingDataNode.id)) // unclaimed trainingExamples iteration)
                {
                    // try to get the model ID from the the training examples node ONLY if it is connected to one model
                    List<MLSystem> modelNodes = new List<MLSystem>();
                    foreach (var outputPort in trainingDataNode.Outputs)
                    {
                        if (outputPort.IsConnected)
                        {
                            foreach (var connection in outputPort.GetConnections())
                            {
                                if (connection.node is MLSystem)
                                {
                                    var modelNode = connection.node as MLSystem;
                                    if (!modelNodes.Contains(modelNode))
                                        modelNodes.Add(connection.node as MLSystem);
                                }
                            }
                        }
                    }
                    // If only one mlsystem node, use that model ID to get or create an iteration
                    if (modelNodes.Count == 1 && modelNodes[0] != null)
                    {
                        if (string.IsNullOrEmpty(modelID))
                        {
                            modelID = modelNodes[0].id;
                        }
                        // If this was an unclaimed interation, change the modelID to the model found to claim this iteration
                        else
                        {
                            CurrentIteration.ModelData.ModelID = modelNodes[0].id;
                            modelID = CurrentIteration.ModelData.ModelID;
                        }

                    }
                    // If we still didn't manage to find a suitable model ID, we consider the id for this iteration a TRAINING EXAMPLES iteration only to avoid losing data since we need an id
                    else if (string.IsNullOrEmpty(modelID))
                    {
                        modelID = trainingDataNode.id;
                    }
                }

                CurrentIteration = GetOrStartIteration((trainingDataNode.graph as IMLGraph).ID, modelID);

            }
            if (CurrentIteration != null && trainingDataNode != null && trainingDataNode.InputFeatures != null && trainingDataNode.TargetValues != null)
            {
                // Get all GOs from training data node
                var trainingGOs = CurrentIteration.TryGetTrainingGameObjects(trainingDataNode);
                var trainingLabel = CurrentIteration.TryGetTargetValues(trainingDataNode);
                // Extract features from GOs
                SaveAllPossibleFeatures(trainingGOs, ref CurrentIteration.ModelData.AllPossibleTrainingFeaturesData, trainingLabel, isTestingData: false);
            }
        }

        /// <summary>
        /// Saves all possible testing features from a model node (to be called everytime a testing example is collected)
        /// </summary>
        /// <param name="modelNode"></param>
        public void SaveAllPossibleTestingFeatures(MLSystem modelNode)
        {
            if (CurrentIteration == null || !CurrentIteration.HasData()) CurrentIteration = GetOrStartIteration((modelNode.graph as IMLGraph).ID, modelNode.id);

            // If the current iteration is still completely empty, make sure is contained in the iteration list to avoid losing data
            if (!CurrentIteration.HasData() && !IMLIterations.Contains(CurrentIteration))
                IMLIterations.Add(CurrentIteration);

            if (CurrentIteration != null && modelNode != null && modelNode.InputFeatures != null)
            {
                // Get all GOs from Testing data node
                var testingGOs = CurrentIteration.TryGetTestingGameObjects(modelNode);
                var testingLabel = modelNode.GetCurrentTestingLabelFlat();
                // Extract features from GOs
                SaveAllPossibleFeatures(testingGOs, ref CurrentIteration.ModelData.AllPossibleTestingFeaturesData, testingLabel, isTestingData: true);
            }

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Saves all possibles movement features supported by InteractML into a list
        /// </summary>
        /// <param name="GOs"></param>
        /// <param name="features"></param>
        private void SaveAllPossibleFeatures(List<GameObject> GOs, ref List<FeatureTelemetry> features, float[] output, bool isTestingData = false)
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
                positionFeature.AddAsPosition(gameobject, output);
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
                velocityPositionFeature.AddAsVelocity(gameobject, velocityVector3, output, isRotation: false);
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
                accelerationPositionFeature.AddAsAcceleration(gameobject, accelerationVector3, output, isRotation: false);
                features.Add(accelerationPositionFeature);

                // Extract rotation (Euler)
                FeatureTelemetry rotationFeatureEuler = new FeatureTelemetry();
                rotationFeatureEuler.AddAsRotation(gameobject, output, isEuler: true);
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
                velocityRotationFeature.AddAsVelocity(gameobject, velocityV3RotEuler, output, isRotation: true);
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
                accelerationRotationFeature.AddAsAcceleration(gameobject, accelV3RotEuler, output, isRotation: true);
                features.Add(accelerationRotationFeature);
                // Extract rotation (Quaternion)
                FeatureTelemetry rotationFeature = new FeatureTelemetry();
                rotationFeature.AddAsRotation(gameobject, output);
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
                velocityRotationQuatFeature.AddAsVelocity(gameobject, auxQuat, output);
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
                accelerationRotationQuatFeature.AddAsAcceleration(gameobject, auxQuatAccel, output);
                features.Add(accelerationRotationQuatFeature);
            }
        }

        #endregion


    }
}