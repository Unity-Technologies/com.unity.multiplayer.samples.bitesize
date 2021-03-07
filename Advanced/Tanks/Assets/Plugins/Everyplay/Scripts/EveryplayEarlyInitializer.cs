using UnityEngine;
using System.Collections;

public class EveryplayEarlyInitializer : MonoBehaviour
{
    #if (UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
    void Start()
    {
        EveryplaySettings settings = (EveryplaySettings) Resources.Load("EveryplaySettings");

        if (settings != null)
        {
            if (settings.earlyInitializerEnabled && settings.IsEnabled && settings.IsValid)
            {
                StartCoroutine(InitializeEveryplay());
            }
        }
    }

    IEnumerator InitializeEveryplay()
    {
        yield return 0;
        Everyplay.Initialize();
        Destroy(gameObject);
    }

    #else
    [RuntimeInitializeOnLoadMethod]
    static void InitializeEveryplayOnStartup()
    {
        EveryplaySettings settings = (EveryplaySettings) Resources.Load("EveryplaySettings");

        if (settings != null)
        {
            if (settings.earlyInitializerEnabled && settings.IsEnabled && settings.IsValid)
            {
                Everyplay.Initialize();
            }
        }
    }

    #endif
}
