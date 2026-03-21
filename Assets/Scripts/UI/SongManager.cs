using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;



[System.Serializable]
public class DifficultyBeatmap
{
    public string _difficulty;
}

[System.Serializable]
public class DifficultyBeatmapSet
{
    public string _beatmapCharacteristicName;
    public DifficultyBeatmap[] _difficultyBeatmaps;
}

[System.Serializable]
public class SongInfo
{
    public string _songName;
    public string _songAuthorName;
    public string _coverImageFilename;

    public DifficultyBeatmapSet[] _difficultyBeatmapSets;
}

public class SongManager : MonoBehaviour
{
    public Transform songListParent;   // ต้องเป็น Content
    public GameObject songItemPrefab;


    void Start()
    {
        StartCoroutine(LoadSongs());
    }

    IEnumerator LoadSongs()
    {
        string songsPath = Path.Combine(Application.streamingAssetsPath, "Songs");
        if (Application.streamingAssetsPath.StartsWith("jar") || Application.streamingAssetsPath.StartsWith("http"))
        {
            songsPath = Path.Combine(Application.persistentDataPath, "Songs");
        }

        if (!Directory.Exists(songsPath))
        {
            Debug.LogError("Songs folder not found!");
            yield break;
        }

        string[] directories = Directory.GetDirectories(songsPath);

        foreach (string dir in directories)
        {
            string infoPath = Path.Combine(dir, "Info.dat");

            string json = File.ReadAllText(infoPath);
            SongInfo song = JsonUtility.FromJson<SongInfo>(json);

            // 🔥 สร้าง SongItem
            GameObject item = Instantiate(songItemPrefab);
            item.transform.SetParent(songListParent, false);

            // ========================
            // 🎯 ผูก SongItemUI ให้กดแล้วแสดง difficulty
            // ========================
            SongItemUI ui = item.GetComponent<SongItemUI>();

            if (ui != null)
            {
                ui.Setup(song, dir);
            }
            else
            {
                Debug.LogError("SongItemUI not found on prefab!");
            }


            // ========================
            // 🎵 ตั้งค่า Text
            // ========================
            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length > 0)
                texts[0].text = song._songName;

            if (texts.Length > 1)
                texts[1].text = song._songAuthorName;

            // ========================
            // 🖼 โหลด Cover Image
            // ========================
            string coverPath = Path.Combine(dir, song._coverImageFilename);


            if (!File.Exists(coverPath))
            {
                coverPath = Path.Combine(dir, "cover.png");
            }

            if (File.Exists(coverPath))
            {
                byte[] imageBytes = File.ReadAllBytes(coverPath);

                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes); 

                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );

                Image[] images = item.GetComponentsInChildren<Image>();

                foreach (Image img in images)
                {
                    if (img.gameObject.name.Contains("Cover"))
                    {
                        img.sprite = sprite;
                        break;
                    }
                }
            }
        }
    }
}
