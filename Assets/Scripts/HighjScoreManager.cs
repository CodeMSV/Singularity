using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "HighScoresData", menuName = "Game Data/High Scores")]
public class HighScoresData : ScriptableObject
{
    private const string HighScoresKey = "HighScores_SaveData";
    private const int MaxEntries = 5;

    [System.Serializable]
    public struct HighScoreEntry
    {
        public string playerName;
        public int score;
    }

    [System.Serializable]
    public class ScoreList
    {
        public List<HighScoreEntry> entries = new List<HighScoreEntry>();
    }

    [SerializeField] private ScoreList currentScores = new ScoreList();
    
    [System.NonSerialized] private int pendingScore; 

    public void LoadScores()
    {
        if (PlayerPrefs.HasKey(HighScoresKey))
        {
            string json = PlayerPrefs.GetString(HighScoresKey);
            currentScores = JsonUtility.FromJson<ScoreList>(json);
        }
        else
        {
            currentScores = new ScoreList();
        }
    }

    private void SaveScores()
    {
        string json = JsonUtility.ToJson(currentScores);
        PlayerPrefs.SetString(HighScoresKey, json);
        PlayerPrefs.Save();
    }

    public bool CheckForNewHighScore(int score)
    {
        pendingScore = score;
        LoadScores();

        if (currentScores.entries.Count < MaxEntries) return true;
        
        SortScores();
        return score > currentScores.entries.Last().score;
    }

    public void AddNewScore(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) name = "Anónimo";

        HighScoreEntry newEntry = new HighScoreEntry
        {
            playerName = name,
            score = pendingScore
        };

        currentScores.entries.Add(newEntry);
        SortScores();

        if (currentScores.entries.Count > MaxEntries)
        {
            currentScores.entries.RemoveRange(MaxEntries, currentScores.entries.Count - MaxEntries);
        }

        SaveScores();
    }

    public List<HighScoreEntry> GetScores()
    {
        SortScores();
        return currentScores.entries;
    }

    private void SortScores()
    {
        currentScores.entries = currentScores.entries.OrderByDescending(e => e.score).ToList();
    }

    [ContextMenu("Borrar Puntuaciones")]
    public void ClearScores()
    {
        currentScores = new ScoreList();
        PlayerPrefs.DeleteKey(HighScoresKey);
        PlayerPrefs.Save();
    }
}