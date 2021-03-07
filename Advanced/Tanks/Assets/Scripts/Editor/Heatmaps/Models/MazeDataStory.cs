using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnalyticsHeatmap
{
    abstract public class MazeDataStory : DataStory
    {
        public List<List<MazeMapPoint>> m_Map;
        public List<int[]> m_Route;

        protected int m_Width = 25;
        protected int m_Height = 25;


        protected int m_PlayThroughs = 5;
        protected int m_LinesPerFile = 200;
        protected int m_CurrentFileLines = 0;
        protected int m_EventCount = 100;
        protected string m_EventName = "Heatmap.PlayerPosition";

        protected Dictionary<string, MazeDirection> m_Directions = new Dictionary<string, MazeDirection> {
            { "N", new MazeDirection("N", 0, 1, "S") },
            { "S", new MazeDirection("S", 0, -1, "N") },
            { "E", new MazeDirection("E", 1, 0, "W") },
            { "W", new MazeDirection("W", -1, 0, "E") }
        };

        #region implemented abstract members of DataStory
        public override Dictionary<double, string> Generate()
        {
            return Play();
        }
        #endregion

        abstract protected Dictionary<double, string> Play();


        protected void Prefill()
        {
            m_Map =  new List<List<MazeMapPoint>>();
            m_Route = new List<int[]>();
            for (int x = 0; x < m_Width; x++)
            {
                m_Map.Add(new List<MazeMapPoint>());
                for (int y = 0; y < m_Height; y++)
                {
                    m_Map[x].Add(new MazeMapPoint(x, y));
                }
            }
        }

        protected void Carve(int x0, int y0, MazeDirection direction, MazeDirection lastDirection)
        {
            int x1 = x0 + direction.dx;
            int y1 = y0 + direction.dy;
            if (x1 == 0 || x1 == m_Width || y1 == 0 || y1 == m_Height)
            {
                return;
            }
            if ( m_Map[x1][y1].seen )
            {
                return;
            }
            m_Map[x0][y0].exit = direction.direction;
            m_Map[x1][y1].entrance = direction.opposite;
            m_Map[x1][y1].seen = true;

            var possibles = new List<string>(){ "N", "S", "E", "W" };
            Shuffle(possibles);

            if (lastDirection != null)
            {
                possibles.Remove(lastDirection.direction);
                possibles.Add(lastDirection.direction);
            }

            for (var i = 0; i < possibles.Count; i++) {
                Carve(x1, y1, m_Directions[possibles[i]], direction);
            }
        }

        static System.Random rnd = new System.Random(42);
        protected static void Shuffle<T>(IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                int k = (rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        protected int[] Move(int[] position, int[] lastPosition)
        {
            int[] newPosition = position.Clone() as int[];
            if (position[0] <= 1 || position[0] >= m_Width-1 || position[1] <= 0 || position[1] >= m_Height-1)
            {
                return lastPosition;
            }
            var possibles = new List<string>(){ "N", "S", "E", "W" };
            Shuffle(possibles);
            for (var a = 0; a < possibles.Count; a++) {
                MazeDirection direction = m_Directions[possibles[a]];
                MazeMapPoint pt0 = m_Map[position[0]][position[1]];
                int newX = position[0] + direction.dx;
                int newY = position[1] + direction.dy;
                MazeMapPoint pt1 = m_Map[newX][newY];

                if (lastPosition[0] == pt1.x && lastPosition[1] == pt1.y)
                {
                    continue;
                }
                else if (pt0.exit == direction.direction || pt0.entrance == direction.direction)
                {
                    newPosition[0] = pt1.x;
                    newPosition[1] = pt1.y;
                    break;
                }
            }
            return newPosition;
        }

        public static Vector3 GetVecPos(string dir)
        {
            switch(dir)
            {
                case "N":
                    return new Vector3(0f, .5f, 0f);
                case "S":
                    return new Vector3(0f, -.5f, 0f);
                case "E":
                    return new Vector3(0.5f, 0f, 0f);
                case "W":
                    return new Vector3(-0.5f, 0f, 0f);
            }
            return Vector3.zero;
        }

        public static Quaternion GetQ(string dir)
        {
            switch(dir)
            {
                case "N":
                    return Quaternion.identity;
                case "S":
                    return Quaternion.identity;
                case "E":
                    return Quaternion.Euler(0f, 0, 90f);
                case "W":
                    return Quaternion.Euler(0f, 0, 90f);
            }
            return Quaternion.identity;
        }
    }

    public class MazeMapPoint
    {
        public bool seen = false;
        public int x = 0;
        public int y = 0;
        public string exit;
        public string entrance;

        public MazeMapPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class MazeDirection
    {
        public string direction;
        public int dx;
        public int dy;
        public string opposite;

        public MazeDirection(string direction, int dx, int dy, string opposite)
        {
            this.direction = direction;
            this.dx = dx;
            this.dy = dy;
            this.opposite = opposite;
        }
    }
}

