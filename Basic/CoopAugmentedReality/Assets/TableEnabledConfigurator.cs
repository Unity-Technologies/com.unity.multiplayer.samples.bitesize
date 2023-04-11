using UnityEngine;

public class TableEnabledConfigurator : MonoBehaviour
{
	[SerializeField] private MeshRenderer tableMesh;
    void OnEnable()
    {
		tableMesh.enabled = true;
	}

	void OnDisable()
	{
		tableMesh.enabled = false;
	}
}
