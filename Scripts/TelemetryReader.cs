using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractML.Telemetry;
using ReusableMethods;
using InteractML;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InteractML.Telemetry
{
    public class TelemetryReader : MonoBehaviour
    {
        #region Variables

        List<IterationAccuracy> m_AccuraciesPerIteration;
        EasyRapidlib m_EasyRapidlibModel;

        public List<TelemetryData> TelemetryFiles;

        /// <summary>
        /// Path of folder to search in
        /// </summary>
        public string FolderPath;

        // Flags for loading
        public bool LoadingStarted { get { return m_LoadingStarted; } }
        [System.NonSerialized]
        private bool m_LoadingStarted;
        public bool LoadingFinished { get { return m_LoadingFinished; } }
        [System.NonSerialized]
        private bool m_LoadingFinished;

        public bool UseAsync;
        public int TotalFilesNum { get => m_TotalFilesNum; }
        private int m_TotalFilesNum;
        public int FilesLoadedNum { get => m_FilesLoadedNum; }
        private int m_FilesLoadedNum;



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

        /// <summary>
        /// Loads all telemetry files from a path asynchronously
        /// </summary>
        /// <param name="path"></param>
        /// <param name="specificID"></param>
        /// <param name="absolutePath"></param>
        public void LoadAllTelemetryFilesFromPath(string path, string specificID = "", bool absolutePath = false, bool useAsync = true)
        {
            if (m_LoadingStarted)
            {
                Debug.LogError("Can't start loading when there is a loading in progress...");
                return;
            }

            // is this a relative path?
            if (!absolutePath)
            {
                var assetsPath = IMLDataSerialization.GetAssetsPath();
                path = Path.Combine(assetsPath, path);
            }

            if (Directory.Exists(path))
            {
                m_LoadingStarted = true;
                m_LoadingFinished = false;

                // Async
                if (useAsync)
                {
                    var task = Task.Run(async () => { 
                        var result = await LoadTelemetryFilesAsync(path, specificID);
                        return result;
                    }) ;
                    // Starts coroutine that will dump all loaded values into internal list
                    StartCoroutine(LoadTelemetryFilesAsyncCoroutine(task));

                }
                // No Async
                else
                {
                    StartCoroutine(LoadTelemetryFilesCoroutine(path, specificID));
                }


            }
            else
            {
                Debug.LogError($"The folder {path} doesn't exist!");
            }
        }

        private async Task<List<TelemetryDataJSONAsync>> LoadTelemetryFilesAsync(string path, string specificID)
        {
            var result = await Task.Run(async () =>
            {
                //// Clear previously loaded list
                //TelemetryFiles.Clear();

                List<TelemetryDataJSONAsync> telemetryFiles = new List<TelemetryDataJSONAsync>();


                // First, find all the folders
                // Iterate to upload all files in folder, including subdirectories
                string[] files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
                m_TotalFilesNum = files.Length;
                m_FilesLoadedNum = 0;
                Debug.Log($"{files.Length} files found. Loading data sets, please wait...");
                foreach (string file in files)
                {
                    // If there is a json file, attempt to load
                    if (Path.GetExtension(file) == ".json")
                    {
                        // Are we looking for a specific ID?
                        if (!string.IsNullOrEmpty(specificID))
                        {
                            // skip if the file doesn't contain the ID we want
                            if (!file.Contains(specificID))
                                continue;
                        }

                        // Load training data set
                        //TelemetryDataJSONAsync telemetryFile = new TelemetryDataJSONAsync();
                        var telemetryFile = await IMLDataSerialization.LoadObjectFromDiskAsync<TelemetryDataJSONAsync>(file);

                        // Add to list if not null
                        if (telemetryFile != null)
                        {
                            telemetryFiles.Add(telemetryFile);
                            m_FilesLoadedNum++;
                        }
                    }
                }

                if (telemetryFiles.Count == 0)
                {
                    Debug.Log("Couldn't load folder!");
                    m_LoadingStarted = false; // allow to re-load if user wants to
                }
                else
                {
                    m_LoadingFinished = true;
                    m_LoadingStarted = false; // allow to re-load if user wants to
                    Debug.Log($"{telemetryFiles.Count} Telemetry Files Loaded!");
                    return telemetryFiles;
                }
                return null;

            });

            return result;
        }

        /// <summary>
        /// Awaits until the loading task is finished to dump contents into internal list
        /// </summary>
        /// <param name="taskLoading"></param>
        /// <returns></returns>
        private IEnumerator LoadTelemetryFilesAsyncCoroutine(Task<List<TelemetryDataJSONAsync>> taskLoading)
        {
            while (!taskLoading.IsCompleted)
            {
                // wait a frame
                yield return null;
            }
            var loadedFiles = taskLoading.Result;
            TelemetryFiles.Clear();
            foreach (var file in loadedFiles)
            {
                var data = ScriptableObject.CreateInstance<TelemetryData>();
                data.MigrateFrom(file);
                TelemetryFiles.Add(data);
            }

            Debug.Log("Files loaded!");
            yield break;
        }

        private IEnumerator LoadTelemetryFilesCoroutine(string path, string specificID = "")
        {
            yield return null;

            // First, find all the folders
            // Iterate to upload all files in folder, including subdirectories
            string[] files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
            m_TotalFilesNum = files.Length + 1;
            m_FilesLoadedNum = 0;
            Debug.Log($"{files.Length} files found. Loading data sets, please wait...");
            yield return null;

            foreach (string file in files)
            {
                // Are we looking for a specific ID?
                if (!string.IsNullOrEmpty(specificID))
                {
                    // skip if the file doesn't contain the ID we want
                    if (!file.Contains(specificID))
                        continue;
                }

                Debug.Log($"Loading file {file}");
                // Load training data set
                //TelemetryData telemetryFile = new TelemetryData();
                var telemetryFile = IMLDataSerialization.LoadObjectFromDisk<TelemetryData>(file);

                // Add to list if not null
                if (telemetryFile != null)
                {
                    //telemetryFiles.Add(telemetryFile);
                    TelemetryFiles.Add(telemetryFile);
                    Debug.Log($"File loaded!");
                    m_FilesLoadedNum++;
                }

                yield return null;
            }

            if (TelemetryFiles.Count == 0)
            {
                Debug.Log("Couldn't load folder!");
                m_LoadingStarted = false; // allow to re-load if user wants to
            }
            else
            {
                m_LoadingFinished = true;
                m_LoadingStarted = false; // allow to re-load if user wants to
                Debug.Log($"Finished Loading {TelemetryFiles.Count} Telemetry Files!");
            }

        }

    }


}
