using System;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    public class ApplicationController : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
        }
    }
}
