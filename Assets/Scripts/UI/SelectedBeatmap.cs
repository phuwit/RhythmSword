using UnityEngine;

public class SelectedBeatmap : MonoBehaviour
{
    public static SelectedBeatmap Instance;
    public static string SongFolder;
    public static string DifficultyFile;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }   
    }
}
