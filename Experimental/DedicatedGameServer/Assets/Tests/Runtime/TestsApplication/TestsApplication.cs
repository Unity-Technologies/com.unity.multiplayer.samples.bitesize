using Unity.DedicatedGameServerSample.Runtime;

namespace Unity.DedicatedGameServerSample.Tests.Runtime
{
    class TestsApplication : BaseApplication<TestsModel, TestsView, TestsController>
    {
        internal new static TestsApplication Instance { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
