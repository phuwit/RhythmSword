using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour {
    public void GoToMenu() {
        SceneManager.LoadScene("UI", LoadSceneMode.Single);
    }

    public void RestartGame() {
        SceneManager.LoadScene("MainGame");
    }
}