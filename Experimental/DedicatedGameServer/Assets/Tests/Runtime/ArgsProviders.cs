using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Tests.Runtime
{
    class BaseProvider<T> : IEnumerable<T> where T : MonoBehaviour
    {
        string m_Path;
        public BaseProvider(string path)
        {
            m_Path = path;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { m_Path }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
                if (root.GetComponent<T>())
                {
                    yield return root.GetComponent<T>();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
    }

    class TestsApplicationProvider : BaseProvider<TestsApplication>
    {
        public TestsApplicationProvider() : base("Assets/Tests/Runtime/TestsApplication") { }
    }
}
