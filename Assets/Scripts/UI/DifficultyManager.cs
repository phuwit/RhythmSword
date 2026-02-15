using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    public GameObject difficultyPanel;
    public Transform difficultyButtonParent;
    public GameObject difficultyButtonPrefab;

    void Awake()
    {
        Instance = this;
        difficultyPanel.SetActive(false);
    }

    public void ShowDifficulties(SongInfo song, string path)
    {
        difficultyPanel.SetActive(true);

        // ลบปุ่มเก่า
        foreach (Transform child in difficultyButtonParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var set in song._difficultyBeatmapSets)
        {
            foreach (var diff in set._difficultyBeatmaps)
            {
                GameObject btn = Instantiate(difficultyButtonPrefab, difficultyButtonParent);

                btn.GetComponentInChildren<TextMeshProUGUI>().text = diff._difficulty;

                btn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Debug.Log("Selected Difficulty: " + diff._difficulty);

                    // ตรงนี้ไว้เข้าเกมจริง
                });
            }
        }
    }
}
