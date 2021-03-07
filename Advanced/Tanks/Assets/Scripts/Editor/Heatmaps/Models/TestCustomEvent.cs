using System.Collections.Generic;
using System;

public class TestCustomEvent : List<TestEventParam> {
    public string name = "Enter an event name";
    private const string separator = "|z|";

    override public string ToString()
    {
        string retv = name;
        if (Count > 0)
        {
            retv += separator;
        }
        for (int a = 0; a < Count; a++)
        {
            var param = this[a];
            retv += param.ToString();
            if (a < Count-1)
            {
                retv += separator;
            }
        }
        return retv;
    }

    public static TestCustomEvent Parse(string inputString)
    {
        string[] stringSeparators = new string[] {separator};
        var retv = new TestCustomEvent();
        var inputList = inputString.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
        retv.name = inputList[0];

        for (var a = 1; a < inputList.Length; a++)
        {
            string paramLine = inputList[a];
            if (!string.IsNullOrEmpty(paramLine))
            {
                retv.Add(TestEventParam.Parse(paramLine));
            }
        }
        return retv;
    }

    public static string StringifyList(List<TestCustomEvent> list)
    {
        string retv = "";
        for (int a = 0; a < list.Count; a++)
        {
            retv += list[a].ToString() + "\n";
        }
        return retv;
    }

    public string WriteEvent(int deviceId, int sessionId, double currentSeconds, string platform, bool isDebug = false)
    {
        return WriteEvent("device" + deviceId + "-DDDD-DDDD", "session" + sessionId + "-SSSS-SSSS", currentSeconds, platform, isDebug);
    }

    public string WriteEvent(string deviceId, string sessionId, double currentSeconds, string platform, bool isDebug = false)
    {
        double currentMilliseconds = currentSeconds * 1000;

        string evt = "";
        evt += currentMilliseconds + "\t";

        // AppID
        evt += "1234-abcd-5678-efgh\t";

        // Event Type
        evt += "custom\t";

        // User ID, Session ID
        evt += deviceId + "\t";
        evt += sessionId + "\t";

        // Remote IP
        evt += "1.1.1.1\t";

        // Platform
        evt += platform + "\t";

        // SDK Version
        evt += "5.3.4\t";

        // IsDebug
        evt += isDebug + "\t";

        // User agent
        evt += "Corridor%20Z/3 CFNetwork/758.2.8 Darwin/15.0.0\t";

        // Submit time
        evt += currentMilliseconds + "\t";

        // Event Name
        evt += this.name + "\t";

        evt += WriteParams();

        return evt;
    }

    string WriteParams()
    {
        // Build the JSON
        string evt = "{";

        for (int b = 0; b < this.Count; b++)
        {
            TestEventParam param = this[b];
            evt += Quotify(param.name) + ":";
            switch (param.type)
            {
                case TestEventParam.Str:
                    evt += Quotify(param.strValue);
                    break;
                case TestEventParam.Num:
                    float num = UnityEngine.Random.Range(param.min, param.max);
                    evt += Quotify(num.ToString());
                    break;
                case TestEventParam.Bool:
                    bool boolean = UnityEngine.Random.Range(0f, 1f) > .5f;
                    evt += Quotify(boolean.ToString());
                    break;
            }
            evt += ",";
        }
        evt += Quotify("unity.name") + ":" + Quotify(this.name) + "}\n";
        return evt;
    }

    string Quotify(string value)
    {
        return "\"" + value +         "\"";
    }

    public void SetParam(string key, object value, object value2 = null)
    {
        TestEventParam param = null;
        for (var a = 0; a < Count; a++)
        {
            if (this[a].name == key)
            {
                param = this[a];
                break;
            }
        }
        if (param != null)
        {
            switch(param.type)
            {
                case TestEventParam.Bool:
                    param.boolValue = (bool)value;
                    break;
                case TestEventParam.Str:
                    param.strValue = value.ToString();
                    break;
                case TestEventParam.Num:
                    param.min = (float)value;
                    param.max = (float)value2;
                    break;
            }
        }
    }
}

public class TestEventParam {
    public const int Bool = 2;
    public const int Str = 0;
    public const int Num = 1;

    public const string separator = "|x|";

    public string name = "Enter a param name";
    public int type = 0;
    public float min;
    public float max;
    public string strValue = "Enter a string value";
    public bool boolValue = false;

    public TestEventParam()
    {
    }

    public TestEventParam(string name, int type, string value)
    {
        this.name = name;
        this.type = type;
        this.strValue = value;
    }

    public TestEventParam(string name, int type, float min, float max)
    {
        this.name = name;
        this.type = type;
        this.min = min;
        this.max = max;
    }

    override public string ToString()
    {
        string value = (type == Str) ? strValue : min + separator + max;
        string retv = name + separator + type + separator + value;
        return retv;
    }

    public static TestEventParam Parse(string inputString)
    {
        string[] stringSeparators = new string[] {separator};

        string name = "Enter a param name";
        int type =  Str;
        string strValue = "Enter a string value";
        float minValue = 0;
        float maxValue = 0;
        var inputList = inputString.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (inputList.Length > 2)
        {
            name = inputList[0];
            type = int.Parse(inputList[1]);
            strValue = inputList[2];
            if (type == Num && inputList.Length > 3)
            {
                float.TryParse(inputList[2], out minValue);
                float.TryParse(inputList[3], out maxValue);
            }
        }
        if (type == Str)
        {
            return new TestEventParam(name, type, strValue);
        }
        return new TestEventParam(name, type, minValue, maxValue);
    }
}