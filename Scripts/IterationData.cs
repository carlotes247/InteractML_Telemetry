using ReusableMethods;
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

        /// <summary>
        /// Saves lives features(and GOs) connected to modelNode
        /// </summary>
        /// <param name="modelNode"></param>
        internal void SaveLiveFeatures(MLSystem modelNode)
        {
            // Get Features and GameObjects in this iteration
            if (modelNode != null)
            {
                // Make sure list is init
                if (ModelData.FeaturesInUse == null)
                    ModelData.FeaturesInUse = new List<string>();

                // save features and GOs (inside method)
                SaveFeatures(modelNode.InputFeatures, ref ModelData.FeaturesInUse, ref ModelData.GameObjectsInUse);
            }

        }

        /// <summary>
        /// Saves GO connected to feature
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="GameObjectList"></param>
        internal void SaveGameObjectFromFeature(XNode.Node feature, ref List<string> GameObjectList)
        {
            if (feature != null)
            {
                // Make sure list is init
                if (GameObjectList == null)
                    GameObjectList = new List<string>();

                GameObject inputGO = null;
                string inputGOName = ""; // Only used when we have many gameObjects connected to one feature
                // What type of feature is this?
                // Position?
                if (feature is GameObjectMovementFeatures.PositionNode)
                {
                    inputGO = (feature as GameObjectMovementFeatures.PositionNode).GameObjectDataIn;
                }
                // Rotation?
                else if (feature is GameObjectMovementFeatures.RotationEulerNode)
                {
                    inputGO = (feature as GameObjectMovementFeatures.RotationEulerNode).GameObjectDataIn;
                }
                // Rotation Quaternion?
                else if (feature is GameObjectMovementFeatures.RotationQuaternionNode)
                {
                    inputGO = (feature as GameObjectMovementFeatures.RotationQuaternionNode).GameObjectDataIn;

                }
                // Distance to First Input?
                else if (feature is GameObjectMovementFeatures.DistanceToFirstInputNode)
                {
                    var distanceNode = feature as GameObjectMovementFeatures.DistanceToFirstInputNode;
                    if (distanceNode.FirstInput != null)
                    {
                        String.Concat(inputGOName, "_", distanceNode.FirstInput.name);
                    }
                    if (distanceNode.SecondInputs != null)
                    {
                        foreach (var secondInput in distanceNode.SecondInputs)
                        {
                            String.Concat(inputGOName, "_", secondInput.name);
                        }
                    }

                }
                // Velocity?
                else if (feature is GameObjectMovementFeatures.VelocityNode)
                {
                    var velocityFeature = feature as GameObjectMovementFeatures.VelocityNode;
                    if (velocityFeature.FeatureToInput != null)
                    {
                        // Which feature are we calculating the velocity from?
                        // Position?
                        if (velocityFeature.FeatureToInput is GameObjectMovementFeatures.PositionNode)
                        {
                            inputGO = (velocityFeature.FeatureToInput as GameObjectMovementFeatures.PositionNode).GameObjectDataIn;
                        }
                        // Rotation Euler?
                        else if (velocityFeature.FeatureToInput is GameObjectMovementFeatures.RotationEulerNode)
                        {
                            inputGO = (velocityFeature.FeatureToInput as GameObjectMovementFeatures.RotationEulerNode).GameObjectDataIn;
                        }
                        // Rotation Quaternion?
                        else if (velocityFeature.FeatureToInput is GameObjectMovementFeatures.RotationQuaternionNode)
                        {
                            inputGO = (velocityFeature.FeatureToInput as GameObjectMovementFeatures.RotationQuaternionNode).GameObjectDataIn;
                        }
                        // Distance to first input?
                        else if (velocityFeature.FeatureToInput is GameObjectMovementFeatures.DistanceToFirstInputNode)
                        {
                            var distanceNode = velocityFeature.FeatureToInput as GameObjectMovementFeatures.DistanceToFirstInputNode;
                            if (distanceNode.FirstInput != null)
                            {
                                String.Concat(inputGOName, "_", distanceNode.FirstInput.name);
                            }
                            if (distanceNode.SecondInputs != null)
                            {
                                foreach (var secondInput in distanceNode.SecondInputs)
                                {
                                    String.Concat(inputGOName, "_", secondInput.name);
                                }
                            }
                        }
                        // Acceleration?
                        else if (velocityFeature.FeatureToInput is GameObjectMovementFeatures.VelocityNode)
                        {
                            var accelerationFeature = velocityFeature.FeatureToInput as GameObjectMovementFeatures.VelocityNode;
                            if (accelerationFeature.FeatureToInput != null)
                            {
                                // Which feature are we calculating the ACCELERATION from?
                                // Position?
                                if (accelerationFeature.FeatureToInput is GameObjectMovementFeatures.PositionNode)
                                {
                                    inputGO = (accelerationFeature.FeatureToInput as GameObjectMovementFeatures.PositionNode).GameObjectDataIn;
                                }
                                // Rotation Euler?
                                else if (accelerationFeature.FeatureToInput is GameObjectMovementFeatures.RotationEulerNode)
                                {
                                    inputGO = (accelerationFeature.FeatureToInput as GameObjectMovementFeatures.RotationEulerNode).GameObjectDataIn;
                                }
                                // Rotation Quaternion?
                                else if (accelerationFeature.FeatureToInput is GameObjectMovementFeatures.RotationQuaternionNode)
                                {
                                    inputGO = (accelerationFeature.FeatureToInput as GameObjectMovementFeatures.RotationQuaternionNode).GameObjectDataIn;
                                }
                            }
                        }

                    }
                }

                // is there a gameobject connected?
                if (inputGO != null) GameObjectList.Add(inputGO.name);
                else if (!string.IsNullOrEmpty(inputGOName)) GameObjectList.Add(inputGOName);

            }
        }

        /// <summary>
        /// Saves IML features (and GOs connected to features) present in the trainingNode
        /// </summary>
        /// <param name="trainingExamplesNode"></param>
        internal void SaveTrainingFeatures(TrainingExamplesNode trainingExamplesNode)
        {
            // Make sure list is init
            if (ModelData.TrainingFeatures == null)
                ModelData.TrainingFeatures = new List<string>();

            // Get Feature from training examples
            SaveFeatures(trainingExamplesNode.InputFeatures, ref ModelData.TrainingFeatures, ref ModelData.TrainingGameObjects);
        }

        /// <summary>
        /// Saves feature names and GOs
        /// </summary>
        /// <param name="features"></param>
        /// <param name="listToSaveFeatures"></param>
        /// <param name="listToSaveGOs"></param>
        internal void SaveFeatures(List<XNode.Node> features, ref List<string> listToSaveFeatures, ref List<string> listToSaveGOs)
        {
            if (features != null)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    var feature = features[i];
                    if (feature != null) listToSaveFeatures.Add(feature.name);

                    // Is this a velocity?
                    if (feature is GameObjectMovementFeatures.VelocityNode)
                    {
                        var velocityFeature = feature as GameObjectMovementFeatures.VelocityNode;
                        if (velocityFeature.FeatureToInput != null)
                        {
                            // What is connected to the velocity?
                            // Acceleration?
                            if (velocityFeature.FeatureToInput is GameObjectMovementFeatures.VelocityNode)
                            {
                                var accelerationFeature = velocityFeature.FeatureToInput as GameObjectMovementFeatures.VelocityNode;
                                // Add the acceleration and the feature connected to it
                                listToSaveFeatures.Add(accelerationFeature.name);
                                if (accelerationFeature.FeatureToInput != null)
                                {
                                    feature = accelerationFeature.FeatureToInput as XNode.Node;
                                    listToSaveFeatures.Add(feature.name);
                                }
                            }
                            // Any other feature connected...
                            else
                            {
                                feature = velocityFeature.FeatureToInput as XNode.Node;
                                listToSaveFeatures.Add(feature.name);
                            }

                        }
                    }
                    // Once we got the feature, add GameObjectFromFeature
                    SaveGameObjectFromFeature(feature, ref listToSaveGOs);
                }
            }

        }

        /// <summary>
        /// Saves training dataset (and training Features and training GOs connected to training node)
        /// </summary>
        /// <param name="modelNode"></param>
        internal void SaveTrainingData(MLSystem modelNode)
        {
            if (modelNode != null)
            {
                // Go through all the IML Training Examples if we can
                if (!Lists.IsNullOrEmpty(ref modelNode.IMLTrainingExamplesNodes))
                {
                    // Make sure list is init
                    if (ModelData.TrainingData == null) ModelData.TrainingData = new List<IMLTrainingExample>();

                    // Go through each node
                    for (int i = 0; i < modelNode.IMLTrainingExamplesNodes.Count; i++)
                    {
                        // If there are training examples in this node...
                        if (modelNode.IMLTrainingExamplesNodes[i].TrainingExamplesVector.Count > 0)
                        {
                            // Go through all the training examples
                            for (int j = 0; j < modelNode.IMLTrainingExamplesNodes[i].TrainingExamplesVector.Count; j++)
                            {
                                // Check that individual example is not null
                                var IMLTrainingExample = modelNode.IMLTrainingExamplesNodes[i].TrainingExamplesVector[j];
                                if (IMLTrainingExample != null)
                                {
                                    // Check that inputs/outputs are not null
                                    if (IMLTrainingExample.Inputs != null && IMLTrainingExample.Outputs != null)
                                    {
                                        // Add a new example to list
                                        var trainingExample = new IMLTrainingExample();
                                        trainingExample.SetInputs(IMLTrainingExample.Inputs);
                                        trainingExample.SetOutputs(IMLTrainingExample.Outputs);
                                        ModelData.TrainingData.Add(trainingExample);
                                    }
                                    // If there are null outputs we debug an error
                                    else
                                    {
                                        Debug.LogError("Null inputs/outputs found when training IML model. Training aborted!");
                                    }
                                }
                            }
                        }

                        // Get Features connected to this trainingData node
                        SaveTrainingFeatures(modelNode.IMLTrainingExamplesNodes[i]);
                    }
                }
            }
        }

    }
}