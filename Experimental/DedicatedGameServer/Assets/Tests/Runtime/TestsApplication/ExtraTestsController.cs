using Unity.DedicatedGameServerSample.Runtime;

namespace Unity.DedicatedGameServerSample.Tests.Runtime
{
    internal class ExtraTestsController : Controller<TestsApplication>
    {
        void OnDestroy()
        {
            RemoveListeners();
        }
        internal override void RemoveListeners() { }
    }
}
