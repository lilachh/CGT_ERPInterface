using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace ERPInterface
{
    public class Log
    {
        private const string SError = "Error";

        private const string SDebug = "Debug";

        private const string DefaultName = "Info";



        static Log()
        {

            var path = AppDomain.CurrentDomain.BaseDirectory + @"\Log4NetConfig.xml";

            log4net.Config.XmlConfigurator.Configure(new FileInfo(path));

        }



        public static log4net.ILog GetLog(string logName)
        {

            var log = log4net.LogManager.GetLogger(logName);

            return log;

        }



        public static void Debug(string message)
        {

            var log = log4net.LogManager.GetLogger(SDebug);

            if (log.IsDebugEnabled)

                log.Debug(message);

        }



        public static void Debug(string message, Exception ex)
        {

            var log = log4net.LogManager.GetLogger(SDebug);

            if (log.IsDebugEnabled)

                log.Debug(message, ex);

        }



        public static void Error(string message)
        {

            var log = log4net.LogManager.GetLogger(SError);

            if (log.IsErrorEnabled)

                log.Error(message);

        }



        public static void Error(string message, Exception ex)
        {

            var log = log4net.LogManager.GetLogger(SError);

            if (log.IsErrorEnabled)

                log.Error(message, ex);

        }



        public static void Fatal(string message)
        {

            var log = log4net.LogManager.GetLogger(DefaultName);

            if (log.IsFatalEnabled)

                log.Fatal(message);

        }



        public static void Info(string message)
        {

            log4net.ILog log = log4net.LogManager.GetLogger(DefaultName);

            if (log.IsInfoEnabled)

                log.Info(message);

        }



        public static void Warn(string message)
        {

            var log = log4net.LogManager.GetLogger(DefaultName);

            if (log.IsWarnEnabled)

                log.Warn(message);

        }
    }
}