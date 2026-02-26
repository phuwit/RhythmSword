using UnityEngine;

using System.Collections.Generic;

using SimpleJSON;
using System.Collections;


[RequireComponent(typeof(AudioSource))]
public class NoteSpawner : MonoBehaviour {
    private AudioSource audioSource;

    [SerializeField] private GameState gameState;
    [SerializeField] private float audioDelay;  // Positive is audio lagging behind
    [SerializeField] private float noteXOffset;
    [SerializeField] private float noteYOffset;
    public GameObject notePrefab;
    public Material noteArrowMaterial;
    public Material noteDotMaterial;

    private int bpm;
    private int noteJumpSpeed;
    private float noteJumpStartBeatOffset;
    private float noteHalfJumpDuration;
    private float noteJumpDistance;

    private readonly List<Note> notes = new();
    private int currentNoteIndex = 0;

    private readonly List<GameObject> noteInstances = new();

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

    void Start() {
        string mapDirName = gameState.mapDirName;

        if (mapDirName.Length == 0) {
            Debug.LogError("Game scene manager: Error: Map not set");
            throw new System.Exception("Map not set when loading game scene");
        }
        if (gameState.difficulty.Length == 0) {
            Debug.LogError("Game scene manager: Error: Difficulty not set");
            throw new System.Exception("Difficulty not set when loading game scene");
        }

        string mapInfoPath = "Maps/" + mapDirName + "/info";
        TextAsset mapInfoFile = Resources.Load<TextAsset>(mapInfoPath);
        if (mapInfoFile == null) {
            Debug.LogError($"Game scene manager: Error: Map file not found ({mapInfoPath})");
            throw new System.Exception("Map file not found when loading game scene");
        }
        JSONNode mapInfo = JSON.Parse(mapInfoFile.text);

        string mapVersion = mapInfo["_version"];
        if (!mapVersion.StartsWith("2.")) {
            Debug.LogError($"Map version {mapVersion} is unsupported (only 2.x is supported)");
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
            throw new System.Exception("Difficulty is invalid when loading game scene");
        }

        string mapNotesPath = "Maps/" + mapDirName + "/" + mapNotesFileName;
        TextAsset mapNotesFile = Resources.Load<TextAsset>(mapNotesPath);
        if (mapNotesFile == null) {
            Debug.LogError($"Game scene manager: Error: Notes file not found ({mapNotesPath})");
            throw new System.Exception("Notes file not found when loading game scene");
        }
        JSONNode mapNotes = JSON.Parse(mapNotesFile.text);

        string mapNotesVersion = "0.0.0";
        if (mapNotes.HasKey("version")) {
            mapNotesVersion = mapNotes["version"];
        }
        else if (mapNotes.HasKey("_version")) {
            mapNotesVersion = mapNotes["_version"];
        }

        if (!(mapNotesVersion.StartsWith("2.") || mapNotesVersion.StartsWith("3."))) {
            Debug.LogError($"Map notes version {mapNotesVersion} is unsupported (only 2.x and 3.x is supported)");
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
        }
        else {
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

        string musicPath = "Maps/" + mapDirName + "/" + RemoveExtension(mapInfo["_songFilename"]);
        AudioClip musicClip = Resources.Load<AudioClip>(musicPath);

        if (musicClip == null) {
            Debug.LogError($"Game scene manager: Error: Music file not found ({musicPath})");
            throw new System.Exception("Music file not found when loading game scene");
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = musicClip;

        StartCoroutine(StartMusic());
    }

    IEnumerator StartMusic() {
        yield return new WaitForSeconds(1f);
        audioSource.Play();
    }

    void Update() {
        if (audioSource.isPlaying) {
            float currentMusicTime = audioSource.time - audioDelay;
            float currentBeat = bpm / 60f * currentMusicTime;

            while (currentNoteIndex < notes.Count && notes[currentNoteIndex].beat + noteHalfJumpDuration < currentBeat) {
                Note note = notes[currentNoteIndex];
                Vector3 position = new(noteXPositionLookup[note.lineIndex] + noteXOffset, noteYPositionLookup[note.lineLayer] + noteYOffset, noteJumpDistance);
                Quaternion rotation = Quaternion.Euler(0f, 0f, noteRotationLookup[note.cutDirection] + note.angleOffset);
                GameObject noteInstance = Instantiate(notePrefab, position, rotation);
                noteInstances.Add(noteInstance);

                Renderer renderer = noteInstance.GetComponent<Renderer>();
                if (note.cutDirection == NoteCutDirection.Any) {
                    renderer.material = noteDotMaterial;
                }
                else {
                    renderer.material = noteArrowMaterial;
                }

                ColorNote colorNote = noteInstance.GetComponent<ColorNote>();
                colorNote.beat = note.beat;
                colorNote.lineIndex = note.lineIndex;
                colorNote.lineLayer = note.lineLayer;
                colorNote.color = note.color;
                colorNote.cutDirection = note.cutDirection;

                currentNoteIndex++;
            }

            List<GameObject> expiredNoteInstances = new();

            foreach (GameObject noteInstance in noteInstances) {
                ColorNote colorNote = noteInstance.GetComponent<ColorNote>();
                float beat = colorNote.beat;
                float beatDelta = currentBeat - beat;
                float distance = noteJumpDistance - beatDelta / bpm * 60f * noteJumpSpeed;

                if (distance < -2f) {  // 2 m. behind player, despawn
                    expiredNoteInstances.Add(noteInstance);
                }

                Vector3 position = noteInstance.transform.position;
                position.z = distance;
                noteInstance.transform.position = position;
            }

            foreach (GameObject noteInstance in expiredNoteInstances) {
                noteInstances.Remove(noteInstance);
                Destroy(noteInstance);
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
