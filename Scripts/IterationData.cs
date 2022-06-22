﻿using ReusableMethods;
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

        // GameObjects per iteration
        private List<GameObject> m_GOsTrainingFeatures;
        private List<GameObject> m_GOsTestingFeatures;
        private bool m_AreTrainingGOsPopulated;
        private bool m_AreTestingGOsPopulated;
        /// <summary>
        /// Keeps track of all possible velocity+acceleration extractors per GameObject
        /// </summary>
        private Dictionary<GameObject, Extractors.AllExtractors> m_TrainingVelocityExtractorsPerGO;
        /// <summary>
        /// Keeps track of all possible velocity+acceleration extractors per GameObject
        /// </summary>
        private Dictionary<GameObject, Extractors.AllExtractors> m_TestingVelocityExtractorsPerGO;

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
        /// (Recursive) Returns all GOs connected to a feature
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        internal List<GameObject> GetGameObjectsFromFeature(XNode.Node feature)
        {
            List<GameObject> GOsToReturn = new List<GameObject>();
            if (feature != null)
            {
                GameObject inputGO = null;
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
                        var auxGOs = GetGameObjectsFromFeature(distanceNode.FirstInput);
                        if (auxGOs != null && auxGOs.Count > 0)
                        {
                            foreach (var go in auxGOs)
                            {
                                if (go != null) GOsToReturn.Add(inputGO);
                            }
                        }                        
                    }
                    if (distanceNode.SecondInputs != null)
                    {
                        foreach (var secondInput in distanceNode.SecondInputs)
                        {
                            var auxGOs = GetGameObjectsFromFeature(secondInput);
                            foreach (var go in auxGOs)
                            {
                                if (go != null) GOsToReturn.Add(inputGO);
                            }
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
                                var auxGOs = GetGameObjectsFromFeature(distanceNode.FirstInput);
                                foreach (var go in auxGOs)
                                {
                                    if (go != null) GOsToReturn.Add(go);
                                }
                                
                            }
                            if (distanceNode.SecondInputs != null)
                            {
                                foreach (var secondInput in distanceNode.SecondInputs)
                                {
                                    var auxGOs = GetGameObjectsFromFeature(secondInput);
                                    foreach (var go in auxGOs)
                                    {
                                        if (go != null) GOsToReturn.Add(go);
                                    }
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

                // if there was one inputGo, add it to the list
                if (inputGO != null) GOsToReturn.Add(inputGO);
            }
            return GOsToReturn;
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

        /// <summary>
        /// Saves testing dataset (and testing features and testing GOs connected to model node)
        /// </summary>
        /// <param name="modelNode"></param>
        internal void SaveTestingData(MLSystem modelNode)
        {
            if (modelNode != null)
            {
                // Go through all the testing data if we can
                if (modelNode.TestingData != null && modelNode.TestingData.Count > 0)
                {
                    // Make sure list is init
                    if (ModelData.TestingData == null) ModelData.TestingData = new List<List<IMLTrainingExample>>();

                    // Go through each unique class
                    for (int i = 0; i < modelNode.TestingData.Count; i++)
                    {
                        var subListModel = modelNode.TestingData[i];
                        List<IMLTrainingExample> subListTelemetry = null;
                        // Get or create reference for telemetry copy of testing data
                        if (i < ModelData.TestingData.Count) 
                            subListTelemetry = ModelData.TestingData[i];

                        if (subListTelemetry == null)
                        {
                            subListTelemetry = new List<IMLTrainingExample>();
                            ModelData.TestingData.Add(subListTelemetry);
                        }

                        // If there are testing examples in this node...
                        if (subListModel != null && subListModel.Count > 0)
                        {
                            // Go through all the testing examples
                            for (int j = 0; j < subListModel.Count; j++)
                            {
                                // Check that individual example is not null
                                var testingExampleModel = subListModel[j];
                                if (testingExampleModel != null)
                                {
                                    // Check that inputs/outputs are not null
                                    if (testingExampleModel.Inputs != null && testingExampleModel.Outputs != null)
                                    {
                                        // Add a new example to list
                                        var testingExampleTelemetry = new IMLTrainingExample();
                                        testingExampleTelemetry.SetInputs(testingExampleModel.Inputs);
                                        testingExampleTelemetry.SetOutputs(testingExampleModel.Outputs);
                                        subListTelemetry.Add(testingExampleTelemetry);
                                    }
                                    // If there are null outputs we debug an error
                                    else
                                    {
                                        Debug.LogError($"Null inputs/outputs found when saving testing dataset for IML model {modelNode.id}");
                                    }
                                }
                            }
                        }

                    }
                }
            }

        }

        #region Methods to collect all possible features

        /// <summary>
        /// True if all the training GameObjects have been extracted
        /// </summary>
        /// <returns></returns>
        internal bool AllTrainingGameObjectsExtracted()
        {
            return m_AreTrainingGOsPopulated;
        }

        /// <summary>
        /// Gets GameObjects connected to any feature in node
        /// </summary>
        private List<GameObject> GetTrainingGameObjects()
        {
            if (m_GOsTrainingFeatures == null) m_GOsTrainingFeatures = new List<GameObject>();

            return m_GOsTrainingFeatures;
        }

        /// <summary>
        /// Populates an internal list of GameObjects connected to any feature in training node
        /// </summary>
        /// <param name="trainingExamplesNode"></param>
        private void PopulateTrainingGameObjects(TrainingExamplesNode trainingExamplesNode)
        {
            if (m_GOsTrainingFeatures == null) m_GOsTrainingFeatures = new List<GameObject>();            

            // Get all GOs from features
            foreach (var feature in trainingExamplesNode.InputFeatures)
            {
                var allGOs = GetGameObjectsFromFeature(feature);

                // Make sure we aren't listing duplicates
                foreach (var gameobject in allGOs)
                {
                    if (gameobject != null && !m_GOsTrainingFeatures.Contains(gameobject))
                        m_GOsTrainingFeatures.Add(gameobject);
                }
            }

            // Populate feature extractors
            PopulateExtractors(m_GOsTrainingFeatures, ref m_TrainingVelocityExtractorsPerGO);

            m_AreTrainingGOsPopulated = true;
        }

        /// <summary>
        /// Populates and return an internal list of GameObjects connected to any feature in training node
        /// </summary>
        /// <param name="trainingExamplesNode"></param>
        internal List<GameObject> TryGetTrainingGameObjects(TrainingExamplesNode trainingExamplesNode)
        {
            if (!AllTrainingGameObjectsExtracted())
                PopulateTrainingGameObjects(trainingExamplesNode);

            return GetTrainingGameObjects();
        }

        internal List<GameObject> TryGetTestingGameObjects(MLSystem modelNode)
        {
            if (!AllTestingGameObjectsExtracted())
                PopulateTestingGameObjects(modelNode);

            return GetTestingGameObjects();

        }

        /// <summary>
        /// True if all the testing GameObjects have been extracted
        /// </summary>
        /// <returns></returns>
        internal bool AllTestingGameObjectsExtracted()
        {
            return m_AreTestingGOsPopulated;
        }

        /// <summary>
        /// Gets GameObjects connected to any feature in node
        /// </summary>
        private List<GameObject> GetTestingGameObjects()
        {
            if (m_GOsTestingFeatures == null) m_GOsTestingFeatures = new List<GameObject>();

            return m_GOsTestingFeatures;
        }

        /// <summary>
        /// Populates an internal list of GameObjects connected to any feature in testing node
        /// </summary>
        /// <param name="modelNode"></param>
        private void PopulateTestingGameObjects(MLSystem modelNode)
        {
            if (m_GOsTestingFeatures == null) m_GOsTestingFeatures = new List<GameObject>();

            // Get all GOs from features
            foreach (var feature in modelNode.InputFeatures)
            {
                var allGOs = GetGameObjectsFromFeature(feature);

                // Make sure we aren't listing duplicates
                foreach (var gameobject in allGOs)
                {
                    if (gameobject != null && !m_GOsTestingFeatures.Contains(gameobject))
                        m_GOsTestingFeatures.Add(gameobject);
                }
            }

            // Populate feature extractors
            PopulateExtractors(m_GOsTestingFeatures, ref m_TestingVelocityExtractorsPerGO);

            m_AreTestingGOsPopulated = true;
        }


        /// <summary>
        /// Populates velocity extractors in private dictionary
        /// </summary>
        /// <param name="gos"></param>
        /// <param name="dictionary"></param>
        private void PopulateExtractors(List<GameObject> gos, ref Dictionary<GameObject, Extractors.AllExtractors> dictionary)
        {
            if (gos != null)
            {
                if (dictionary == null) dictionary = new Dictionary<GameObject, Extractors.AllExtractors>();
                // iterate gameobjects
                foreach (var go in gos)
                {
                    if (!dictionary.ContainsKey(go))
                    {
                        // create new velocity extractor and add it to dict
                        var extractors = new Extractors.AllExtractors(go);
                        dictionary.Add(go, extractors);
                    }

                }
            }
        }

        /// <summary>
        /// Velocity Extractor per GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        internal Extractors.AllExtractors TryGetTrainingExtractors(GameObject go)
        {
            if (!AllTrainingGameObjectsExtracted() || go == null)
                return null;

            return m_TrainingVelocityExtractorsPerGO[go];
        }

        /// <summary>
        /// Velocity Extractor per GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        internal Extractors.AllExtractors TryGetTestingExtractors(GameObject go)
        {
            if (!AllTestingGameObjectsExtracted() || go == null)
                return null;

            return m_TestingVelocityExtractorsPerGO[go];
        }

        #endregion
    }
}