using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool SharedInstance;
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public int amountToPool;
    public int whereListAt;

    void Awake()
    {
        SharedInstance = this;
    }

    void Start()
    {
        whereListAt = 0;
        pooledObjects = new List<GameObject>();
        GameObject tmp;
        for(int i = 0; i < amountToPool; i++)
        {
            tmp = Instantiate(objectToPool);
            tmp.SetActive(false);
            pooledObjects.Add(tmp);
        }
    }

    public GameObject GetPooledObject()
    {
        int i = whereListAt;

        do
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                whereListAt = (i + 1) % amountToPool;
                return pooledObjects[i];
            }

            i = (i + 1) % amountToPool;
        } while (i == whereListAt);
        // executes when do method went through all of pooledObjects in one call
        return null;
        }
}
