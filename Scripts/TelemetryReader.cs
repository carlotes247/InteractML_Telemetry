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
        public bool CoroutineAsyncRunning { get => m_CoroutineAsyncRunning; }
        [System.NonSerialized]
        private bool m_CoroutineAsyncRunning;

        public bool UseAsync;
        public int TotalFilesNum { get => m_TotalFilesNum; }
        private int m_TotalFilesNum;
        public int FilesLoadedNum { get => m_FilesLoadedNum; }
        private int m_FilesLoadedNum;
        public int FilesProcessedCoroutineAsyncNum { get => m_FilesProcessedCoroutineAsyncNum; }
        private int m_FilesProcessedCoroutineAsyncNum;

        #endregion

        #region Loading Files

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
                m_CoroutineAsyncRunning = false;
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
            TelemetryFiles.Clear();
            while (!taskLoading.IsCompleted)
            {
                // wait a frame
                yield return null;
            }
            m_LoadingFinished = false;
            m_LoadingStarted = true; // keep the loading flags on while the coroutine finishes
            m_CoroutineAsyncRunning = true;

            m_FilesProcessedCoroutineAsyncNum = 0;
            var loadedFiles = taskLoading.Result;
            foreach (var file in loadedFiles)
            {
                var data = ScriptableObject.CreateInstance<TelemetryData>();
                data.MigrateFrom(file);
                TelemetryFiles.Add(data);
                m_FilesProcessedCoroutineAsyncNum++;
                yield return null;
            }

            Debug.Log("Files loaded!");

            m_LoadingFinished = true;
            m_LoadingStarted = false; // allow to re-load if user wants to
            m_CoroutineAsyncRunning = false;

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

        #endregion


    }


}
