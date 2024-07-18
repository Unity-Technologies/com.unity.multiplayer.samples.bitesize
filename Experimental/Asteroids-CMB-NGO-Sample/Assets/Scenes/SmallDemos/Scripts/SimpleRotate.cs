using UnityEngine;

public class SimpleRotate : MonoBehaviour
{
    [Range(-60f, 60)]
    public float RotateRate = 10.0f;

    [Range(-60f, 60)]
    public float XAxisRate = 0.0f;

    [Range(-60f, 60)]
    public float ZAxisRate = 0.0f;

    private Vector3 m_Euler = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        m_Euler = transform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        m_Euler.x = Mathf.LerpAngle(m_Euler.x, m_Euler.x + XAxisRate, Time.deltaTime);
        m_Euler.y = Mathf.LerpAngle(m_Euler.y, m_Euler.y + RotateRate, Time.deltaTime);
        m_Euler.z = Mathf.LerpAngle(m_Euler.z, m_Euler.z + ZAxisRate, Time.deltaTime);
        transform.localEulerAngles = m_Euler;
    }
}
