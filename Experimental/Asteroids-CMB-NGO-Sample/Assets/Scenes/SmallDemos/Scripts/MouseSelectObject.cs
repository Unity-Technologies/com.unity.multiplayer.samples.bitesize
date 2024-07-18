using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class MouseSelectObject
{
    public static bool SelectPoint<T>(out Vector3 position, TagHandle handle) where T : Component
    {
        return SelectPoint<T>(out  position, handle, Input.mousePosition);
    }

    public static bool SelectPoint<T>(out Vector3 position, TagHandle handle, Vector3 mousePosition) where T : Component
    {
        var screenRay = Camera.main.ScreenPointToRay(mousePosition);
        position = Vector3.zero;
        //Grab the information we need
        var hitInfo = Physics.RaycastAll(screenRay).ToList();
        if (hitInfo == null || hitInfo.Count == 0)
        {
            return false;
        }

        hitInfo.Sort(SortByClosestToOrigin);
        foreach (var hit in hitInfo)
        {
            if (!hit.collider.CompareTag(handle))
            {
                continue;
            }
            var objectTypeToFind = hit.collider.GetComponent<T>();
            if (objectTypeToFind == null)
            {
                if (hit.transform.parent != null)
                {
                    objectTypeToFind = hit.transform.parent.GetComponent<T>();
                }

                if (objectTypeToFind == null)
                {
                    continue;
                }
            }

            if (objectTypeToFind != null)
            {
                position = hit.point;
                return true;
            }
        }

        return false;
    }

    public static T SelectObject<T>() where T : Component
    {
        if (Camera.current == null)
        {
            Camera.SetupCurrent(Camera.allCameras[0]);
        }

        var screenRay = Camera.current.ScreenPointToRay(Input.mousePosition);

        //Grab the information we need
        var hitInfo = Physics.RaycastAll(screenRay).ToList();
        if (hitInfo == null || hitInfo.Count == 0)
        {
            return null;
        }

        hitInfo.Sort(SortByClosestToOrigin);

        foreach (var hit in hitInfo)
        {
            var objectTypeToFind = hit.collider.GetComponent<T>();
            if (objectTypeToFind == null)
            {
                if (hit.transform.parent != null)
                {
                    objectTypeToFind = hit.transform.parent.GetComponent<T>();
                }
                
                if (objectTypeToFind == null)
                {
                    continue;
                }
            }

            if (objectTypeToFind != null)
            {
                return objectTypeToFind;
            }
        }
        return null;
    }

    private static int SortByClosestToOrigin(RaycastHit first, RaycastHit second)
    {
        if (first.distance < second.distance)
        {
            return -1;
        }

        if (first.distance > second.distance)
        {
            return 1;
        }

        return 0;
    }
}
