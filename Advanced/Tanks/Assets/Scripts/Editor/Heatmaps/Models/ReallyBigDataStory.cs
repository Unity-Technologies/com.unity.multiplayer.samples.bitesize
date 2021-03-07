using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnalyticsHeatmap
{
    public class ReallyBigDataStory : DataStory
    {
        public ReallyBigDataStory()
        {
            name = "Really Big Game";
            genre = "3D Flight Combat Sim";
            description = "This demonstrates some important ideas about scale, direction, and time.";
            whatToTry = "Generate this data. Open the Heatmapper and click the Process button, which first shows you combat kills. ";
            whatToTry += "Or does it? You might not see much right away for two reasons. First this demo shows data in a very large ";
            whatToTry += "area and you're probably zoomed in. Second, check the Particle size. Try setting size to around 25, ";
            whatToTry += "then zoom out so you can see all the points. ";
            whatToTry += "Notice that this data is a bit sparse because of the scale of the map. ";
            whatToTry += "Under 'Aggregate', change the value of 'Space Smooth' to 500 and re-process. ";
            whatToTry += "Adjust the particle size to 250. Now you can see the general areas where ";
            whatToTry += "kills have occurred and the map becomes more useful.\n\n";

            whatToTry += "Under Render, Find the 'Option' dropdown. If you click it, you'll see in addition to 'CombatKills' ";
            whatToTry += "there's an option for 'PlayerPosition'. Choose that and instead of seeing kills, you'll see where in this sim ";
            whatToTry += "your players have gone. While space smooth of 500 was good for kills, it's likely too coarse for player position. ";
            whatToTry += "Re-adjust space smoothing and particle size to 10. ";
            whatToTry += "Uncheck the 'Direction' checkbox and Process again. ";
            whatToTry += "Now, under Particle 'Shape' pick 'Arrow'. What you're now seeing is not simply WHERE the player went, ";
            whatToTry += "but what direction they flew.\n\n";

            whatToTry += "Under 'Smooth', select '#' for 'Time', then Process again. ";
            whatToTry += "You might try bringing the particle size up to around 25. In the Render section ";
            whatToTry += "under 'Time' note the start and end values. Change the end value to 1, change 'Play Speed' to 0.1 and ";
            whatToTry += "press the 'Play' button to watch the airplanes fly! With a little practice, you can even scrub the timeline.";

            sampleCode = "using UnityAnalyticsHeatmap;\n\n";
            sampleCode += "// First event reflects kills\n";
            sampleCode += "// Note how we're also recording the time, and \n// (by sending the entire transform instead of just position)\n";
            sampleCode += "// the rotation.\n";
            sampleCode += "HeatmapEvent.Send(\"CombatKills\",transform, Time.timeSinceLevelLoad);\n";
            sampleCode += "// A second event reflects player's position in space\n";
            sampleCode += "HeatmapEvent.Send(\"PlayerPosition\",transform, Time.timeSinceLevelLoad);\n";
            sampleCode += "// Obviously these need to be sent at appropriate times.";
        }

        #region implemented abstract members of DataStory
        public override Dictionary<double, string> Generate()
        {
            SetRandomSeed();
            List<string> eventNames = new List<string>(){"Heatmap.CombatKills", "Heatmap.PlayerPosition"};

            List<TestCustomEvent> events = new List<TestCustomEvent>();
            for (int a = 0; a < eventNames.Count; a++)
            {
                TestCustomEvent customEvent = new TestCustomEvent();
                customEvent.name = eventNames[a];
                var x = new TestEventParam("x", TestEventParam.Str, "");
                customEvent.Add(x);
                var y = new TestEventParam("y", TestEventParam.Str, "");
                customEvent.Add(y);
                var z = new TestEventParam("z", TestEventParam.Str, "");
                customEvent.Add(z);
                var t = new TestEventParam("t", TestEventParam.Str, "");
                customEvent.Add(t);
                var rx = new TestEventParam("rx", TestEventParam.Str, "");
                customEvent.Add(rx);
                var ry = new TestEventParam("ry", TestEventParam.Str, "");
                customEvent.Add(ry);
                var rz = new TestEventParam("rz", TestEventParam.Str, "");
                customEvent.Add(rz);
                events.Add(customEvent);
            }

            var retv = new Dictionary<double, string>();

            string data = "";
            int fileCount = 0;
            int eventCount = 500;
            int deviceCount = 5;
            int sessionCount = 1;

            // Custom to this lesson
            float randomRange = .25f;
            float radius = 1000f;
            Vector3 position = Vector3.zero, rotation = Vector3.zero, pointOnCircle = Vector3.zero;

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
                        TestCustomEvent customEvent = (c % 100 == 0) ? events[0] : events[1];
                        customEvent.SetParam("t", c.ToString());

                        Vector3 lastPosition = new Vector3(position.x,position.y,position.z);
                        position = UpdatePosition(ref position, ref pointOnCircle, radius, randomRange);
                        Vector3 dir = (lastPosition-position).normalized;
                        rotation = Quaternion.LookRotation(dir).eulerAngles;


                        customEvent.SetParam("x", position.x.ToString());
                        customEvent.SetParam("y", position.y.ToString());
                        customEvent.SetParam("z", position.z.ToString());
                        customEvent.SetParam("rx", rotation.x.ToString());
                        customEvent.SetParam("ry", rotation.y.ToString());
                        customEvent.SetParam("rz", rotation.z.ToString());

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
