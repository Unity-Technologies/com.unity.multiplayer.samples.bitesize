using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamically generates player ship spawn points
/// </summary>
public class PlayerSpawnPoints : MonoBehaviour
{
    [Tooltip("Determines how many outer rings from the center of the world space will be used to generate spawn points.")]
    [Range(1, 8)]
    public int Layers = 4;

    [Tooltip("Determines the radius distance from world space center to the outer most ring containing player spawn points.")]
    public float Radius = 100;

    [Tooltip("Determines how many spawn points will be generated in the outer most ring. The number of spawn points per ring reduces the closer to the most inner ring.")]
    public int Divisions = 45;

    [Tooltip("Defines the layer mask to use when checking if a spawn point is occupied by an already spawned object.")]
    [SerializeField]
    private LayerMask m_LayerMasks;

    [Tooltip("The calculated total bounds of the ship requesting a spawn point.")]
    private Bounds m_PlayerTotalBounds;

    /// <summary>
    /// The dynamically generated table of spawn points
    /// </summary>
    private Dictionary<int, List<Vector3>> m_SpawnPoints = new Dictionary<int, List<Vector3>>();


    /// <summary>
    /// Generates the table of spawn points based on this component's configuration
    /// </summary>
    private void Start()
    {
        ShipController.PlayerSpawnPoints = this;

        // Generate spawn points
        var layerDistance = Radius / Layers;
        var currentRotation = Quaternion.identity;
        var eulerRotation = Vector3.zero;
        for (int i = 0; i < Layers; i++)
        {
            m_SpawnPoints.Add(i, new List<Vector3>());
            for (int j = 0; j < 360; j += Divisions)
            {
                eulerRotation.y = j;
                currentRotation.eulerAngles = eulerRotation;
                var direction = currentRotation * Vector3.forward;
                var distance = layerDistance * (i + 1);
                var point = (direction * distance) + new Vector3(0.0f, 0.5f, 0.0f);
                m_SpawnPoints[i].Add(direction * distance);
            }
        }
    }

    /// <summary>
    /// Generates a bounds region that defines the collision bounds of the GameObject passed in
    /// </summary>
    private void SetPlayerBounds(ref GameObject playerShip)
    {
        var colliders = playerShip.GetComponentsInChildren<Collider>();
        m_PlayerTotalBounds = new Bounds(colliders[0].bounds.center, colliders[0].bounds.size);
        foreach (var collider in colliders)
        {
            m_PlayerTotalBounds.Encapsulate(collider.bounds);
        }
    }

    /// <summary>
    /// Gets a spawn point in the world space not occupied by a spawned object
    /// </summary>
    /// <param name="playerShip">the ship to be spawned</param>
    /// <returns></returns>
    public Vector3 GetSpawnPoint(GameObject playerShip)
    {
        if (playerShip == null)
        {
            return Vector3.zero;
        }

        // Get the bounds of the player ship
        SetPlayerBounds(ref playerShip);

        // Find a point not already occupied by a spawned object 
        var mask = m_LayerMasks.value >> 1;
        for (int i = 0; i < Layers; i++)
        {
            var spawnPoints = m_SpawnPoints[i];
            foreach (var point in spawnPoints)
            {
                
                var colliders = Physics.OverlapBox(point, m_PlayerTotalBounds.extents, Quaternion.identity, mask, QueryTriggerInteraction.Collide);
                if (colliders.Length == 0 && !Physics.CheckBox(point, m_PlayerTotalBounds.extents, Quaternion.identity, mask, QueryTriggerInteraction.Collide))
                { 
                    return point;
                }
            }
        }

        Debug.LogWarning($"Found no viable spawn locations!");
        // Otherwise we found nothing and return the center of the world space
        return Vector3.zero;
    }
}
