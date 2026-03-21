using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnManager : MonoBehaviour
{
    private float startPressed = 0;
    [SerializeField] private float pressForSeconds = 2f;
    [SerializeField] private OVRInput.Button returnButton = OVRInput.Button.Start;
    [SerializeField] private GameObject returnIndicator;
    [SerializeField] private Slider sliderVisual;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.Get(returnButton)) {
            if (OVRInput.GetDown(returnButton)) {
                startPressed = Time.timeSinceLevelLoad;
                returnIndicator.SetActive(true);
                sliderVisual.value = 0;
            }
            var pressedTime = Time.timeSinceLevelLoad - startPressed;
            if (pressedTime >= pressForSeconds) {
                SceneManager.LoadScene("UI", LoadSceneMode.Single);
            }
            sliderVisual.value = pressedTime / pressForSeconds;
        } else {
            returnIndicator.SetActive(false);
        }
    }
}
