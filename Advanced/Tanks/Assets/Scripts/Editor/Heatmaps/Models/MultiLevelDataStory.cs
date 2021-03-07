using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnalyticsHeatmap
{
    public class MultiLevelDataStory : MazeDataStory
    {
        int m_Levels = 5;

        public MultiLevelDataStory()
        {
            name = "Maze 1: Multilevel Game";
            genre = "2D Maze Game";
            description = "This demo shows how you can separate events by level. In fact, you can separate by ";
            description += "pretty much ANYTHING.";
            whatToTry = "Generate this data, which shows player position in a 2D maze game. Open the Heatmapper. ";
            whatToTry += "Ensure your space smoothing is 1 and particle size is .75. Uncheck 'time' and click the Process button. ";
            whatToTry += "Under 'Time' set the end time and 'Play Speed' to 1. Click 'Play'.\n\n";
            whatToTry += "Now, there's a LOT of data here and the action looks very messy. That's because what you're seeing is actually ";
            whatToTry += "data from MANY levels on top of each other. Click the 'Separate on Field' button and replace the words ";
            whatToTry += "'Field Name' with 'level' (case matters!). Now Process again. ";
            whatToTry += "Again, set the end time to 1 and press 'Play'. Hey presto! You can see the clear shape ";
            whatToTry += "of players navigating a single maze level. Did you see the 'Option' dropdown appear? Click that dropdown. ";
            whatToTry += "You can now choose to view each level's worth of data individually.\n\n";
            whatToTry += "Under 'Aggregate', uncheck 'Unique Devices' and click Process. Open the 'Option' list to see how it has changed. ";
            whatToTry += "Not only are the levels separated, so are the individual devices. You can use this to see how individual ";
            whatToTry += "players play.";
            sampleCode = "using UnityAnalyticsHeatmap;\n";
            sampleCode += "using System.Collections.Generic;\n\n";
            sampleCode += "// The level and gameTurn variables are examples.\n";
            sampleCode += "// You'll need to maintain a level variable and place the result in the dictionary.\n";
            sampleCode += "// gameTurn points out that the 'time' variable can reflect any numerical value you want.\n";
            sampleCode += "HeatmapEvent.Send(\"PlayerPosition\",transform.position,gameTurn,new Dictionary<string,object>(){{\"level\", level}});";
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

                        customEvent.SetParam("x", position[0].ToString());
                        customEvent.SetParam("y", position[1].ToString());
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

