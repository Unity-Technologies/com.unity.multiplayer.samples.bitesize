using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;

public class StressTestState : NetworkBehaviour
{
    public static StressTestState Instance;
    public Button ForceMode;

    private Text m_ButtonLabel;

    private NetworkVariable<bool> m_ForceModeToggle = new NetworkVariable<bool>();

    public bool ForceModeOwnersOnly()
    {
        return m_ForceModeToggle.Value;
    }

    private void Awake()
    {
        Instance = this;
        if (ForceMode)
        {
            m_ButtonLabel = ForceMode.GetComponentInChildren<Text>();
            ForceMode.gameObject.SetActive(false);
            ForceMode.onClick.AddListener(new UnityAction(ChangeForceMode));
        }
    }

    private void ChangeForceMode()
    {
        m_ForceModeToggle.Value = !m_ForceModeToggle.Value;
        if (!m_ButtonLabel)
        {
            return;
        }
        if (m_ForceModeToggle.Value) 
        {
            m_ButtonLabel.text = "Owned Objects";
        }
        else
        {
            m_ButtonLabel.text = "All Objects";
        }
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.LocalClient.IsSessionOwner)
        {
            ForceMode.gameObject.SetActive(true);
        }

        NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        base.OnNetworkSpawn();
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (sessionOwnerPromoted == NetworkManager.LocalClientId)
        {
            ForceMode.gameObject.SetActive(true);
            if (!HasAuthority)
            {
                NetworkObject.ChangeOwnership(sessionOwnerPromoted);
            }
        }
        else
        {
            ForceMode.gameObject.SetActive(false);
        }
    }
}
