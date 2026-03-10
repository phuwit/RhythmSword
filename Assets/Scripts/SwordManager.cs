using UnityEngine;

public class SwordManager : MonoBehaviour {
    [SerializeField] private MapManager mapManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Transform handAnchor;
    [SerializeField] private Transform tipAnchor;
    [SerializeField] private bool isLeftSword;
    [SerializeField] private int steps = 8;
    [SerializeField] private float vibrationDuration = 0.1f;
    [SerializeField] private float vibrationIntensity = 1f;

    [SerializeField] private GameObject collideParticlesContainer;
    [SerializeField] private GameObject collideParticles;

    private Vector3 lastHandAnchorPosition = new();
    private Vector3 lastTipAnchorPosition = new();
    private readonly Collider[] colliders = new Collider[64];

    void Update() {
        Vector3 currentHandAnchorPosition = handAnchor.position;
        Vector3 currentTipAnchorPosition = tipAnchor.position;

        for (int step = 0; step < steps; step++) {
            float progress = (step + 1f) / steps;
            Vector3 handAnchorPosition = Vector3.Lerp(lastHandAnchorPosition, currentHandAnchorPosition, progress);
            Vector3 tipAnchorPosition = Vector3.Lerp(lastTipAnchorPosition, currentTipAnchorPosition, progress);

            int count = Physics.OverlapCapsuleNonAlloc(handAnchorPosition, tipAnchorPosition, 0.025f, colliders);

            for (int i = 0; i < count; i++) {
                Collider collider = colliders[i];
                if (collider.CompareTag("Note")) {
                    GameObject noteGameObject = collider.gameObject;
                    NoteInstance noteInstance = mapManager.GetNoteInstanceFromGameObject(noteGameObject);
                    float currentBeat = mapManager.GetCurrentBeat();
                    Debug.Log($"Hit. timing: {currentBeat - noteInstance.note.beat} (= {currentBeat} - {noteInstance.note.beat})\nPos: ({handAnchorPosition.x}, {handAnchorPosition.y}, {handAnchorPosition.z}) - ({tipAnchorPosition.x}, {tipAnchorPosition.y}, {tipAnchorPosition.z})");
                    bool wrongColor = !((isLeftSword && noteInstance.note.color == NoteColor.Left) || (!isLeftSword && noteInstance.note.color == NoteColor.Right));
                    HitData hitData = new(currentBeat, true, wrongColor, false);
                    scoreManager.RegisterHit(noteInstance.note, hitData);
                    mapManager.DeactivateNoteInstance(noteInstance);
                    VibrateController(0f, vibrationDuration, isLeftSword ? NoteColor.Left : NoteColor.Right);
                    
                    var particleInstance = Instantiate(collideParticles, collideParticlesContainer.transform, true);
                    particleInstance.transform.position = noteGameObject.transform.position;
                }
            }
        }

        lastHandAnchorPosition = currentHandAnchorPosition;
        lastTipAnchorPosition = currentTipAnchorPosition;
    }

    // private void OnTriggerEnter(Collider other) {
    //     if (other.CompareTag("Note")) {
    //         GameObject noteGameObject = other.gameObject;
    //         NoteInstance noteInstance = mapManager.GetNoteInstanceFromGameObject(noteGameObject);
    //         float currentBeat = mapManager.GetCurrentBeat();
    //         Debug.Log($"Hit. timing: {currentBeat - noteInstance.note.beat} (= {currentBeat} - {noteInstance.note.beat})");
    //         mapManager.DeactivateNoteInstance(noteInstance);

    //         VibrateController(0f, vibrationDuration, isLeftSword ? NoteColor.Left : NoteColor.Right);
    //     }
    // }

    public void VibrateController(float delay, float time, NoteColor side) {
        if (side == NoteColor.Left) {
            Invoke(nameof(StartVibrateLeftController), delay);
            Invoke(nameof(StopVibrateLeftController), delay + time);
        } else if (side == NoteColor.Right) {
            Invoke(nameof(StartVibrateRightController), delay);
            Invoke(nameof(StopVibrateRightController), delay + time);
        }
    }

    private void StartVibrateLeftController() {
        OVRInput.SetControllerVibration(1f, vibrationIntensity, OVRInput.Controller.LTouch);
    }

    private void StartVibrateRightController() {
        OVRInput.SetControllerVibration(1f, vibrationIntensity, OVRInput.Controller.RTouch);
    }

    private void StopVibrateLeftController() {
        OVRInput.SetControllerVibration(0f, 0, OVRInput.Controller.LTouch);
    }

    private void StopVibrateRightController() {
        OVRInput.SetControllerVibration(0f, 0, OVRInput.Controller.RTouch);
    }
}
