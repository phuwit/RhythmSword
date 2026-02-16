using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    public GameObject difficultyPanel;
    public Transform difficultyButtonParent;
    public GameObject difficultyButtonPrefab;

    private List<DifficultyButtonUI> buttons = new List<DifficultyButtonUI>();
    private DifficultyButtonUI currentSelected;

    private string selectedSongPath;
    private string selectedDifficulty;

    public GameObject startButton;

    int GetDifficultyRank(string diff)
    {
        switch (diff)
        {
            case "Easy": return 0;
            case "Normal": return 1;
            case "Hard": return 2;
            case "Expert": return 3;
            case "ExpertPlus": return 4;
            default: return 99;
        }
    }



    void Awake()
    {
        Instance = this;
        difficultyPanel.SetActive(false);
    }

    public void ShowDifficulties(SongInfo song, string path)
    {
        difficultyPanel.SetActive(true);
        selectedSongPath = path;

        foreach (Transform child in difficultyButtonParent)
        {
            Destroy(child.gameObject);
        }

        buttons.Clear();
        currentSelected = null;

        List<DifficultyButtonUI> tempButtons = new List<DifficultyButtonUI>();

        foreach (var set in song._difficultyBeatmapSets)
        {
            foreach (var diff in set._difficultyBeatmaps)
            {
                GameObject btnObj = Instantiate(difficultyButtonPrefab, difficultyButtonParent);

                DifficultyButtonUI ui = btnObj.GetComponent<DifficultyButtonUI>();
                ui.label.text = diff._difficulty;
                ui.Setup(this, diff._difficulty);

                buttons.Add(ui);
                tempButtons.Add(ui);
            }
        }

        // 🔥 เลือก default = ง่ายที่สุด
        DifficultyButtonUI easiest = null;
        int bestRank = 999;

        foreach (var btn in tempButtons)
        {
            int rank = GetDifficultyRank(btn.label.text);
            if (rank < bestRank)
            {
                bestRank = rank;
                easiest = btn;
            }
        }

        if (easiest != null)
        {
            Select(easiest);
        }
    }


    public void Select(DifficultyButtonUI selected)
    {
        currentSelected = selected;
        selectedDifficulty = selected.label.text;

        foreach (var btn in buttons)
        {
            btn.SetSelected(btn == selected);
        }

        Debug.Log("Selected Difficulty: " + selectedDifficulty);
    }

    public void OnStartButton()
    {
        Debug.Log("Start Pressed");

        if (string.IsNullOrEmpty(selectedSongPath))
        {
            Debug.Log("No song selected");
            return;
        }

        PlayerPrefs.SetString("SelectedSongPath", selectedSongPath);
        PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);

        SceneManager.LoadScene("GameplayScene");
    }


}
