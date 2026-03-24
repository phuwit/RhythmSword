using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.IO;

using SimpleJSON;

using TMPro;
using System.Data.SqlTypes;
using UnityEngine.Networking;


[RequireComponent(typeof(AudioSource))]
public class MapManager : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private UiInteractionsManager uiInteractionsManager;
    [SerializeField] private GameState gameState;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private Material planeArrowMaterial;
    [SerializeField] private Material planeDotMaterial;
    [SerializeField] private Material outerBodyLeftMaterial;
    [SerializeField] private Material outerBodyRightMaterial;
    [SerializeField] private Material innerBodyLeftMaterial;
    [SerializeField] private Material innerBodyRightMaterial;
    [SerializeField] private float laneWidth = 0.6f;
    [SerializeField] private float rowHeight = 0.55f;
    [SerializeField] private int initialPoolSize = 32;
    [SerializeField] private float audioDelay = 0.12f;  // Positive is for compensating audio lagging behind
    [SerializeField] private float noteXOffset = 0f;
    [SerializeField] private float noteYOffset = 0.6f;
    [SerializeField] private float noteZOffset = 0.65f;
    [SerializeField] private float halfJumpSpeedFactor = 3f;

    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalAccuracyText;
    [SerializeField] private TMP_Text finalComboText;
    [SerializeField] private TMP_Text finalRankText;


    private int bpm;
    private int noteJumpSpeed;
    private float noteJumpStartBeatOffset;
    private float noteHalfJumpDuration;
    private float noteJumpDistance;
    private float initialSpawnDistance;
    private string songRootPath;

    private readonly List<Note> notes = new();
    private int currentNoteIndex = 0;

    private readonly HashSet<NoteInstance> activeNoteInstances = new();
    private readonly Stack<NoteInstance> inactiveNoteInstances = new();
    private readonly Dictionary<GameObject, NoteInstance> noteInstanceLookup = new();

    private readonly NoteLineIndex[] noteLineIndexLookup = { NoteLineIndex.LeftMost, NoteLineIndex.CenterLeft, NoteLineIndex.CenterRight, NoteLineIndex.RightMost };
    private readonly NoteLineLayer[] noteLineLayerLookup = { NoteLineLayer.Bottom, NoteLineLayer.Center, NoteLineLayer.Top };
    private readonly NoteColor[] noteColorLookup = { NoteColor.Left, NoteColor.Right };
    private readonly NoteCutDirection[] noteCutDirectionLookup = { NoteCutDirection.Up, NoteCutDirection.Down, NoteCutDirection.Left, NoteCutDirection.Right, NoteCutDirection.UpLeft, NoteCutDirection.UpRight, NoteCutDirection.DownLeft, NoteCutDirection.DownRight, NoteCutDirection.Any };
    private readonly Dictionary<NoteLineIndex, float> noteXPositionLookup = new();
    private readonly Dictionary<NoteLineLayer, float> noteYPositionLookup = new();
    private readonly Dictionary<NoteCutDirection, float> noteRotationLookup = new() {
        {NoteCutDirection.Up, 0f},
        {NoteCutDirection.UpLeft, 45f},
        {NoteCutDirection.Left, 90f},
        {NoteCutDirection.DownLeft, 135f},
        {NoteCutDirection.Down, 180f},
        {NoteCutDirection.DownRight, 225f},
        {NoteCutDirection.Right, 270f},
        {NoteCutDirection.UpRight, 315f},
        {NoteCutDirection.Any, 0f},
    };

    private string RemoveExtension(string name) {
        int lastPeriodIndex = name.LastIndexOf('.');
        if (lastPeriodIndex >= 0) {
            return name[..lastPeriodIndex];
        }
        return name;
    }

    // From https://github.com/AllPoland/ArcViewer/blob/5afafd1bfbec00959f68e94118eb712ead704061/Assets/__Scripts/Previewer/MapControl/BeatmapManager.cs#L134
    // GNU General Public License v3.0
    private float GetJumpDistance(float hjd, int bpm, int njs) {
        float rt = 60f / bpm * hjd;
        return njs * 2f * rt;
    }

    private void CreateNoteInstance() {
        GameObject gameObject = Instantiate(notePrefab, new Vector3(), new Quaternion());
        ColorNote script = gameObject.GetComponent<ColorNote>();
        Renderer arrowRenderer = gameObject.transform.Find("ArrowPlane").gameObject.GetComponent<Renderer>();
        Renderer outerBodyRenderer = gameObject.transform.Find("OuterBody").gameObject.GetComponent<Renderer>();
        Renderer innerBodyRenderer = gameObject.transform.Find("InnerBody").gameObject.GetComponent<Renderer>();
        NoteInstance noteInstance = new() {
            gameObject = gameObject,
            note = new Note(),
            script = script,
            arrowRenderer = arrowRenderer,
            outerBodyRenderer = outerBodyRenderer,
            innerBodyRenderer = innerBodyRenderer,
        };

        gameObject.SetActive(false);

        inactiveNoteInstances.Push(noteInstance);
        noteInstanceLookup[gameObject] = noteInstance;
    }

    public NoteInstance GetNoteInstanceFromGameObject(GameObject gameObject) {
        return noteInstanceLookup[gameObject];
    }

    public float GetCurrentBeat() {
        if (audioSource.isPlaying) {
            float currentSongTime = audioSource.time - audioDelay;
            return bpm / 60f * currentSongTime;
        } else {
            return 0f;
        }
    }

    void Start() {
        string mapDirName = gameState.mapDirName;
        songRootPath = Path.Join(Application.persistentDataPath, "Songs");

        Debug.Log($"MAP DEBUG → Folder: {gameState.mapDirName} | Difficulty: {gameState.difficulty}");

        if (mapDirName.Length == 0) {
            Debug.LogError("Game scene manager: Error: Map not set");
            throw new System.Exception("Map not set");
        }
        if (gameState.difficulty.Length == 0) {
            Debug.LogError("Game scene manager: Error: Difficulty not set");
            throw new System.Exception("Difficulty not set");
        }

        string mapInfoPath = Path.Join(songRootPath, Path.Join(mapDirName, "info.dat"));
        if (!File.Exists(mapInfoPath)) {
            Debug.LogError($"Game scene manager: Error: Map file not found ({mapInfoPath})");
            throw new System.Exception("Map file not found");
        }
        string mapInfoText = File.ReadAllText(mapInfoPath);

        JSONNode mapInfo = JSON.Parse(mapInfoText);

        if (!mapInfo.HasKey("_version")) {
            Debug.LogError("Game scene manager: Error: Map info format is unsuppored (only version 2.x is suppored)");
            throw new System.Exception("Map info format is unsuppored");
        }
        string mapVersion = mapInfo["_version"];
        if (!mapVersion.StartsWith("2.")) {
            Debug.LogError($"Map version {mapVersion} is unsupported (only 2.x is supported)");
            throw new System.Exception("Map version is unsuppored");
        }

        bpm = mapInfo["_beatsPerMinute"].AsInt;

        string mapNotesFileName = "";

        foreach (JSONNode difficultySet in mapInfo["_difficultyBeatmapSets"].AsArray) {
            if (difficultySet["_beatmapCharacteristicName"] != "Standard") {
                continue;
            }

            foreach (JSONNode difficulty in difficultySet["_difficultyBeatmaps"].AsArray) {
                if (difficulty["_difficulty"] == gameState.difficulty) {
                    mapNotesFileName = difficulty["_beatmapFilename"];

                    noteJumpSpeed = difficulty["_noteJumpMovementSpeed"].AsInt;
                    noteJumpStartBeatOffset = difficulty["_noteJumpStartBeatOffset"].AsFloat;

                    break;
                }
            }
        }

        if (mapNotesFileName.Length == 0) {
            Debug.LogError("Game scene manager: Error: Invalid difficulty");
            throw new System.Exception("Difficulty is invalid");
        }

        string mapNotesPath = Path.Join(songRootPath, Path.Join(mapDirName, mapNotesFileName));
        if (!File.Exists(mapNotesPath)) {
            Debug.LogError($"Game scene manager: Error: Notes file not found ({mapNotesPath})");
            throw new System.Exception("Notes file not found");
        }
        string mapNotesText = File.ReadAllText(mapNotesPath);

        JSONNode mapNotes = JSON.Parse(mapNotesText);

        string mapNotesVersion = "0.0.0";
        if (mapNotes.HasKey("version")) {
            mapNotesVersion = mapNotes["version"];
        } else if (mapNotes.HasKey("_version")) {
            mapNotesVersion = mapNotes["_version"];
        }

        if (!(mapNotesVersion.StartsWith("2.") || mapNotesVersion.StartsWith("3."))) {
            Debug.LogError($"Map notes version {mapNotesVersion} is unsupported (only 2.x and 3.x is supported)");
            throw new System.Exception("Map notes version is unsupported");
        }

        int mapNotesMajorVersion = 3;
        if (mapNotesVersion.StartsWith("2.")) {
            mapNotesMajorVersion = 2;
        }

        if (mapNotesMajorVersion == 2) {
            foreach (JSONNode note in mapNotes["_notes"].AsArray) {
                if (note["_type"].AsInt == 0 || note["_type"].AsInt == 1) {
                    notes.Add(new Note { beat = note["_time"].AsFloat, lineIndex = noteLineIndexLookup[note["_lineIndex"].AsInt], lineLayer = noteLineLayerLookup[note["_lineLayer"].AsInt], color = noteColorLookup[note["_type"].AsInt], cutDirection = noteCutDirectionLookup[note["_cutDirection"].AsInt], angleOffset = 0 });
                }
            }
        } else {
            foreach (JSONNode note in mapNotes["colorNotes"].AsArray) {
                if (note["c"].AsInt == 0 || note["c"].AsInt == 1) {
                    notes.Add(new Note { beat = note["b"].AsFloat, lineIndex = noteLineIndexLookup[note["x"].AsInt], lineLayer = noteLineLayerLookup[note["y"].AsInt], color = noteColorLookup[note["c"].AsInt], cutDirection = noteCutDirectionLookup[note["d"].AsInt], angleOffset = note["a"].AsInt });
                }
            }
        }

        Debug.Log($"Loaded {notes.Count} notes");  // TODO: Remove

        noteXPositionLookup[NoteLineIndex.LeftMost] = laneWidth * -1.5f;
        noteXPositionLookup[NoteLineIndex.CenterLeft] = laneWidth * -0.5f;
        noteXPositionLookup[NoteLineIndex.CenterRight] = laneWidth * 0.5f;
        noteXPositionLookup[NoteLineIndex.RightMost] = laneWidth * 1.5f;

        noteYPositionLookup[NoteLineLayer.Bottom] = 0f;
        noteYPositionLookup[NoteLineLayer.Center] = 0.55f;
        noteYPositionLookup[NoteLineLayer.Top] = 1.05f;

        noteHalfJumpDuration = 4f;
        while (GetJumpDistance(noteHalfJumpDuration, bpm, noteJumpSpeed) > 35.998f) {
            noteHalfJumpDuration /= 2f;
        }
        noteHalfJumpDuration = Mathf.Max(noteHalfJumpDuration + noteJumpStartBeatOffset, 0.25f);
        noteJumpDistance = GetJumpDistance(noteHalfJumpDuration, bpm, noteJumpSpeed);
        initialSpawnDistance = noteJumpDistance * 3f;

        for (int i = 0; i < initialPoolSize; i++) {
            CreateNoteInstance();
        }
        
        string songPath = Path.Join(songRootPath, Path.Join(mapDirName, (string)mapInfo["_songFilename"]));
        if (!File.Exists(songPath)) {
            Debug.LogError($"Game scene manager: Error: Song file not found ({songPath})");
            throw new System.Exception("Song file not found");
        }

        scoreManager.Init(notes.Count, bpm);

        StartCoroutine(StartSong(songPath));
    }

    private IEnumerator StartSong(string filePath) {
        yield return new WaitForSeconds(1f);
        // UnityWebRequest requires a URI scheme. Local files need the "file://" prefix.
        string uriPath = filePath;
        
        // Ensure the path is properly formatted for cross-platform WebRequests
        if (!uriPath.StartsWith("file://") && !uriPath.StartsWith("http"))
        {
            // On Windows, paths might look like "C:\...", so we format it safely.
            // On Android, paths start with "/", so adding "file://" makes it "file:///"
            uriPath = "file://" + uriPath;
        }
        // Request the file as an OGG Vorbis audio clip
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.OGGVORBIS))
        {
            // Wait for the download to complete
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error loading OGG file: {uwr.error}\nAttempted Path: {uriPath}");
            }
            else
            {
                // Extract the audio clip from the request
                AudioClip loadedClip = DownloadHandlerAudioClip.GetContent(uwr);
                
                // Optional: Give the clip a name for easier debugging in the editor
                loadedClip.name = Path.GetFileName(filePath);

                // Assign and play
                audioSource.clip = loadedClip;
                audioSource.Play();
                
                Debug.Log($"Successfully loaded and playing: {loadedClip.name}");
            }
        }
    }

    private NoteInstance GetNoteInstance() {
        while (inactiveNoteInstances.Count < 1) {
            CreateNoteInstance();
        }

        NoteInstance noteInstance = inactiveNoteInstances.Pop();
        activeNoteInstances.Add(noteInstance);

        return noteInstance;
    }

    public void DeactivateNoteInstance(NoteInstance noteInstance) {
        noteInstance.gameObject.SetActive(false);
        activeNoteInstances.Remove(noteInstance);
        inactiveNoteInstances.Push(noteInstance);
    }

    void Update() {
        if (audioSource.isPlaying) {
            float currentSongTime = audioSource.time - audioDelay;
            float currentBeat = bpm / 60f * currentSongTime;

            while (currentNoteIndex < notes.Count && notes[currentNoteIndex].beat - (1f + (1f / halfJumpSpeedFactor)) * noteHalfJumpDuration < currentBeat) {
                Note note = notes[currentNoteIndex++];
                NoteInstance noteInstance = GetNoteInstance();

                noteInstance.note = note;

                Vector3 position = new(noteXPositionLookup[note.lineIndex] + noteXOffset, noteYPositionLookup[note.lineLayer] + noteYOffset, initialSpawnDistance + noteZOffset);
                Quaternion rotation = Quaternion.Euler(0f, 0f, noteRotationLookup[note.cutDirection] + note.angleOffset);
                noteInstance.gameObject.transform.SetPositionAndRotation(position, rotation);

                if (note.color == NoteColor.Left) {
                    noteInstance.outerBodyRenderer.material = outerBodyLeftMaterial;
                    noteInstance.innerBodyRenderer.material = innerBodyLeftMaterial;
                } else {
                    noteInstance.outerBodyRenderer.material = outerBodyRightMaterial;
                    noteInstance.innerBodyRenderer.material = innerBodyRightMaterial;
                }

                if (note.cutDirection == NoteCutDirection.Any) {
                    noteInstance.arrowRenderer.material = planeDotMaterial;
                } else {
                    noteInstance.arrowRenderer.material = planeArrowMaterial;
                }

                ColorNote script = noteInstance.script;
                script.beat = note.beat;
                script.lineIndex = note.lineIndex;
                script.lineLayer = note.lineLayer;
                script.color = note.color;
                script.cutDirection = note.cutDirection;

                noteInstance.gameObject.SetActive(true);
            }

            HashSet<NoteInstance> expiredNoteInstances = new();

            foreach (NoteInstance noteInstance in activeNoteInstances) {
                Note note = noteInstance.note;
                float beat = note.beat;
                float beatLeft = beat - currentBeat;
                float distance;

                if (beatLeft > noteHalfJumpDuration) {
                    float halfJumpTimeLeft = (beatLeft - noteHalfJumpDuration) / bpm * 60f;
                    float halfJumpTotalTime = noteHalfJumpDuration / halfJumpSpeedFactor / bpm * 60f;
                    float halfJumpTimePassed = halfJumpTotalTime - halfJumpTimeLeft;
                    float halfJumpDistance = initialSpawnDistance - noteJumpDistance / 2f;
                    distance = initialSpawnDistance - (
                        3f * (
                            halfJumpDistance -
                            noteJumpSpeed * halfJumpTotalTime
                        ) /
                        Mathf.Pow(halfJumpTotalTime, 3f) *
                        (
                            Mathf.Pow(halfJumpTotalTime, 2f) * halfJumpTimePassed -
                            halfJumpTotalTime * Mathf.Pow(halfJumpTimePassed, 2f) +
                            Mathf.Pow(halfJumpTimePassed, 3f) / 3f
                        ) +
                        noteJumpSpeed * halfJumpTimePassed
                    );
                } else {
                    distance = beatLeft / bpm * 60f * noteJumpSpeed;
                }

                float zPosition = distance + noteZOffset;

                if (zPosition < -2f) {  // 2 m. behind player, despawn
                    expiredNoteInstances.Add(noteInstance);
                }

                Vector3 position = noteInstance.gameObject.transform.position;
                position.z = zPosition;
                noteInstance.gameObject.transform.position = position;
            }

            foreach (NoteInstance noteInstance in expiredNoteInstances) {
                scoreManager.RegisterHit(noteInstance.note, new(0, false, false, false));
                DeactivateNoteInstance(noteInstance);
            }
        } else {
            HashSet<NoteInstance> expiredNoteInstances = new();

            foreach (NoteInstance noteInstance in activeNoteInstances) {
                Vector3 position = noteInstance.gameObject.transform.position;
                position.z += noteJumpSpeed * Time.deltaTime;

                if (position.z < -2f) {  // 2 m. behind player, despawn
                    expiredNoteInstances.Add(noteInstance);
                }

                noteInstance.gameObject.transform.position = position;
            }

            foreach (NoteInstance noteInstance in expiredNoteInstances) {
                scoreManager.RegisterHit(noteInstance.note, new(0, false, false, false));
                DeactivateNoteInstance(noteInstance);
            }

            Debug.Log($"timesamples: {audioSource.timeSamples}");

            if (activeNoteInstances.Count <= 0 && currentNoteIndex != 0) {
                // level completed
                endGamePanel.SetActive(true);
                uiInteractionsManager.SetUiInteraction(true);

                finalScoreText.text = "Score : " + scoreManager.GetScore().ToString();
                finalAccuracyText.text = "Accuracy : " + scoreManager.GetAccuracy().ToString("F2") + "%";
                finalComboText.text = "Max Combo : " + scoreManager.GetMaxCombo().ToString();
                finalRankText.text = "Rank : " + scoreManager.GetRank();
            }
        }
    }
}

public class Note {
    public float beat;
    public NoteLineIndex lineIndex;
    public NoteLineLayer lineLayer;
    public NoteColor color;
    public NoteCutDirection cutDirection;
    public int angleOffset;
}

public class NoteInstance {
    public GameObject gameObject;
    public Note note;
    public ColorNote script;
    public Renderer outerBodyRenderer;
    public Renderer innerBodyRenderer;
    public Renderer arrowRenderer;
}
