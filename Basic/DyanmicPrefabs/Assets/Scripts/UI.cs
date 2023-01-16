using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class UI : MonoBehaviour
{
    public string clientNumber;

    public int number;

    public VisualTreeAsset visualTreeAsset;
    
    
    // Update is called once per frame
    void Update()
    {
        if (number > 9)
        {
            number = 0;
        }
        number++;
        clientNumber = number.ToString();
    }
}
