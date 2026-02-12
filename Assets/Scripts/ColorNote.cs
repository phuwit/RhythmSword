using UnityEngine;

public enum NoteLineIndex
{
    LeftMost,
    CenterLeft,
    CenterRight,
    RightMost,
}

public enum NoteLineLayer
{
    Bottom,
    Center,
    Top,
}

public enum NoteColor
{
    Left,
    Right,
}

public enum NoteCutDirection 
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight,
    Any,
}

public class ColorNote : MonoBehaviour
{
    [SerializeField]
    private float beat;
    [SerializeField]
    private NoteLineIndex lineIndex;
    [SerializeField]
    private NoteLineLayer lineLayer;
    [SerializeField]
    private NoteColor color;
    [SerializeField]
    private NoteCutDirection cutDirection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
