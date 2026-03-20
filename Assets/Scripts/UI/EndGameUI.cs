using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour {
    public void GoToMenu() {
        Debug.Log("Go to menu");
        SceneManager.LoadScene("UI", LoadSceneMode.Single);
    }

    public void RestartGame() {
        Debug.Log("Restart Game");
        SceneManager.LoadScene("MainGame");
    }
}