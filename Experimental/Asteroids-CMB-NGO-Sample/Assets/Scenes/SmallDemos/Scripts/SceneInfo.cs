using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInfo : MonoBehaviour
{
    private TextMesh m_TextMesh;
    private void Start()
    {
        m_TextMesh = GetComponent<TextMesh>();
    }

    private void OnGUI()
    {
        m_TextMesh.text = gameObject.scene.name;
    }

    private void Update()
    {
        if (Camera.main == null) 
        {
            return;
        }
        var angles = transform.eulerAngles;
        angles.y = Camera.main.transform.eulerAngles.y;
        transform.eulerAngles = angles;
    }
}
