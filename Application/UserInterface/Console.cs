/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using log4net.Repository;
using log4net.Appender;
using log4net.Core;

namespace IdealistViewer
{
    public class Console
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly object m_syncRoot = new object();

        private Viewer m_viewer;
        public IConsoleCommandParser CommandParser;

        /// <summary>
        /// The default prompt text.
        /// </summary>
        public string DefaultPrompt
        {
            set { m_defaultPrompt = value + "# "; }
            get { return m_defaultPrompt; }
        }
        protected string m_defaultPrompt;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="defaultPrompt"></param>
        /// <param name="cmdparser"></param>
        public Console(Viewer viewer,string defaultPrompt, IConsoleCommandParser cmdparser)
        {
            DefaultPrompt = defaultPrompt;
            CommandParser = cmdparser;
            m_viewer = viewer;
        }

        /// <summary>
        /// derive an ansi color from a string, ignoring the darker colors.
        /// This is used to help automatically bin component tags with colors
        /// in various print functions.
        /// </summary>
        /// <param name="input">arbitrary string for input</param>
        /// <returns>an ansii color</returns>
        private static ConsoleColor DeriveColor(string input)
        {
            int colIdx = (input.ToUpper().GetHashCode() % 6) + 9;
            return (ConsoleColor)colIdx;
        }

        /// <summary>
        /// Sends a warning to the current console output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Warn(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Yellow, format, args);
        }

        /// <summary>
        /// Sends a warning to the current console output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Warn(string sender, string format, params object[] args)
        {
            WritePrefixLine(DeriveColor(sender), sender);
            WriteNewLine(ConsoleColor.Yellow, format, args);
        }

        /// <summary>
        /// Sends a notice to the current console output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Notice(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.White, format, args);
        }

        /// <summary>
        /// Sends a notice to the current console output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Notice(string sender, string format, params object[] args)
        {
            WritePrefixLine(DeriveColor(sender), sender);
            WriteNewLine(ConsoleColor.White, format, args);
        }

        /// <summary>
        /// Sends an error to the current console output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Error(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Red, format, args);
        }

        /// <summary>
        /// Sends an error to the current console output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Error(string sender, string format, params object[] args)
        {
            WritePrefixLine(DeriveColor(sender), sender);
            Error(format, args);
        }

        /// <summary>
        /// Sends a status message to the current console output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Status(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Blue, format, args);
        }

        /// <summary>
        /// Sends a status message to the current console output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Status(string sender, string format, params object[] args)
        {
            WritePrefixLine(DeriveColor(sender), sender);
            WriteNewLine(ConsoleColor.Blue, format, args);
        }

        [Conditional("DEBUG")]
        public void Debug(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Gray, format, args);
        }

        [Conditional("DEBUG")]
        public void Debug(string sender, string format, params object[] args)
        {
            WritePrefixLine(DeriveColor(sender), sender);
            WriteNewLine(ConsoleColor.Gray, format, args);
        }

        private void WriteNewLine(ConsoleColor color, string format, params object[] args)
        {
            try
            {
                lock (m_syncRoot)
                {
                    try
                    {
                        if (color != ConsoleColor.White)
                            System.Console.ForegroundColor = color;

                        System.Console.WriteLine(format, args);
                        System.Console.ResetColor();
                    }
                    catch (ArgumentNullException)
                    {
                        // Some older systems dont support coloured text.
                        System.Console.WriteLine(format, args);
                    }
                    catch (FormatException)
                    {
                        System.Console.WriteLine(args);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void WritePrefixLine(ConsoleColor color, string sender)
        {
            try
            {
                lock (m_syncRoot)
                {
                    sender = sender.ToUpper();

                    System.Console.WriteLine("[" + sender + "] ");

                    System.Console.Write("[");

                    try
                    {
                        System.Console.ForegroundColor = color;
                        System.Console.Write(sender);
                        System.Console.ResetColor();
                    }
                    catch (ArgumentNullException)
                    {
                        // Some older systems dont support coloured text.
                        System.Console.WriteLine(sender);
                    }

                    System.Console.Write("] \t");
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public string ReadLine()
        {
            try
            {
                string line = System.Console.ReadLine();

                while (line == null)
                {
                    line = System.Console.ReadLine();
                }

                return line;
            }
            catch (Exception e)
            {
                m_log.Error("[Console]: System.Console.ReadLine exception " + e.ToString());
                return String.Empty;
            }
        }

        public int Read()
        {
            return System.Console.Read();
        }

        public IPAddress CmdPromptIPAddress(string prompt, string defaultvalue)
        {
            IPAddress address;
            string addressStr;

            while (true)
            {
                addressStr = CmdPrompt(prompt, defaultvalue);
                if (IPAddress.TryParse(addressStr, out address))
                {
                    break;
                }
                else
                {
                    m_log.Error("Illegal address. Please re-enter.");
                }
            }

            return address;
        }

        public uint CmdPromptIPPort(string prompt, string defaultvalue)
        {
            uint port;
            string portStr;

            while (true)
            {
                portStr = CmdPrompt(prompt, defaultvalue);
                if (uint.TryParse(portStr, out port))
                {
                    if (port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
                    {
                        break;
                    }
                }

                m_log.Error("Illegal address. Please re-enter.");
            }

            return port;
        }

        // Displays a prompt and waits for the user to enter a string, then returns that string
        // (Done with no echo and suitable for passwords - currently disabled)
        public string PasswdPrompt(string prompt)
        {
            // FIXME: Needs to be better abstracted
            System.Console.WriteLine(String.Format("{0}: ", prompt));
            //ConsoleColor oldfg = System.Console.ForegroundColor;
            //System.Console.ForegroundColor = System.Console.BackgroundColor;
            string temp = System.Console.ReadLine();
            //System.Console.ForegroundColor = oldfg;
            return temp;
        }

        // Displays a command prompt and waits for the user to enter a string, then returns that string
        public string CmdPrompt(string prompt)
        {
            System.Console.WriteLine(String.Format("{0}: ", prompt));
            return ReadLine();
        }

        // Displays a command prompt and returns a default value if the user simply presses enter
        public string CmdPrompt(string prompt, string defaultresponse)
        {
            string temp = CmdPrompt(String.Format("{0} [{1}]", prompt, defaultresponse));
            if (temp == String.Empty)
            {
                return defaultresponse;
            }
            else
            {
                return temp;
            }
        }

        // Displays a command prompt and returns a default value, user may only enter 1 of 2 options
        public string CmdPrompt(string prompt, string defaultresponse, string OptionA, string OptionB)
        {
            bool itisdone = false;
            string temp = CmdPrompt(prompt, defaultresponse);
            while (itisdone == false)
            {
                if ((temp == OptionA) || (temp == OptionB))
                {
                    itisdone = true;
                }
                else
                {
                    System.Console.WriteLine("Valid options are " + OptionA + " or " + OptionB);
                    temp = CmdPrompt(prompt, defaultresponse);
                }
            }
            return temp;
        }

        // Runs a command with a number of parameters
        public Object RunCmd(string Cmd, string[] cmdparams)
        {
            CommandParser.OnConsoleCommand(Cmd, cmdparams);
            return null;
        }

        // Shows data about something
        public void ShowCommands(string ShowWhat)
        {
            Show(new string[] { ShowWhat });
        }

        public void Prompt()
        {
            string tempstr = CmdPrompt(m_defaultPrompt);
            RunCommand(tempstr);
        }

        public void RunCommand(string cmdline)
        {
            Regex Extractor = new Regex(@"(['""][^""]+['""])\s*|([^\s]+)\s*", RegexOptions.Compiled);
            char[] delims = { ' ', '"' };
            MatchCollection matches = Extractor.Matches(cmdline);
            // Get matches

            if (matches.Count == 0)
                return;

            string cmd = matches[0].Value.Trim(delims);
            string[] cmdparams = new string[matches.Count - 1];

            for (int i = 1; i < matches.Count; i++)
            {
                cmdparams[i - 1] = matches[i].Value.Trim(delims);
            }

            try
            {
                RunCmd(cmd, cmdparams);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[Console]: Command [{0}] failed with exception {1}", cmdline, e.ToString());
            }
        }

        public string LineInfo
        {
            get
            {
                string result = String.Empty;

                string stacktrace = Environment.StackTrace;
                List<string> lines = new List<string>(stacktrace.Split(new string[] { "at " }, StringSplitOptions.None));

                if (lines.Count > 4)
                {
                    lines.RemoveRange(0, 4);

                    string tmpLine = lines[0];

                    int inIndex = tmpLine.IndexOf(" in ");

                    if (inIndex > -1)
                    {
                        result = tmpLine.Substring(0, inIndex);

                        int lineIndex = tmpLine.IndexOf(":line ");

                        if (lineIndex > -1)
                        {
                            lineIndex += 6;
                            result += ", line " + tmpLine.Substring(lineIndex, (tmpLine.Length - lineIndex) - 5);
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Show help information
        /// </summary>
        /// <param name="helpArgs"></param>
        public void ShowHelp(string[] helpArgs)
        {
            if (helpArgs.Length == 0)
            {
                Notice("");


                Notice("goto region/x/y/z - teleport to a location");
                Notice("say [message] - says a message over chat");

                // TODO: not yet implemented
                //Notice("help [command] - display general help or specific command help.  Try help help for more info.");
                Notice("quit - equivalent to shutdown.");

                Notice("set log level [level] - change the console logging level only.  For example, off or debug.");
                Notice("show info - show server information (e.g. startup path).");

                //if (m_stats != null)
                //    Notice("show stats - show statistical information for this server");

                Notice("show threads - list tracked threads");
                Notice("show uptime - show server startup time and uptime.");
                Notice("show version - show server version.");
                Notice("shutdown - shutdown the server.\n");

                return;
            }
        }

        /// <summary>
        /// Outputs to the console information about the region
        /// </summary>
        /// <param name="showParams">
        /// What information to display (valid arguments are "uptime", "users", ...)
        /// </param>
        public void Show(string[] showParams)
        {
            switch (showParams[0])
            {
                case "info":
                    Notice("Version: " + m_viewer.Version);
                    Notice("Startup directory: " + m_viewer.StartupDirectory);
                    break;



                case "version":
                    Notice("Version: " + m_viewer.Version);
                    break;
            }
        }

        /// <summary>
        /// Console output is only possible if a console has been established.
        /// That is something that cannot be determined within this class. So
        /// all attempts to use the console MUST be verified.
        /// </summary>
        private void Notice(string msg)
        {
            Notice(msg);
        }

        /// <summary>
        /// Set the level of log notices being echoed to the console
        /// </summary>
        /// <param name="setParams"></param>
        public void SetConsoleLogLevel(string[] setParams)
        {
            ILoggerRepository repository = LogManager.GetRepository();
            IAppender[] appenders = repository.GetAppenders();
            IdealistViewerAppender consoleAppender = null;

            foreach (IAppender appender in appenders)
            {
                if (appender.Name == "Console")
                {
                    consoleAppender = (IdealistViewerAppender)appender;
                    break;
                }
            }

            if (null == consoleAppender)
            {
                Notice("No appender named Console found (see the log4net config file for this executable)!");
                return;
            }

            if (setParams.Length > 0)
            {
                Level consoleLevel = repository.LevelMap[setParams[0]];
                if (consoleLevel != null)
                    consoleAppender.Threshold = consoleLevel;
                else
                    Notice(
                        String.Format(
                            "{0} is not a valid logging level.  Valid logging levels are ALL, DEBUG, INFO, WARN, ERROR, FATAL, OFF",
                            setParams[0]));
            }

            // If there is no threshold set then the threshold is effectively everything.
            Level thresholdLevel
                = (null != consoleAppender.Threshold ? consoleAppender.Threshold : log4net.Core.Level.All);

            Notice(String.Format("Console log level is {0}", thresholdLevel));
        }

    }

}
