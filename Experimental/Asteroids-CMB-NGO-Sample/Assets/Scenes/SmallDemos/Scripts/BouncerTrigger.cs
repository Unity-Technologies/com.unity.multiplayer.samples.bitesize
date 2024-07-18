using System;
using UnityEngine;

public class BouncerTrigger : MonoBehaviour
{
    [Range(0.1f, 5.0f)]
    public float Multiplier = 1.5f;
    private TagHandle m_TagHandle;

    private void Awake()
    {
        m_TagHandle = TagHandle.GetExistingTag("Boundary");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(m_TagHandle))
        {
            return;
        }

        var colliderRigidbody = other.gameObject.GetComponent<Rigidbody>();
        if (colliderRigidbody == null || colliderRigidbody && colliderRigidbody.isKinematic) 
        {
            return;
        }

        colliderRigidbody.linearVelocity += Multiplier * transform.forward;
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(m_TagHandle))
        {
            return;
        }

        var colliderRigidbody = other.gameObject.GetComponent<Rigidbody>();
        if (colliderRigidbody == null || colliderRigidbody && colliderRigidbody.isKinematic)
        {
            return;
        }
        colliderRigidbody.linearVelocity += Multiplier * transform.forward;
    }
}
