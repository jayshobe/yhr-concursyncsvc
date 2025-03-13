
using System;

namespace ConcurSyncLib
{


    public static class Log
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(ConcurSyncLib.Extensions.Log4NetLayout));

        public static void LogInfo(string message)
        {
            logger.Info(message);
        }

        public static void LogTrace(string message)
        {
            logger.Info(message);
        }

        public static void LogWarning(string message)
        {
            logger.Warn(message);
        }

        public static void LogError(string message, Exception ex = null)
        {

            logger.Error(message);
            if (ex != null)
            {
                logger.Error(ex.ToString());
            }
        }

        public static void LogDebug(string message)
        {
            logger.Debug(message);
        }

        public static void LogFatal(string message, Exception ex = null)
        {

            logger.Fatal(message);
            if (ex != null)
            {
                logger.Error(ex.ToString());
            }

        }
    }



}
