using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class HighScoresDisplay : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryTemplate;
    [SerializeField] private GameObject newRecordInputArea; 
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TextMeshProUGUI yourScoreText; 

    [Header("Datos")]
    [SerializeField] private HighScoresData highScoresData;

    private List<GameObject> displayEntries = new List<GameObject>();

    private void Awake()
    {
        if(entryTemplate != null) entryTemplate.SetActive(false);
        if(newRecordInputArea != null) newRecordInputArea.SetActive(false);
    }

    public void Setup(int currentScore, bool isNewRecord)
    {
        if (yourScoreText != null) 
            yourScoreText.text = $"TU PUNTUACIÓN: {currentScore}";

        if (isNewRecord)
        {
            if(newRecordInputArea != null) 
            {
                newRecordInputArea.SetActive(true);
                if(nameInputField != null) nameInputField.Select();
            }
        }
        else
        {
            if(newRecordInputArea != null) newRecordInputArea.SetActive(false);
        }

        UpdateDisplay();
    }

    // ESTA ES LA FUNCIÓN QUE BUSCAS. ES PÚBLICA, DEBE SALIR.
    public void OnSaveButtonClicked()
    {
        string playerName = "Soldado";
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            playerName = nameInputField.text.Trim();
        }

        if (highScoresData != null)
        {
            highScoresData.AddNewScore(playerName);
        }

        if(newRecordInputArea != null) newRecordInputArea.SetActive(false);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        foreach (GameObject go in displayEntries) Destroy(go);
        displayEntries.Clear();

        if (highScoresData == null) return;

        var scores = highScoresData.GetScores();
        for (int i = 0; i < scores.Count; i++)
        {
            GameObject entry = Instantiate(entryTemplate, entryContainer);
            entry.SetActive(true); 
            
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString(); 
                texts[1].text = scores[i].playerName; 
                texts[2].text = scores[i].score.ToString(); 
            }

            displayEntries.Add(entry);
        }
    }
}