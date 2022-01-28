// ;
using Sandbox.ModAPI;
using System;
using System.IO;
using VRage.Utils;

namespace ExShared
{
    public class Logger
    {
        public const string c_LogPrefix = "[" + PocketShieldCore.Constants.LOG_PREFIX + "]";

        public bool Suppressed
        {
            get { return m_Suppressed; }
            set
            {
                if (value == m_Suppressed)
                    return;

                if (value)
                {
                    m_Suppressed = false;
                    WriteLine(">> Log Suppressed <<");
                    m_Suppressed = true;
                }
                else
                {
                    m_Suppressed = false;
                    WriteLine(">> Log Unsuppressed <<");
                }
            }
        }
        public int LogLevel { get; set; }

        private bool m_Suppressed = false;
        private TextWriter m_TextWriter = null;

        public Logger(string _name)
        {
            LogLevel = 5;

            string filename = "debug_" + _name;
            try
            {
                m_TextWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(filename + ".log", typeof(Logger));
            }
            catch (Exception _e)
            {
                MyLog.Default.WriteLine(c_LogPrefix + " > Exception < Problem encountered while initializing logger '" + filename + "': " + _e.Message);
            }

            WriteLine(">> Log Begin <<");
        }

        public void Close()
        {
            if (m_TextWriter != null)
            {
                m_Suppressed = false;
                WriteLine(">> Log End <<");
                m_TextWriter.Close();
            }
        }

        public void Write(string _message, int _level = 0)
        {
            if (m_Suppressed || _level > LogLevel)
                return;

            try
            {
                m_TextWriter.Write("[" + DateTime.Now.ToString("yy.MM.dd HH:mm:ss.fff") + "][" + _level + "]: " + _message);
                m_TextWriter.Flush();
            }
            catch (Exception _e)
            {
                MyLog.Default.WriteLine(c_LogPrefix + " > Exception < Problem encountered while logging: " + _e.Message);
                MyLog.Default.WriteLine(c_LogPrefix + "   Msg: " + _message);
            }
        }

        public void WriteLine(string _message, int _level = 0)
        {
            if (m_Suppressed || _level > LogLevel)
                return;

            try
            {
                m_TextWriter.WriteLine("[" + DateTime.Now.ToString("yy.MM.dd HH:mm:ss.fff") + "][" + _level + "]: " + _message);
                m_TextWriter.Flush();
            }
            catch (Exception _e)
            {
                MyLog.Default.WriteLine(c_LogPrefix + " > Exception < Problem encountered while logging: " + _e.Message);
                MyLog.Default.WriteLine(c_LogPrefix + "   Msg: " + _message);
            }
        }

        public void WriteInline(string _message, int _level = 0, bool _breakNow = false)
        {
            if (m_Suppressed || _level > LogLevel)
                return;

            try
            {
                if (_breakNow)
                    m_TextWriter.WriteLine(_message);
                else
                    m_TextWriter.Write(_message);
                m_TextWriter.Flush();
            }
            catch (Exception _e)
            {
                MyLog.Default.WriteLine(c_LogPrefix + " > Exception < Problem encountered while logging: " + _e.Message);
                MyLog.Default.WriteLine(c_LogPrefix + "   Msg: " + _message);
            }
        }

        public void WriteCRLF(int _level = 0)
        {
            if (m_Suppressed || _level > LogLevel)
                return;

            try
            {
                m_TextWriter.WriteLine();
                m_TextWriter.Flush();
            }
            catch (Exception _e)
            {
                MyLog.Default.WriteLine(c_LogPrefix + " > Exception < Problem encountered while logging: " + _e.Message);
                MyLog.Default.WriteLine(c_LogPrefix + "   Msg: \\n");
            }
        }

        private string GetDateTimeAsString()
        {
            DateTime datetime = DateTime.Now;
            //DateTime datetime = DateTime.UtcNow + m_LocalUtcOffset.TimeSpan;
            return datetime.ToString("yy.MM.dd HH:mm:ss.fff");
        }

    }
}
