using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace UnityAnalyticsHeatmap
{
    public class FPSDropoffDataStory : MazeDataStory
    {
        int m_Levels = 5;

        public FPSDropoffDataStory()
        {
            name = "Maze 2: FPS Dropoff";
            genre = "2D Maze Game";
            description = "In the second half of our maze game exploration, your question is: where in my game does my framerate drop? ";
            description += "We're sending the same PlayerPosition event, but this time " ;
            description += "we're sending 'fps' as a parameter. We can chart it and get an idea of where the game might be slowing down.";
            whatToTry = "Generate this data. In the Heatmapper, process it. In the 'Render' section, check that your shape is 'Cube' or 'Square'. ";
            whatToTry += "Now check 'Remap color to field'. In the textfield ";
            whatToTry += "that appears, enter the value 'fps'. Set 'Remap Operation' to 'Lowest Wins'. Now process again.\n\n";
            whatToTry += "Observe how color now represents places with higher and lower FPS. ";
            whatToTry += "Remember that color on the right side of the gradient represents HIGHER density, and since you've re-mapped fps ";
            whatToTry += "the color on the right will display HIGH fps. Also look at the possible Remap operations. ";
            whatToTry += "When mapping FPS, we probably care most about the lowest FPS scores (i.e., where our game is going slow), ";
            whatToTry += "so 'Lowest Wins' makes a lot of sense. But we can also operate incrementally, cumlatively, etc., ";
            whatToTry += "depending on what's most important for the variable we're mapping.\n\n";
            whatToTry += "Remap operations:\n";
            whatToTry += "* Increment: Add exactly one each time we see a point at this location\n";
            whatToTry += "* Cumulative: Add all remap values together\n";
            whatToTry += "* First Wins: Use the first value we see\n";
            whatToTry += "* Last Wins: Use the last value we see\n";
            whatToTry += "* Min Wins: Use the lowest of all values\n";
            whatToTry += "* Max Wins: Use the highest of all values\n";
            sampleCode = "using UnityAnalyticsHeatmap;\n";
            sampleCode += "using System.Collections.Generic;\n\n";
            sampleCode += "// The fps and gameTurn variables are examples.\n";
            sampleCode += "// You'll need to calculate fps and place the result in the dictionary.\n";
            sampleCode += "// gameTurn points out that the 'time' variable can reflect any numerical value you want.\n";
            sampleCode += "HeatmapEvent.Send(\"PlayerPosition\",transform.position,gameTurn,new Dictionary<string,object>(){{\"fps\", fps}});";
        }

        override protected Dictionary<double, string> Play()
        {
            SetRandomSeed();
            List<string> eventNames = new List<string>(){m_EventName};

            List<TestCustomEvent> events = new List<TestCustomEvent>();
            for (int a = 0; a < eventNames.Count; a++)
            {
                TestCustomEvent customEvent = new TestCustomEvent();
                customEvent.name = eventNames[a];
                var x = new TestEventParam("x", TestEventParam.Str, "");
                customEvent.Add(x);
                var y = new TestEventParam("y", TestEventParam.Str, "");
                customEvent.Add(y);
                var t = new TestEventParam("t", TestEventParam.Str, "");
                customEvent.Add(t);
                var fps = new TestEventParam("fps", TestEventParam.Str, "");
                customEvent.Add(fps);
                var level = new TestEventParam("level", TestEventParam.Str, "");
                customEvent.Add(level);
                events.Add(customEvent);
            }

            var retv = new Dictionary<double, string>();

            string data = "";
            int fileCount = 0;
            int eventCount = 100;
            int deviceCount = 5;
            int sessionCount = 3;

            // Custom to this lesson
            int[] position = new int[2]{m_Width/2, m_Height/2};
            int[] lastPosition = new int[2]{m_Width/2, m_Height/2};

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
                    Prefill();
                    Carve(m_Width/2, m_Height/2, m_Directions["N"], null);

                    int level = UnityEngine.Random.Range(1, m_Levels);


                    for (int c = 0; c < eventCount; c++)
                    {
                        currentSeconds ++;
                        TestCustomEvent customEvent = events[0];
                        customEvent.SetParam("t", c.ToString());

                        if (c == 0) {
                            position = new int[2]{m_Width/2, m_Height/2};
                            lastPosition = new int[2]{m_Width/2, m_Height/2};
                        }

                        int[] previousPosition = position.Clone() as int[];
                        position = Move(position, lastPosition);
                        m_Route.Add(position);
                        lastPosition = previousPosition;

                        float fps = UnityEngine.Random.Range(30f, 90f);
                        fps -= Mathf.Abs(10-position[0]) * Mathf.Abs(10-position[1]);

                        customEvent.SetParam("x", position[0].ToString());
                        customEvent.SetParam("y", position[1].ToString());
                        customEvent.SetParam("fps", fps.ToString());
                        customEvent.SetParam("level", level.ToString());

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
    }
}

