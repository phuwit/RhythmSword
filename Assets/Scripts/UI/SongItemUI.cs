using UnityEngine;
using UnityEngine.UI;

public class SongItemUI : MonoBehaviour
{
    [SerializeField] private Image smallCoverImage;
    private Image bigCoverImage;
    private SongInfo songData;
    private string songPath;

    public Button button;

    public void Setup(SongInfo data, string path, Image _bigCoverImage)
    {
        songData = data;
        songPath = path;
        bigCoverImage = _bigCoverImage;

        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        bigCoverImage.sprite = smallCoverImage.sprite;
        DifficultyManager.Instance.ShowDifficulties(songData, songPath);
    }
}
