using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyButtonUI : MonoBehaviour
{
    public TextMeshProUGUI label;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.magenta;

    private Button button;
    private DifficultyManager manager;
    private string difficultyName;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    public void Setup(DifficultyManager mgr, string diffName)
    {
        manager = mgr;
        difficultyName = diffName;
    }

    void OnClicked()
    {
        manager.Select(this);
    }

    public void SetSelected(bool value)
    {
        label.color = value ? selectedColor : normalColor;
    }
}
