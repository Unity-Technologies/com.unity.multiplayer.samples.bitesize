using System.Collections;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    ///<summary>
    /// A collection of Coroutines Helpers
    ///</summary>
    internal static class CoroutinesHelper
    {
        /*
            usage: yield return CoroutinesHelper.OneSecond; 
            This is better than : yield return new waitforseconds(1);
            because: it doesn't generate garbage.
        */
        public static readonly WaitForSeconds PointZeroOneSeconds = new WaitForSeconds(0.01f);
        public static readonly WaitForSeconds PointZeroFiveSeconds = new WaitForSeconds(0.05f);
        public static readonly WaitForSeconds PointOneSeconds = new WaitForSeconds(0.1f);
        public static readonly WaitForSeconds PointTwoSeconds = new WaitForSeconds(0.2f);
        public static readonly WaitForSeconds PointThreeSeconds = new WaitForSeconds(0.3f);
        public static readonly WaitForSeconds PointFiveSeconds = new WaitForSeconds(0.5f);
        public static readonly WaitForSeconds PointSevenSeconds = new WaitForSeconds(0.7f);
        public static readonly WaitForSeconds PointSevenFiveSeconds = new WaitForSeconds(0.75f);
        public static readonly WaitForSeconds OneSecond = new WaitForSeconds(1);
        public static readonly WaitForSeconds OnePointFiveSeconds = new WaitForSeconds(1.5f);
        public static readonly WaitForSeconds TwoSeconds = new WaitForSeconds(2);
        public static readonly WaitForSeconds ThreeSeconds = new WaitForSeconds(3);
        public static readonly WaitForSeconds FourSeconds = new WaitForSeconds(4);
        public static readonly WaitForSeconds FiveSeconds = new WaitForSeconds(5);
        public static readonly WaitForSeconds EightSeconds = new WaitForSeconds(8);
        public static readonly WaitForSeconds TenSeconds = new WaitForSeconds(10);
        public static readonly WaitForSeconds TwelveSeconds = new WaitForSeconds(12);
        public static readonly WaitForSeconds FifteenSeconds = new WaitForSeconds(15);
        public static readonly WaitForSeconds TwentySeconds = new WaitForSeconds(20);
        public static readonly WaitForSeconds TwentyFiveSeconds = new WaitForSeconds(25);
        static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

        /// <summary>
        /// EndOfFrame does not work in the batchmode editor, so we need 
        /// a workaround: https://forum.unity.com/threads/do-not-use-waitforendofframe.883648/
        /// </summary>
        /// <returns></returns>
        public static IEnumerator WaitAFrame()
        {
#if UNITY_EDITOR
            yield return Application.isBatchMode ? null : EndOfFrame;
#else
            yield return EndOfFrame;
#endif
        }

        public static void StopAndNullifyRoutine(ref Coroutine routine, MonoBehaviour behaviourWhichStartedIt)
        {
            if (routine == null) { return; }
            if (!behaviourWhichStartedIt) { return; }
            behaviourWhichStartedIt.StopCoroutine(routine);
            routine = null;
        }

        public static IEnumerator WaitAndDo(IEnumerator delay, System.Action action)
        {
            yield return delay;
            action?.Invoke();
        }

        public static IEnumerator WaitAndDo(YieldInstruction delay, System.Action action)
        {
            yield return delay;
            action?.Invoke();
        }
    }
}