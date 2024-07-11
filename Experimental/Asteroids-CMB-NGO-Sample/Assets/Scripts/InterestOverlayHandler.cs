using System;
using Unity.Netcode;
using UnityEngine;

public class InterestOverlayHandler : MonoBehaviour
{
    public static InterestOverlayHandler Singleton;

    public GameObject InterestTexture;
    public GameObject InterestHighlighter;
    public GameObject InterestCamera;


    private const float k_MinDistance = 200.0f;
    private const float k_MaxDistance = 2000.0f;
    private float m_CurrentZoom = 1000.0f;
    private const float k_ZoomIncrements = 25.0f;
    private Camera m_InterestCamera;
    private int NumberOfStates;

    public enum InterestDisaplayStates
    {
        None,
        Normal,
    }

    public void ChangeInterestZoom(bool zoomIn)
    {
        if (m_InterestCamera != null && CurrentState != InterestDisaplayStates.None)
        {
            var zoomAmount = k_ZoomIncrements * (zoomIn ? -1 : 1);

            m_CurrentZoom = Mathf.Clamp(m_CurrentZoom + zoomAmount, k_MinDistance, k_MaxDistance);
            var position = m_InterestCamera.transform.localPosition;
            position.y = m_CurrentZoom;
            m_InterestCamera.transform.localPosition = position;
        }
    }

    private void Start()
    {
        // Prevent From using the RenderTexture on OSX due to URP bug.
        if (NetworkManagerHelper.Instance.IsRunningOSX)
        {
            if (InterestCamera != null)
            {
                InterestCamera.SetActive(false);
            }

            if (InterestTexture != null)
            {
                InterestTexture.SetActive(false);
            }
            return;
        }

        Singleton = this;
        NumberOfStates = Enum.GetValues(typeof(InterestDisaplayStates)).Length;
        SetState(InterestDisaplayStates.None);
        if (InterestCamera != null)
        {
            m_InterestCamera = InterestCamera.GetComponent<Camera>();
        }
    }

    public InterestDisaplayStates CurrentState { get; private set; }

    private void UpdateCurrentState()
    {
        if (InterestCamera != null)
        {
            InterestCamera.gameObject.SetActive(CurrentState != InterestDisaplayStates.None);
        }

        //if (InterestHighlighter != null)
        //{
        //    InterestHighlighter.SetActive(CurrentState == InterestDisaplayStates.Highlight);
        //}

        if (InterestTexture != null)
        {
            InterestTexture.gameObject.SetActive(CurrentState != InterestDisaplayStates.None);
        }
    }

    public void SetState(InterestDisaplayStates nextState)
    {
        CurrentState = nextState;
        UpdateCurrentState();
    }

    public void SetPlayerColor(Color color)
    {
        if (m_InterestCamera)
        {
            color += Color.white;
            color *= 0.20f;
            m_InterestCamera.backgroundColor = color;
        }
    }

    public void NextState()
    {
        var currentState = (int)CurrentState;
        currentState = ++currentState % NumberOfStates;
        CurrentState = (InterestDisaplayStates)currentState;
        UpdateCurrentState();
    }
}
