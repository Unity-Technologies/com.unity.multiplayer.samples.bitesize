using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnalyticsHeatmap
{
    public class BasicDataStory : DataStory
    {
        public BasicDataStory()
        {
            name = "Basic Functionality";
            genre = "Any";
            description = "This first demo shows off a few key ideas, such as particle shapes, sizes, and colors.";
            whatToTry = "Generate this data, then open the Heatmapper and click the Process button. ";
            whatToTry += "First, notice the numbers at the bottom of the Heatmapper ('Points displayed/total') ";
            whatToTry += "These give you an idea of how much data you should expect to see displayed.\n\n";
            whatToTry += "Notice that the generated heatmap has three colors. Click on the color gradient to 'tune' the ";
            whatToTry += "colors. Play with the gradient and see how that changes the look of the heatmap. You can even change the alphas.\n\n";
            whatToTry += "Now, under 'Particle', change the size and shape settings and again observe how this affects the display.\n\n";
            whatToTry += "Finally, check the box 'Hot tips' at the bottom of the Heatmapper. In the scene, click on any heatmap point. ";
            whatToTry += "If you roll over that point or any other, you'll now see a tooltip with the data that the point represents. Note that ";
            whatToTry += "hot tips cost a lot in terms of performance, so uncheck the box except when you need to see the data!";
            sampleCode = "using UnityAnalyticsHeatmap;\n\n";
            sampleCode += "HeatmapEvent.Send(\"ShotWeapon\",transform.position);";
        }

        #region implemented abstract members of DataStory
        public override Dictionary<double, string> Generate()
        {
            SetRandomSeed();
            List<string> eventNames = new List<string>(){"Heatmap.ShotWeapon"};

            List<TestCustomEvent> events = new List<TestCustomEvent>();
            for (int a = 0; a < eventNames.Count; a++)
            {
                TestCustomEvent customEvent = new TestCustomEvent();
                customEvent.name = eventNames[UnityEngine.Random.Range(0, eventNames.Count)];
                var x = new TestEventParam("x", TestEventParam.Num, 0, 0);
                customEvent.Add(x);
                var y = new TestEventParam("y", TestEventParam.Num, 0, 0);
                customEvent.Add(y);
                var z = new TestEventParam("z", TestEventParam.Num, 0, 0);
                customEvent.Add(z);
                events.Add(customEvent);
            }

            var retv = new Dictionary<double, string>();

            string data = "";
            int fileCount = 0;
            int eventCount = 500;
            int deviceCount = 5;
            int sessionCount = 1;

            // Custom to this lesson
            float minRadius = 5f;
            float radius = 10f;

            DateTime now = DateTime.UtcNow;
            int totalSeconds = deviceCount * eventCount * sessionCount;
            double endSeconds = Math.Round((now - UnityAnalytics.DateTimeUtils.s_Epoch).TotalSeconds);
            double startSeconds = endSeconds - totalSeconds;
            double currentSeconds = startSeconds;
            double firstDate = currentSeconds;

            for (int a = 0; a < deviceCount; a++)
            {
                string platform = "ios";
                for (int b = 0; b < sessionCount; b++)
                {
                    for (int c = 0; c < eventCount; c++)
                    {
                        currentSeconds ++;
                        TestCustomEvent customEvent = events[UnityEngine.Random.Range(0, events.Count)];
                        customEvent.SetParam("t", currentSeconds - startSeconds);

                        float mult = UnityEngine.Random.Range(0f, 1f) > .5f ? -1f : 1f;
                        float distance = UnityEngine.Random.Range(minRadius, radius) * mult;
                        Vector3 rot = new Vector3(UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(-1f,1f));
                        Vector3 position = rot.normalized * distance;
                        customEvent.SetParam("x", position.x, position.x);
                        customEvent.SetParam("y", position.y, position.y);
                        customEvent.SetParam("z", position.z, position.z);

                        string evt = customEvent.WriteEvent(a, b, currentSeconds, platform);
                        data += evt;

                        if (a == deviceCount-1 && b == sessionCount-1 && c == eventCount-1) {
                            retv.Add(firstDate, data);
                            fileCount++;
                        }
                    }
                }
            }
            return retv;
        }
        #endregion
    }
}
