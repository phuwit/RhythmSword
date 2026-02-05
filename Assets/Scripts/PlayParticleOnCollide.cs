using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class PlayParticleOnCollide : MonoBehaviour
{
    private VisualEffect visualEffect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        visualEffect = GetComponent<VisualEffect>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnCollisionEnter (Collision other) {
        visualEffect.Play();
        Debug.Log("collision enter");
    }

    void OnCollisionExit (Collision other) {
        visualEffect.Stop();
        Debug.Log("colision exit");
    }
}
