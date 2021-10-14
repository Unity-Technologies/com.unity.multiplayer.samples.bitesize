using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddForce : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [SerializeField]
    private float forceToAdd = 100;

    private void OnTriggerStay(Collider other)
    {
        if (!enabled) return;
        if (other.gameObject.GetComponentInParent<ServerIngredient>() != null)
        {
            other.gameObject.GetComponent<Rigidbody>().AddForce((other.transform.position - transform.position) * forceToAdd);
        }
    }
}
