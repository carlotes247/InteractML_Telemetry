using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Holds all questionnaire answers per participant
    /// </summary>
    [System.Serializable]
    public class ParticipantQuestionnaireData 
    {
        public string ParticipantID;
        public List<QuestionnaireAnswer> QuestionnaireAnswers;

        ParticipantQuestionnaireData()
        {
            ParticipantID = "";
            QuestionnaireAnswers = new List<QuestionnaireAnswer>();
        }

        public Task LoadAnswersFromJSONAsync(string path, string fileName)
        {
            var task = Task.Run(async () => 
            { 
                var result = await IMLDataSerialization.LoadObjectFromDiskAsync<ParticipantQuestionnaireData>(path, fileName);
                if (result != null)
                {
                    this.ParticipantID = result.ParticipantID;
                    this.QuestionnaireAnswers = result.QuestionnaireAnswers;
                }
            });
            return task;
        }
    }
}