using System;

/// <summary>
/// Answer to questionnaire
/// </summary>
[Serializable]
public struct QuestionnaireAnswer
{
    public DateTime Timestamp;
    public int Enjoyment;
    public int GameFeel;
    public int SubjectiveAccuracy;
    public int Controllability;
}
