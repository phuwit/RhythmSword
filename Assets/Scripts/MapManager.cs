using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using SimpleJSON;


[RequireComponent(typeof(AudioSource))]
public class MapManager : MonoBehaviour {
    private AudioSource audioSource;

    [SerializeField] private GameState gameState;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Material planeArrowMaterial;
    [SerializeField] private Material planeDotMaterial;
    [SerializeField] private Material outerBodyLeftMaterial;
    [SerializeField] private Material outerBodyRightMaterial;
    [SerializeField] private Material innerBodyLeftMaterial;
    [SerializeField] private Material innerBodyRightMaterial;
    [SerializeField] private int initialPoolSize = 32;
    [SerializeField] private float audioDelay = 0.12f;  // Positive is for compensating audio lagging behind
    [SerializeField] private float noteXOffset = 0f;
    [SerializeField] private float noteYOffset = 0f;
    [SerializeField] private float noteZOffset = 2f;
    [SerializeField] private float halfJumpSpeedFactor = 3f;

    private int bpm;
    private int noteJumpSpeed;
    private float noteJumpStartBeatOffset;
    private float noteHalfJumpDuration;
    private float noteJumpDistance;
    private float initialSpawnDistance;

    private readonly List<Note> notes = new();
    private int currentNoteIndex = 0;

    private readonly HashSet<NoteInstance> activeNoteInstances = new();
    private readonly Stack<NoteInstance> inactiveNoteInstances = new();

    private readonly NoteLineIndex[] noteLineIndexLookup = { NoteLineIndex.LeftMost, NoteLineIndex.CenterLeft, NoteLineIndex.CenterRight, NoteLineIndex.RightMost };
    private readonly NoteLineLayer[] noteLineLayerLookup = { NoteLineLayer.Bottom, NoteLineLayer.Center, NoteLineLayer.Top };
    private readonly NoteColor[] noteColorLookup = { NoteColor.Left, NoteColor.Right };
    private readonly NoteCutDirection[] noteCutDirectionLookup = { NoteCutDirection.Up, NoteCutDirection.Down, NoteCutDirection.Left, NoteCutDirection.Right, NoteCutDirection.UpLeft, NoteCutDirection.UpRight, NoteCutDirection.DownLeft, NoteCutDirection.DownRight, NoteCutDirection.Any };
    private readonly Dictionary<NoteLineIndex, float> noteXPositionLookup = new() {
        {NoteLineIndex.LeftMost, - 1.5f},
        {NoteLineIndex.CenterLeft, - 0.5f},
        {NoteLineIndex.CenterRight, 0.5f},
        {NoteLineIndex.RightMost, 1.5f},
    };
    private readonly Dictionary<NoteLineLayer, float> noteYPositionLookup = new() {
        {NoteLineLayer.Bottom, 0.5f},
        {NoteLineLayer.Center, 1.5f},
        {NoteLineLayer.Top, 2.5f},
    };
    private readonly Dictionary<NoteCutDirection, float> noteRotationLookup = new() {
        {NoteCutDirection.Down, 0f},
        {NoteCutDirection.DownRight, 45f},
        {NoteCutDirection.Right, 90f},
        {NoteCutDirection.UpRight, 135f},
        {NoteCutDirection.Up, 180f},
        {NoteCutDirection.UpLeft, 225f},
        {NoteCutDirection.Left, 270f},
        {NoteCutDirection.DownLeft, 315f},
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
    }

    void Start() {
        string mapDirName = gameState.mapDirName;

        if (mapDirName.Length == 0) {
            Debug.LogError("Game scene manager: Error: Map not set");
            throw new System.Exception("Map not set");
        }
        if (gameState.difficulty.Length == 0) {
            Debug.LogError("Game scene manager: Error: Difficulty not set");
            throw new System.Exception("Difficulty not set");
        }

        string mapInfoPath = "Maps/" + mapDirName + "/info";
        TextAsset mapInfoFile = Resources.Load<TextAsset>(mapInfoPath);
        if (mapInfoFile == null) {
            Debug.LogError($"Game scene manager: Error: Map file not found ({mapInfoPath})");
            throw new System.Exception("Map file not found");
        }

        JSONNode mapInfo = JSON.Parse(mapInfoFile.text);

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
                    mapNotesFileName = RemoveExtension(mapNotesFileName);

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

        string mapNotesPath = "Maps/" + mapDirName + "/" + mapNotesFileName;
        TextAsset mapNotesFile = Resources.Load<TextAsset>(mapNotesPath);
        if (mapNotesFile == null) {
            Debug.LogError($"Game scene manager: Error: Notes file not found ({mapNotesPath})");
            throw new System.Exception("Notes file not found");
        }

        JSONNode mapNotes = JSON.Parse(mapNotesFile.text);

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

        string songPath = "Maps/" + mapDirName + "/" + RemoveExtension(mapInfo["_songFilename"]);
        AudioClip songClip = Resources.Load<AudioClip>(songPath);

        if (songClip == null) {
            Debug.LogError($"Game scene manager: Error: Song file not found ({songPath})");
            throw new System.Exception("Song file not found");
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = songClip;

        StartCoroutine(StartSong());
    }

    private IEnumerator StartSong() {
        yield return new WaitForSeconds(1f);
        audioSource.Play();
    }

    private NoteInstance GetNoteInstance() {
        while (inactiveNoteInstances.Count < 1) {
            CreateNoteInstance();
        }

        NoteInstance noteInstance = inactiveNoteInstances.Pop();
        activeNoteInstances.Add(noteInstance);
        noteInstance.gameObject.SetActive(true);

        return noteInstance;
    }

    private void DeactivateNoteInstance(NoteInstance noteInstance) {
        noteInstance.gameObject.SetActive(false);
        activeNoteInstances.Remove(noteInstance);
        inactiveNoteInstances.Push(noteInstance);
    }

    void Update() {
        if (audioSource.isPlaying) {
            float currentSongTime = audioSource.time - audioDelay;
            float currentBeat = bpm / 60f * currentSongTime;

            while (currentNoteIndex < notes.Count && notes[currentNoteIndex].beat - 1.5f * noteHalfJumpDuration < currentBeat) {
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
                DeactivateNoteInstance(noteInstance);
            }
        }
    }
}

internal class Note {
    public float beat;
    public NoteLineIndex lineIndex;
    public NoteLineLayer lineLayer;
    public NoteColor color;
    public NoteCutDirection cutDirection;
    public int angleOffset;
}

internal class NoteInstance {
    public GameObject gameObject;
    public Note note;
    public ColorNote script;
    public Renderer outerBodyRenderer;
    public Renderer innerBodyRenderer;
    public Renderer arrowRenderer;
}
