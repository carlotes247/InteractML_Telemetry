using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractML.Telemetry;
using ReusableMethods;
using InteractML;
using System.Linq;

namespace InteractML.Telemetry
{
    public class TelemetryReader : MonoBehaviour
    {
        #region Variables
        
        List<IterationAccuracy> m_AccuraciesPerIteration;
        EasyRapidlib m_EasyRapidlibModel;

        #endregion

        /// <summary>
        /// Calculate the average accuracy from all iterations in a telemetryFile
        /// </summary>
        /// <param name="telemetryFile"></param>
        /// <returns></returns>
        private float CalculateAvgAccuracy(TelemetryData telemetryFile, ref List<IterationAccuracy> accuracyIterations, ref EasyRapidlib easyRapidlibModel)
        {
            if (accuracyIterations == null)
                accuracyIterations = new List<IterationAccuracy>();
            else
                accuracyIterations.Clear();
            List<float> accuracyIterationsFloat = new List<float>();
            float averageAccuracy = 0f;
            // If all is good with telemetry file
            if (telemetryFile != null && !Lists.IsNullOrEmpty(ref telemetryFile.IMLIterations))
            {
                // Iterate through IML iterations and 'construct' a complete iteration
                foreach (var IMLIteration in telemetryFile.IMLIterations)
                {
                    List<IMLTrainingExample> uniqueClassesList = new List<IMLTrainingExample>();
                    int numUniqueClasses = 0;
                    // if we got both training data and testing data, calculate accuracy for this entry
                    if (!Lists.IsNullOrEmpty(ref IMLIteration.ModelData.TrainingData) && !Lists.IsNullOrEmpty(ref IMLIteration.ModelData.TestingData))
                    {
                        // create kNN model if needed
                        if (easyRapidlibModel == null)
                            easyRapidlibModel = new EasyRapidlib(EasyRapidlib.LearningType.Classification);
                        // clear training data set
                        easyRapidlibModel.ClearTrainingExamples();
                        // add training data to model
                        easyRapidlibModel.AddTrainingDataSet(IMLIteration.ModelData.TrainingData);
                        // train kNN model
                        easyRapidlibModel.TrainModel();
                        // get list of unique classes in training set
                        TrainingExamplesNode.SetUniqueClassesFromDataVector(ref uniqueClassesList, ref numUniqueClasses, IMLIteration.ModelData.TrainingData);
                        // testing entries in list should match uniqueClasses entries
                        if (uniqueClassesList.Count == IMLIteration.ModelData.TestingData.Count)
                        {
                            // go through each testing entry per class. 
                            foreach (var testingEntry in IMLIteration.ModelData.TestingData)
                            {
                                int numHits = 0;
                                int numMisses = 0;
                                foreach (var testingExample in testingEntry)
                                {
                                    var expectedOutput = testingExample.GetOutputs();
                                    double[] output = easyRapidlibModel.Run(testingExample.GetInputs(), testingExample.GetOutputs().Length);
                                    if (Enumerable.SequenceEqual(output, expectedOutput))
                                    {
                                        // hit
                                        numHits++;
                                    }
                                    else
                                    {
                                        // miss
                                        numMisses++;
                                    }
                                }
                                // Calculate accuracy and add to list of accuracies
                                // float
                                float accuracyIteration = numHits / (numHits + numMisses);
                                accuracyIterationsFloat.Add(accuracyIteration);
                                // struct with extra info
                                IterationAccuracy iterationAccuracyStruct;
                                iterationAccuracyStruct.Accuracy = accuracyIteration;
                                iterationAccuracyStruct.TimeStamp = IMLIteration.EndTimeUTC;
                                iterationAccuracyStruct.IterationID = IMLIteration.ModelData.ModelID;
                                accuracyIterations.Add(iterationAccuracyStruct);

                                Debug.Log($"Accuracy was {accuracyIteration} at {iterationAccuracyStruct.TimeStamp}");

                            }



                            averageAccuracy = accuracyIterationsFloat.Average();
                        }
                    }
                }
            }

            Debug.Log($"Average accuracy is {averageAccuracy}");

            return averageAccuracy;
        }
    }

}
