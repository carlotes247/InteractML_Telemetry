﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractML.Telemetry
{ 
    [System.Serializable]
    public class ParticipantAccuracyData
    {
        /// <summary>
        /// Anonymised ID of participant
        /// </summary>
        public string ParticipantID;
        /// <summary>
        /// List of model IDs
        /// </summary>
        public List<string> ModelIDs;
        public List<ModelAccuracyHistory> ModelsAccuracyHistories;

        public ParticipantAccuracyData()
        {
            InitData();
        }

        /// <summary>
        /// Inits internal data
        /// </summary>
        public void InitData()
        {
            if (ModelIDs == null)
                ModelIDs = new List<string>();
            if (ParticipantID == null)
                ParticipantID = "";
            if (ModelsAccuracyHistories == null)
                ModelsAccuracyHistories = new List<ModelAccuracyHistory>();
        }

        /// <summary>
        /// Adds new iteration accuracy entry to history
        /// </summary>
        /// <param name="modelID"></param>
        /// <param name="graphID"></param>
        /// <param name="sceneName"></param>
        /// <param name="newAccuracyData"></param>
        public void AddIterationAccuracyData(string modelID, string graphID, string sceneName, IterationAccuracy newAccuracyData)
        {
            if (!string.IsNullOrEmpty(modelID))
            {
                // new model accuracy history
                if (!ModelIDs.Contains(modelID))
                {
                    ModelIDs.Add(modelID);
                    // add new entry to model histories
                    ModelAccuracyHistory newHistory;
                    newHistory.ModelID = modelID;
                    newHistory.GraphID = graphID;
                    newHistory.SceneName = sceneName;
                    newHistory.AccuracyOverTime = new List<IterationAccuracy>();
                    newHistory.AccuracyOverTime.Add(newAccuracyData);
                    ModelsAccuracyHistories.Add(newHistory);
                }
                // update existing history
                else
                {
                    // retrieve history
                    var history = ModelsAccuracyHistories.Where(x => x.ModelID.Equals(modelID)).First();
                    history.AccuracyOverTime.Add(newAccuracyData);
                }
            }

        }

        /// <summary>
        /// Adds new iteration accuracy entry to history
        /// </summary>
        /// <param name="modelID"></param>
        /// <param name="graphID"></param>
        /// <param name="sceneName"></param>
        /// <param name="newAccuracy"></param>
        /// <param name="newTimestamp"></param>
        public void AddIterationAccuracyData(string modelID, string graphID, string sceneName, float newAccuracy, System.DateTime newTimestamp)
        {
            if (!string.IsNullOrEmpty(modelID))
            {
                IterationAccuracy newIterationAccuracy;
                newIterationAccuracy.Accuracy = newAccuracy;
                newIterationAccuracy.TimeStamp = newTimestamp;
                AddIterationAccuracyData(modelID, graphID, sceneName, newIterationAccuracy);
            }
        }

        public void SortHistoryByTime()
        {
            if (ModelsAccuracyHistories != null)
            {
                foreach (var modelHistory in ModelsAccuracyHistories)
                {
                    var orderedList = modelHistory.AccuracyOverTime.OrderBy(x => x.TimeStamp.TimeOfDay).ToList();
                    var sortedDates = modelHistory.AccuracyOverTime.OrderByDescending(x => x.TimeStamp);
                   
                }
            }
        }

        /// <summary>
        /// Clears all history from all models, but retains user ID
        /// </summary>
        public void ClearAllHistory()
        {
            ModelIDs.Clear();
            ModelsAccuracyHistories.Clear();
        }

        #region Load/Save

        public Task SaveToJSONAsync(string path, string fileName)
        {
            return Task.Run(async () =>
            {
                await IMLDataSerialization.SaveObjectToDiskAsync(this, path, fileName);
            });
        }

        public Task LoadFromJSONAsync(string path, string fileName)
        {
            return Task.Run(async () =>
            {
                var fileLoaded = await IMLDataSerialization.LoadObjectFromDiskAsync<ParticipantAccuracyData>(path, fileName);

                this.ParticipantID = fileLoaded.ParticipantID;
                this.ModelIDs = fileLoaded.ModelIDs;
                this.ModelsAccuracyHistories = fileLoaded.ModelsAccuracyHistories;
            });
        }

        #endregion
    }
}