using UnityEngine;

public class AddForce : MonoBehaviour
{
    [SerializeField]
    private float m_ForceToAdd = 100;

    private void OnTriggerStay(Collider other)
    {
        if (!enabled) return;
        if (other.gameObject.GetComponentInParent<ServerIngredient>() != null)
        {
            other.gameObject.GetComponent<Rigidbody>().AddForce((other.transform.position - transform.position) * m_ForceToAdd);
        }
    }
}
