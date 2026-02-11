using UnityEngine;
using UnityEngine.VFX;

public class RayInteractor : MonoBehaviour
{
    // [SerializeField]
    // private GameObject Stick;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private GameObject particlesContainer;

    [SerializeField]
    private GameObject particles;
    
    private GameObject lastCollided = null;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new(transform.position, transform.forward);

        // Stick.transform.position = ray.origin;
        Debug.DrawRay(ray.origin, ray.direction, Color.coral);

        if (Physics.Raycast(ray, out RaycastHit hitData, 2f, LayerMask.GetMask("Beats")))
        {
            GameObject hitObject = hitData.transform.gameObject;
            if (lastCollided == hitObject)
            {
                return;
            } 

            var renderer = hitObject.GetComponent<MeshRenderer>();

            renderer.material.color = Random.ColorHSV();

            var particleInstance = Instantiate(particles, particlesContainer.transform, true);
            particleInstance.transform.position = hitObject.transform.position;

            lastCollided = hitObject;
        }
    }
}
