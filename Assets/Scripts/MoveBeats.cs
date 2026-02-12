using UnityEngine;

public class MoveBeats : MonoBehaviour
{
    [SerializeField]
    private float speed = 0.3f;

    private Vector3 defaultPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        defaultPosition = transform.position;
    }

    // Update is called once per frame
    void Update() {
        if(transform.position.x >= 1)
        {
            transform.position = defaultPosition;
            return;
        }
        
        transform.Translate(speed * Time.deltaTime * transform.right);
    }
}
