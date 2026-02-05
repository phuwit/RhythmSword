using UnityEngine;

public class RayColorizer : MonoBehaviour
{
    // [SerializeField]
    // private GameObject Stick;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new(transform.position, transform.forward);

        // Stick.transform.position = ray.origin;
        Debug.DrawRay(ray.origin, ray.direction, Color.coral);

        if (Physics.Raycast(ray, out RaycastHit hitData, 200f, LayerMask.GetMask("Beats")))
        {
            GameObject hitObject = hitData.transform.gameObject;
            var renderer = hitObject.GetComponent<MeshRenderer>();
            renderer.material.color = Random.ColorHSV();
        }
    }
}
