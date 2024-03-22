using Unity.DedicatedGameServerSample.Runtime;

namespace Unity.DedicatedGameServerSample.Tests.Runtime
{
    class TestsController : Controller<TestsApplication>
    {
        void OnDestroy()
        {
            RemoveListeners();
        }
        internal override void RemoveListeners() { }
    }
}
