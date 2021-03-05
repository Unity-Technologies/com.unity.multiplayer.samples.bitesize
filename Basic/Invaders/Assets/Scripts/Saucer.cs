using System;
using MLAPI;
using UnityEngine;

public class Saucer : MonoBehaviour
{
    private const float k_YBoundary = 14.0f;

    // The constant speed at which the Bullet travels
    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("The constant speed at which the Sauce moves")]
    private float m_MoveSpeed = 3.5f;

    private void Update()
    {
        if (transform.position.x > k_YBoundary)
        {
            if (NetworkManager.Singleton.IsServer) Destroy(gameObject);
            return;
        }

        transform.Translate(m_MoveSpeed * Time.deltaTime, 0, 0);
    }
}
