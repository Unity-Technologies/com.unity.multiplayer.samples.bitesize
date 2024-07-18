using UnityEngine;

public class TrailRendererClear : MonoBehaviour
{
    private TrailRenderer _trailRenderer;

    private void OnEnable()
    {
        if (_trailRenderer == null)
            TryGetComponent(out _trailRenderer);
        
        if(_trailRenderer)
            _trailRenderer.Clear();
    }
}
