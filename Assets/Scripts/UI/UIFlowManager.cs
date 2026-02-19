using UnityEngine;
using UnityEngine.InputSystem;

public class UIFlowManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject songSelectPanel;

    [Header("Trigger Actions")]
    public InputActionReference leftTrigger;
    public InputActionReference rightTrigger;

    private bool hasStarted = false;

    void Awake()
    {
        // บังคับสถานะเริ่มต้นให้ถูกต้องเสมอ
        startPanel.SetActive(true);
        songSelectPanel.SetActive(false);
        hasStarted = false;
    }

    void OnEnable()
    {
        if (leftTrigger != null)
            leftTrigger.action.Enable();

        if (rightTrigger != null)
            rightTrigger.action.Enable();
    }

    void OnDisable()
    {
        if (leftTrigger != null)
            leftTrigger.action.Disable();

        if (rightTrigger != null)
            rightTrigger.action.Disable();
    }

    void Update()
    {
        if (hasStarted) return;

        bool leftPressed = leftTrigger != null && leftTrigger.action.triggered;
        bool rightPressed = rightTrigger != null && rightTrigger.action.triggered;

        if (leftPressed || rightPressed)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        startPanel.SetActive(false);
        songSelectPanel.SetActive(true);
        hasStarted = true;
    }
}
