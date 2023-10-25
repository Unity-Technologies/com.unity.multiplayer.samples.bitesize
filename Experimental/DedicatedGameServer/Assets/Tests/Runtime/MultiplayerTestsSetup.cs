using NUnit.Framework;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using UnityEditor;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Tests.Runtime
{
    /// <summary>
    /// Those tests are run ONCE before/after other tests of this assembly
    /// https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html
    /// </summary>
    [SetUpFixture]
    public class MultiplayerTestsSetup
    {
        public MultiplayerTestsSetup() { }

        [SerializeField]
        int m_TargetFrameRateBeforeTests;

        [SerializeField]
        int m_VSyncCountBeforeTests;

        [SerializeField]
        int m_CaptureFramerateBeforeTests;

        [SerializeField]
        StackTraceLogType m_LogStackTraceTypeBeforeTests;
        [SerializeField]
        StackTraceLogType m_WarningStackTraceTypeBeforeTests;
        [SerializeField]
        StackTraceLogType m_AssertionStackTraceTypeBeforeTests;

        [OneTimeSetUp]
        public void RunBeforeAnyTest()
        {
            ApplyTestingSpecificSettings();
            ApplicationEntryPoint.s_AreTestsRunning = true;
        }

        void ApplyTestingSpecificSettings()
        {
            m_TargetFrameRateBeforeTests = Application.targetFrameRate;
            m_VSyncCountBeforeTests = QualitySettings.vSyncCount;
            m_CaptureFramerateBeforeTests = Time.captureFramerate;
            Application.targetFrameRate = -1; // No maximum frame rate to sleep on
            QualitySettings.vSyncCount = 0; // No GPU vSync
            Time.captureFramerate = 40; // Simulate FPS, actually the lower the faster... 10 is too fast and leads to issues.

            m_LogStackTraceTypeBeforeTests = PlayerSettings.GetStackTraceLogType(LogType.Log);
            m_WarningStackTraceTypeBeforeTests = PlayerSettings.GetStackTraceLogType(LogType.Warning);
            m_AssertionStackTraceTypeBeforeTests = PlayerSettings.GetStackTraceLogType(LogType.Assert);

            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
        }

        void RevertTestingSpecificSettings()
        {
            Application.targetFrameRate = m_TargetFrameRateBeforeTests;
            QualitySettings.vSyncCount = m_VSyncCountBeforeTests;
            Time.captureFramerate = m_CaptureFramerateBeforeTests;
            PlayerSettings.SetStackTraceLogType(LogType.Log, m_LogStackTraceTypeBeforeTests);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, m_WarningStackTraceTypeBeforeTests);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, m_AssertionStackTraceTypeBeforeTests);
        }

        [OneTimeTearDown]
        public void RunAfterAllFixtureTests()
        {
            ApplicationEntryPoint.s_AreTestsRunning = false;
            RevertTestingSpecificSettings();
        }
    }
}
