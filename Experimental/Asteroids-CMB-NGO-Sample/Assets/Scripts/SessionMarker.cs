using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class SessionMarker : NetworkBehaviour
{
    public TextMesh SessionInfoTag;
    public TextMesh MarkerPosition;


    private NetworkVariable<bool> m_IsMarkerPlanted = new NetworkVariable<bool>();
    
    private NetworkVariable<Color> m_PlayerColorThatMarked = new NetworkVariable<Color>();
    private NetworkVariable<FixedString128Bytes> m_SessionName = new NetworkVariable<FixedString128Bytes>();

    private MeshRenderer m_MeshRenderer;

    private void Awake()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        SetMarkerColorAndInfo();

        base.OnNetworkSpawn();
    }

    private void SetMarkerColorAndInfo()
    {
        if (!m_MeshRenderer)
        {
            return;
        }

        if (!m_IsMarkerPlanted.Value && HasAuthority)
        { 
            var color = PlayerColor.GetPlayerColor(OwnerClientId);
            m_PlayerColorThatMarked.Value = color;
            m_IsMarkerPlanted.Value = true;
            m_SessionName.Value = $"[{NetworkManagerHelper.Instance.GetSessionName()}] Player-{OwnerClientId} Marker";
        }

        var currentColor = m_MeshRenderer.material.color;
        currentColor.r = m_PlayerColorThatMarked.Value.r;
        currentColor.g = m_PlayerColorThatMarked.Value.g;
        currentColor.b = m_PlayerColorThatMarked.Value.b;
        m_MeshRenderer.material.color = currentColor;

        if (SessionInfoTag != null)
        {
            SessionInfoTag.text = m_SessionName.Value.ToString();
        }

        if (MarkerPosition != null)
        {
            MarkerPosition.text = GetVector3Values(transform.position);
        }
    }
    protected string GetVector3Values(Vector3 vector3)
    {
        return $"({vector3.x:F2},{vector3.y:F2},{vector3.z:F2})";
    }
}
