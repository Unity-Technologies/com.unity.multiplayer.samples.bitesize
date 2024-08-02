using System.Threading.Tasks;
using Services;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private float speed = 4.0f;

    void Update()
    {
        if (IsOwner)
        {
            if (Input.GetKey(KeyCode.W)){
                transform.position += Vector3.forward * speed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.S))
            {
                transform.position += Vector3.back * speed * Time.deltaTime;
            }
        }
    }
}
