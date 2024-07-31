using Services;
using Unity.Services.Vivox;
using UnityEngine;

public class Player : MonoBehaviour
{
    private float speed = 4.0f;
    private float _nextPosUpdate;

    private string channelName;

    void Start()
    {
        channelName = VivoxManager.Instance.SessionName;
        _nextPosUpdate = Time.time;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W)){
            transform.position += Vector3.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S)){
            transform.position += Vector3.back* speed * Time.deltaTime;
        }

        if (Time.time > _nextPosUpdate)
        {
            VivoxService.Instance.Set3DPosition(gameObject, channelName);
            _nextPosUpdate += 0.3f;
        }
    }
}
