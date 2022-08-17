using ReusableMethods;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Calculates accuracy from telemetry files
    /// </summary>
    public class AccuracyCalculator : MonoBehaviour
    {
        #region Variables

        public TelemetryReader TelemetryFileReader { get => m_TelemetryFileReader; }
        [SerializeField]
        private TelemetryReader m_TelemetryFileReader;


        /// <summary>
        /// File with information about participant accuracy
        /// </summary>
        public ParticipantAccuracyDataSO ParticipantAccuracyFile;
        public int WhichFileToProcess { get => m_WhichFileToProcess; set => m_WhichFileToProcess = value; }
        private int m_WhichFileToProcess;
        private List<IterationAccuracy> m_AccuraciesPerIteration;
        
        private EasyRapidlib m_EasyRapidlibModel;

        #endregion


        #region Accuracy calculations

        /// <summary>
        /// Calculate the average accuracy from all iterations in a telemetryFile
        /// </summary>
        /// <param name="telemetryFile"></param>
        /// <returns></returns>
        private bool CalculateAccuracyPerIteration(TelemetryData telemetryFile, ref ParticipantAccuracyData participantAccuracyData, ref EasyRapidlib easyRapidlibModel)
        {
            List<float> accuracyIterationsFloat = new List<float>();
            float averageAccuracy = 0f;
            // If all is good with telemetry file
            if (telemetryFile != null && !Lists.IsNullOrEmpty(ref telemetryFile.IMLIterations))
            {
                // If there isn't a single iteration with training and testing data abort!
                if (!telemetryFile.IMLIterations.Where(x => x.ModelData.TrainingData != null && x.ModelData.TestingData != null).Any())
                {
                    Debug.LogError("Training and testing data are always null in all iterations of selected file!");
                    return false;
                }

                // how many models are included in this file
                List<string> modelIDs = new List<string>();


                // Iterate through IML iterations and 'construct' a complete iteration separated by models
                foreach (var IMLIteration in telemetryFile.IMLIterations)
                {
                    // how many classes are in this iteration
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
                            int numHits = 0;
                            int numMisses = 0;

                            // go through each testing entry per class. 
                            foreach (var testingEntry in IMLIteration.ModelData.TestingData)
                            {
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

                            }

                            // Calculate accuracy and add to list of accuracies
                            // float
                            float totalSamples = numHits + numMisses;
                            float accuracyIteration = numHits / totalSamples;
                            accuracyIterationsFloat.Add(accuracyIteration);
                            // add to participant history
                            participantAccuracyData.AddIterationAccuracyData(IMLIteration.ModelData.ModelID, IMLIteration.GraphID, IMLIteration.SceneName, accuracyIteration, IMLIteration.EndTimeUTC);

                            Debug.Log($"Accuracy was {accuracyIteration} at {IMLIteration.EndTimeUTC}");



                            averageAccuracy = accuracyIterationsFloat.Average();
                        }
                    }
                }
            }

            Debug.Log($"Average accuracy of file {telemetryFile.name} is {averageAccuracy}");

            //return averageAccuracy;
            return true;
        }

        /// <summary>
        /// Calculates the accuracy of one file
        /// </summary>
        /// <param name="whichFile"></param>
        public void CalculateAccuracyOfTelemetryFile(int whichFile, List<TelemetryData> telemetryFiles)
        {
            // if not null, empty and with enough files
            if (telemetryFiles != null && telemetryFiles.Count > 0 && telemetryFiles.Count >= whichFile + 1)
            {
                var file = telemetryFiles[whichFile];
                if (file != null)
                {
                    CalculateAccuracyPerIteration(file, ref ParticipantAccuracyFile.AccuracyData, ref m_EasyRapidlibModel);
                }
            }
        }

        public void CalculateAccuracyOfAllTelemetryFiles(List<TelemetryData> telemetryFiles, bool clearPreviousData = false)
        {
            // if not null and empty 
            if (telemetryFiles != null && telemetryFiles.Count > 0)
            {
                if (clearPreviousData)
                    ParticipantAccuracyFile.AccuracyData.ClearAllHistory();

                foreach (var file in telemetryFiles)
                {
                    if (file != null)
                    {
                        CalculateAccuracyPerIteration(file, ref ParticipantAccuracyFile.AccuracyData, ref m_EasyRapidlibModel);
                    }
                }
            }

        }

        #endregion

    }
}