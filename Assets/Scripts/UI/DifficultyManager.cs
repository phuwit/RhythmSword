using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;

public class DifficultyManager : MonoBehaviour {
    public static DifficultyManager Instance;

    public GameObject difficultyPanel;
    public Transform difficultyButtonParent;
    public GameObject difficultyButtonPrefab;
    public GameObject playButton;
    public SongManager songManager;

    public GameState gameState;

    private SongInfo currentSong;


    private List<DifficultyButtonUI> buttons = new List<DifficultyButtonUI>();
    private DifficultyButtonUI currentSelected;

    private string selectedSongPath;
    private string selectedDifficulty;


    int GetDifficultyRank(string diff) {
        switch (diff) {
            case "Easy": return 0;
            case "Normal": return 1;
            case "Hard": return 2;
            case "Expert": return 3;
            case "ExpertPlus": return 4;
            default: return 99;
        }
    }



    void Awake() {
        Instance = this;
        difficultyPanel.SetActive(false);
        playButton.SetActive(false);
    }

    public void ShowDifficulties(SongInfo song, string path) {
        difficultyPanel.SetActive(true);
        playButton.SetActive(true);

        currentSong = song;
        selectedSongPath = path;
        // selectedDifficulty = null;

        foreach (Transform child in difficultyButtonParent) {
            Destroy(child.gameObject);
        }

        buttons.Clear();
        currentSelected = null;

        List<DifficultyButtonUI> tempButtons = new List<DifficultyButtonUI>();

        foreach (var set in song._difficultyBeatmapSets) {
            foreach (var diff in set._difficultyBeatmaps) {
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

        foreach (var btn in tempButtons) {
            int rank = GetDifficultyRank(btn.label.text);
            if (rank < bestRank) {
                bestRank = rank;
                easiest = btn;
            }
        }

        if (easiest != null) {
            Select(easiest);
        }
    }


    public void Select(DifficultyButtonUI selected) {
        Debug.Log($"selecting {selected.label.text}");
        currentSelected = selected;
        selectedDifficulty = selected.label.text;

        foreach (var btn in buttons) {
            btn.SetSelected(btn == selected);
        }

    }

    public void OnPlayButton() {
        Debug.Log($"PLAY → Folder: {selectedSongPath} | Difficulty: {selectedDifficulty}");

        if (string.IsNullOrEmpty(selectedSongPath) ||
            string.IsNullOrEmpty(selectedDifficulty)) {
            Debug.Log("Song or Difficulty not selected");
            return;
        }

        gameState.mapDirName = Path.GetFileName(selectedSongPath);
        gameState.difficulty = selectedDifficulty;

        SceneManager.LoadScene("MainGame");
    }



}
