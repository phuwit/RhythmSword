using UnityEngine;

public class UiInteractionsManager : MonoBehaviour
{
    [SerializeField] private bool uiInteraction = false;
    [SerializeField] private GameObject uiInteractor;
    [SerializeField] private GameObject[] activeWhenUiOff;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetUiInteraction(uiInteraction);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetUiInteraction(bool _uiInteraction) {
        uiInteraction = _uiInteraction;
        if (uiInteraction == true) {
            uiInteractor.SetActive(true);
            foreach(var gameObject in activeWhenUiOff) {
                gameObject.SetActive(false);
            }
        }
        else {
            uiInteractor.SetActive(false);
            foreach(var gameObject in activeWhenUiOff) {
                gameObject.SetActive(true);
            }
        }
    }
}
