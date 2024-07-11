using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CubeController : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (!HasAuthority)
        {
            return;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(1 * Time.deltaTime, 0, 0);
        }
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(-1 * Time.deltaTime, 0, 0);
        }
        
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(0, 1 * Time.deltaTime, 0);
        }
        
        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(0, -1 * Time.deltaTime, 0);
        }
    }
}
