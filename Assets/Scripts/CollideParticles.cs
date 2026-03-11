using UnityEngine;
using System.Collections;

public class CollideParticles : MonoBehaviour
{
    [SerializeField] private float destroyAfterSeconds = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        StartCoroutine(DestroyAfterSeconds());
    }

    private IEnumerator DestroyAfterSeconds() {
        yield return new WaitForSeconds(destroyAfterSeconds);
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
