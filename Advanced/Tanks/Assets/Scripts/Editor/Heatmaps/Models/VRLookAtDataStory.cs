using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnalyticsHeatmap
{
    public class VRLookAtDataStory : DataStory
    {
        public VRLookAtDataStory()
        {
            name = "VR Look At";
            genre = "VR Adventure";
            description = "Imagine this data as part of a VR game. Your question: as they move through my game, ";
            description += "are users looking where I want them to look?";
            whatToTry = "Generate this data, then click Process in the Heatmapper. In the render setting under 'Shape', ";
            whatToTry += "pick 'Point to Point'. Observe how you can see not just where the user was in the virtual world, ";
            whatToTry += "but also what they were looking at.\n\n";
            whatToTry += "This is done by sending two Vector3s. The first is the position of the player. The second ";
            whatToTry += "is the position of a collider to which we raycast. The same technique could be used in a first-person ";
            whatToTry += "shooter to find the things the player shot.\n\n";
            whatToTry += "Now look at the 'Masking' subsection. This allows you to trim away data based on its position. ";
            whatToTry += "Note how the Y axis has no handles. This is because all the Y data in this demo is at ";
            whatToTry += "the same ordinate. Try tweaking the X and Z values to isolate out and inspect a single source position.";

            sampleCode = "using UnityAnalyticsHeatmap;\n\n";
            sampleCode += "// The otherGameObject in this case is a GameObject represented by a collider.\n";
            sampleCode += "// By raycasting from where the player is standing, we can see what they saw.\n";
            sampleCode += "HeatmapEvent.Send(\"LookAt\",transform.position,otherGameObject.transform.position,Time.timeSinceLevelLoad);";
        }

        #region implemented abstract members of DataStory
        public override Dictionary<double, string> Generate()
        {
            SetRandomSeed();
            List<string> eventNames = new List<string>(){"Heatmap.LookAt"};

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
                var dx = new TestEventParam("dx", TestEventParam.Str, "");
                customEvent.Add(dx);
                var dy = new TestEventParam("dy", TestEventParam.Str, "");
                customEvent.Add(dy);
                var dz = new TestEventParam("dz", TestEventParam.Str, "");
                customEvent.Add(dz);
                events.Add(customEvent);
            }

            var retv = new Dictionary<double, string>();

            string data = "";
            int fileCount = 0;
            int eventCount = 30;
            int deviceCount = 5;
            int sessionCount = 1;

            // Custom to this lesson
            int lookThisManyPlaces = 3;
            int lookThisManyTimesMin = 5;
            int lookThisManyTimesMax = 20;
            float randomRange = .25f;
            float radius = 1000f;

            DateTime now = DateTime.UtcNow;
            int totalSeconds = deviceCount * eventCount * sessionCount;
            double endSeconds = Math.Round((now - UnityAnalytics.DateTimeUtils.s_Epoch).TotalSeconds);
            double startSeconds = endSeconds - totalSeconds;
            double currentSeconds = startSeconds;
            double firstDate = currentSeconds;

            Vector3 position = Vector3.zero, destination = Vector3.zero, pointOnCircle = Vector3.zero;

            for (int a = 0; a < deviceCount; a++)
            {
                string platform = "ios";
                for (int b = 0; b < sessionCount; b++)
                {
                    for (int c = 0; c < eventCount; c++)
                    {
                        pointOnCircle = new Vector3(UnityEngine.Random.Range(-radius, radius),
                            0f,
                            UnityEngine.Random.Range(-radius, radius));
                        position = UpdatePosition(ref position, ref pointOnCircle, radius, randomRange);
                        position.y = 0f;

                        for (int e = 0; e < lookThisManyPlaces; e++)
                        {
                            int numTimesToLook = UnityEngine.Random.Range(lookThisManyTimesMin, lookThisManyTimesMax);
                            float xAddition = UnityEngine.Random.Range(-radius, radius);
                            float yAddition = UnityEngine.Random.Range(0, radius/2f);
                            float zAddition = UnityEngine.Random.Range(-radius, radius);

                            while (numTimesToLook > 0)
                            {
                                numTimesToLook --;
                                currentSeconds ++;
                                TestCustomEvent customEvent = events[0];
                                customEvent.SetParam("t", c.ToString());

                                destination = new Vector3(position.x + xAddition, position.y + yAddition, position.z + zAddition);

                                customEvent.SetParam("x", position.x.ToString());
                                customEvent.SetParam("y", position.y.ToString());
                                customEvent.SetParam("z", position.z.ToString());
                                customEvent.SetParam("dx", destination.x.ToString());
                                customEvent.SetParam("dy", destination.y.ToString());
                                customEvent.SetParam("dz", destination.z.ToString());

                                string evt = customEvent.WriteEvent(a, b, currentSeconds, platform);
                                data += evt;
                            }
                        }

                    }
                }
            }
            retv.Add(firstDate, data);
            fileCount++;
            return retv;
        }
        #endregion
    }
}

