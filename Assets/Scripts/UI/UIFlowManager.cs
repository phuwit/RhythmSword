using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIFlowManager : MonoBehaviour {
    public GameObject startPanel;
    public GameObject songSelectPanel;

    public InputActionReference leftTrigger;
    public InputActionReference rightTrigger;

    bool hasStarted = false;

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() {
        leftTrigger.action.Enable();
        rightTrigger.action.Enable();

        leftTrigger.action.performed += OnTriggerPressed;
        rightTrigger.action.performed += OnTriggerPressed;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() {
        leftTrigger.action.performed -= OnTriggerPressed;
        rightTrigger.action.performed -= OnTriggerPressed;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        startPanel = GameObject.Find("StartPanel");
        songSelectPanel = GameObject.Find("SongSelectPanel");

        if (startPanel != null)
            startPanel.SetActive(true);

        if (songSelectPanel != null)
            songSelectPanel.SetActive(false);

        hasStarted = false;
    }

    void OnTriggerPressed(InputAction.CallbackContext ctx) {
        if (hasStarted) return;

        StartGame();
    }

    void StartGame() {
        startPanel.SetActive(false);
        songSelectPanel.SetActive(true);

        hasStarted = true;
    }
}