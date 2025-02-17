using System;
using System.Collections.Generic;
using System.Text;


public static class FfmpegParser
{
    public static int GetTotalSeconds(string time)
    {
        var split = time.Split(":");
        int hour = int.Parse(split[0]);
        int minute = int.Parse(split[1]);
        int second = int.Parse(split[2].Split(".")[0]);

        return (hour * 3600) + (minute * 60) + second;
    }
}

