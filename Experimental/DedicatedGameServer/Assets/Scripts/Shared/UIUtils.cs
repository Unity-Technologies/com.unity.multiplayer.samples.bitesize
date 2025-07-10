using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Shared
{
    public static class UIUtils
    {
        // Just a helper extension method to get the first child of a VisualElement without using LINQ.
        public static VisualElement GetFirstChild(this VisualElement element)
        {
            foreach (var item in element.Children())
            {
                return item;
            }
            return null;
        }
    }
}
