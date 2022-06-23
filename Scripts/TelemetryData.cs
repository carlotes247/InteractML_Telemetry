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
                CurrentIteration = GetIteration((trainingDataNode.graph as IMLGraph).ID);
            if (CurrentIteration != null && trainingDataNode != null && trainingDataNode.InputFeatures != null)
            {
                Debug.Log("Save all possible training features called!");

                // Get all GOs from training data node
                var trainingGOs = CurrentIteration.TryGetTrainingGameObjects(trainingDataNode);

                // Extract features from GOs
                foreach (var gameobject in trainingGOs)
                {
                    // Make sure list is init
                    if (CurrentIteration.ModelData.AllPossibleTrainingFeaturesData == null)
                        CurrentIteration.ModelData.AllPossibleTrainingFeaturesData = new List<FeatureTelemetry>();
                    // Extract position
                    FeatureTelemetry positionFeature = new FeatureTelemetry();
                    positionFeature.AddAsPosition(gameobject);
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(positionFeature);
                    // Extract VELOCITY pos
                    var extractors = CurrentIteration.TryGetTrainingExtractors(gameobject);
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
                    velocityPositionFeature.AddAsVelocity(gameobject, velocityVector3 , isRotation: false);
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(velocityPositionFeature);
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
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(accelerationPositionFeature);

                    // Extract rotation (Euler)
                    FeatureTelemetry rotationFeatureEuler = new FeatureTelemetry();
                    rotationFeatureEuler.AddAsRotation(gameobject, isEuler: true);
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(rotationFeatureEuler);
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
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(velocityRotationFeature);
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
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(accelerationRotationFeature);
                    // Extract rotation (Quaternion)
                    FeatureTelemetry rotationFeature = new FeatureTelemetry();
                    rotationFeature.AddAsRotation(gameobject);
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(rotationFeature);
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
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(velocityRotationQuatFeature);
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
                    CurrentIteration.ModelData.AllPossibleTrainingFeaturesData.Add(accelerationRotationQuatFeature);
                }

            }
        }

        /// <summary>
        /// Saves all possible testing features from a model node (to be called everytime a testing example is collected)
        /// </summary>
        /// <param name="modelNode"></param>
        public void SaveAllPossibleTestingFeatures (MLSystem modelNode)
        {
            if (CurrentIteration != null && modelNode != null && modelNode.InputFeatures != null)
            {
                // Get all GOs from Testing data node
                var testingGOs = CurrentIteration.TryGetTestingGameObjects(modelNode);

                // Extract features from GOs
                foreach (var gameobject in testingGOs)
                {
                    // Make sure list is init
                    if (CurrentIteration.ModelData.AllPossibleTestingFeaturesData == null)
                        CurrentIteration.ModelData.AllPossibleTestingFeaturesData = new List<FeatureTelemetry>();
                    // Extract position
                    FeatureTelemetry positionFeature = new FeatureTelemetry();
                    positionFeature.AddAsPosition(gameobject);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(positionFeature);
                    // Extract VELOCITY pos
                    var extractors = CurrentIteration.TryGetTestingExtractors(gameobject);
                    var updatedVelocityArray = extractors.VelocityPosition.UpdateFeature(positionFeature); // needed to calculate velocity between frames
                    Vector3 velocityVector3 = new Vector3(
                        updatedVelocityArray[0],
                        updatedVelocityArray[1],
                        updatedVelocityArray[2]);
                    FeatureTelemetry velocityPositionFeature = new FeatureTelemetry();
                    velocityPositionFeature.AddAsVelocity(gameobject, velocityVector3, isRotation: false);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(velocityPositionFeature);
                    // Extract ACCELERATION pos
                    FeatureTelemetry accelerationPositionFeature = new FeatureTelemetry();
                    var updatedAccelerationArray = extractors.AccelerationPosition.UpdateFeature(velocityPositionFeature); // needed to calculate accel between frames
                    Vector3 accelerationVector3 = new Vector3(
                        updatedAccelerationArray[0],
                        updatedAccelerationArray[1],
                        updatedAccelerationArray[2]);
                    accelerationPositionFeature.AddAsAcceleration(gameobject, accelerationVector3, isRotation: false);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(accelerationPositionFeature);

                    // Extract rotation (Euler)
                    FeatureTelemetry rotationFeatureEuler = new FeatureTelemetry();
                    rotationFeatureEuler.AddAsRotation(gameobject, isEuler: true);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(rotationFeatureEuler);
                    // Extract velocity rotation (Euler)
                    var updatedVelocityRotEulerArray = extractors.VelocityRotationEuler.UpdateFeature(rotationFeatureEuler); // needed to calculate velocity between frames
                    Vector3 velocityV3RotEuler = new Vector3();
                    velocityV3RotEuler.x = updatedVelocityRotEulerArray[0];
                    velocityV3RotEuler.y = updatedVelocityRotEulerArray[1];
                    velocityV3RotEuler.z = updatedVelocityRotEulerArray[2];
                    FeatureTelemetry velocityRotationFeature = new FeatureTelemetry();
                    velocityRotationFeature.AddAsVelocity(gameobject, velocityV3RotEuler, isRotation: true);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(velocityRotationFeature);
                    // Extract acceleration rotation (Euler)
                    var updatedAccelerationRotEulerArray = extractors.AccelerationRotationEuler.UpdateFeature(velocityRotationFeature); // needed to calculate accel between frames
                    Vector3 accelV3RotEuler = new Vector3();
                    accelV3RotEuler.x = updatedAccelerationRotEulerArray[0];
                    accelV3RotEuler.y = updatedAccelerationRotEulerArray[1];
                    accelV3RotEuler.z = updatedAccelerationRotEulerArray[2];
                    FeatureTelemetry accelerationRotationFeature = new FeatureTelemetry();
                    accelerationRotationFeature.AddAsAcceleration(gameobject, accelV3RotEuler, isRotation: true);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(accelerationRotationFeature);
                    // Extract rotation (Quaternion)
                    FeatureTelemetry rotationFeature = new FeatureTelemetry();
                    rotationFeature.AddAsRotation(gameobject);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(rotationFeature);
                    // Extract velocity rotation (Quaternion)
                    var updatedVelocityRotQuatArray = extractors.VelocityRotationQuat.UpdateFeature(rotationFeature); // needed to calculate velocity between frames
                    Quaternion auxQuat = new Quaternion();
                    auxQuat.x = updatedVelocityRotQuatArray[0];
                    auxQuat.y = updatedVelocityRotQuatArray[1];
                    auxQuat.z = updatedVelocityRotQuatArray[2];
                    auxQuat.w = updatedVelocityRotQuatArray[3];
                    FeatureTelemetry velocityRotationQuatFeature = new FeatureTelemetry();
                    velocityRotationQuatFeature.AddAsVelocity(gameobject, auxQuat);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(velocityRotationQuatFeature);
                    // Extract acceleration rotation (Quaternion)
                    var updatedAccelerationRotQuatArray = extractors.AccelerationRotationQuat.UpdateFeature(velocityRotationQuatFeature); // needed to calculate accel between frames
                    Quaternion auxQuatAccel = new Quaternion();
                    auxQuatAccel.x = updatedAccelerationRotQuatArray[0];
                    auxQuatAccel.y = updatedAccelerationRotQuatArray[1];
                    auxQuatAccel.z = updatedAccelerationRotQuatArray[2];
                    auxQuatAccel.w = updatedAccelerationRotQuatArray[3];
                    FeatureTelemetry accelerationRotationQuatFeature = new FeatureTelemetry();
                    accelerationRotationQuatFeature.AddAsAcceleration(gameobject, auxQuatAccel);
                    CurrentIteration.ModelData.AllPossibleTestingFeaturesData.Add(accelerationRotationQuatFeature);
                }

            }

        }

        #endregion

        #region Private Methods


        #endregion


    }
}