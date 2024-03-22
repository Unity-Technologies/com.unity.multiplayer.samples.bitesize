using NUnit.Framework;
using System.Collections;
using Unity.DedicatedGameServerSample.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.DedicatedGameServerSample.Tests.Runtime
{
    [TestFixture, TestFixtureSource(typeof(TestsApplicationProvider))]
    class CoreSystemTests
    {
        class TestEvent : AppEvent { }

        [SerializeField]
        TestsApplication m_TestsApplicationPrefab;
        TestsApplication m_TestsApplication;
        public CoreSystemTests(TestsApplication testsApplicationPrefab)
        {
            m_TestsApplicationPrefab = testsApplicationPrefab;
        }

        [UnitySetUp]
        public IEnumerator SetupEnvironment()
        {
            Debug.Log($"Starting test: {TestContext.CurrentContext.Test.Name}");
            m_TestsApplication = GameObject.Instantiate<TestsApplication>(m_TestsApplicationPrefab);
            while (!TestsApplication.Instance)
            {
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDownEnvironment()
        {
            GameObject.DestroyImmediate(m_TestsApplication.gameObject);
            while (TestsApplication.Instance)
            {
                yield return null;
            }
        }

        [Test]
        public void Controller_ListeningToEvent_ReactsWhenEventTriggers()
        {
            TestsApplication.Instance.Controller.AddListener<TestEvent>((evt) => Assert.Pass());
            TestsApplication.Instance.Broadcast(new TestEvent());
            Assert.Fail("Triggered event was not detected by the Application");
        }

        [Test]
        public void Controller_EventTriggers_DoesntReactWhenNotListening()
        {
            void OnTestEvent(TestEvent evt)
            {
                Assert.Fail("Triggered event was detected even after removing the listener");
            }

            TestsApplication.Instance.Controller.AddListener<TestEvent>(OnTestEvent);
            TestsApplication.Instance.Controller.RemoveListener<TestEvent>(OnTestEvent);
            TestsApplication.Instance.Broadcast(new TestEvent());
            Assert.Pass();
        }

        [Test]
        public void Application_Initializes_SubComponentsAreAssignedProperly()
        {
            Assert.IsNotNull(TestsApplication.Instance);
            Assert.IsNotNull(TestsApplication.Instance.Model);
            Assert.IsNotNull(TestsApplication.Instance.View);
            Assert.IsNotNull(TestsApplication.Instance.Controller);
        }

        [Test]
        public void Application_EventBroadcasted_ReachesAllListeningControllers()
        {
            /* Note: Controllers are smart enough to ignore duplicated calls to the same method, 
             * so the signatures of methods need to be different for them to all be called. */
            
            int activations = 0;
            void OnTestEvent1(TestEvent evt)
            {
                activations++;
            }

            void OnTestEvent2(TestEvent evt)
            {
                OnTestEvent1(evt);
            }

            var extraController = TestsApplication.Instance.Controller.gameObject.AddComponent<ExtraTestsController>();
            extraController.AddListener<TestEvent>(OnTestEvent1);
            TestsApplication.Instance.Controller.AddListener<TestEvent>(OnTestEvent2);
            TestsApplication.Instance.Broadcast(new TestEvent());
            Assert.AreEqual(2, activations, $"Only {activations} controller(s) reacted to the event");
        }

        [Test]
        public void Application_EventBroadcastedToMultipleControllers_DuplicatedMethodIsCalledOnce()
        {
            int activations = 0;
            void OnTestEvent(TestEvent evt)
            {
                activations++;
            }
            var extraController = TestsApplication.Instance.Controller.gameObject.AddComponent<ExtraTestsController>();
            extraController.AddListener<TestEvent>(OnTestEvent);
            TestsApplication.Instance.Controller.AddListener<TestEvent>(OnTestEvent);
            TestsApplication.Instance.Broadcast(new TestEvent());
            Assert.AreEqual(1, activations, $"Duplicated method was called {activations} times instead of just 1");
        }
    }
}
