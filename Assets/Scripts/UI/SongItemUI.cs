using UnityEngine;
using UnityEngine.UI;

public class SongItemUI : MonoBehaviour
{
    private SongInfo songData;
    private string songPath;

    public Button button;

    public void Setup(SongInfo data, string path)
    {
        songData = data;
        songPath = path;

        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        DifficultyManager.Instance.ShowDifficulties(songData, songPath);
    }
}
