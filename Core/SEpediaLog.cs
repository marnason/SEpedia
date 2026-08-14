using System;
using VRage.Utils;

namespace SEpedia.Core
{
    internal static class SEpediaLog
    {
        public static void Info(string message)
        {
            MyLog.Default.WriteLineAndConsole("[SEpedia] " + message);
        }

        public static void Warning(string message)
        {
            MyLog.Default.WriteLineAndConsole("[SEpedia] WARNING: " + message);
        }

        public static void Error(string message, Exception exception)
        {
            MyLog.Default.WriteLineAndConsole("[SEpedia] ERROR: " + message + "\n" + exception);
        }
    }
}
