
using System;
using AlicizaX;
using UnityEngine;

namespace AlicizaX.Debugger
{
    public sealed partial class DebuggerComponent
    {
        /// <summary>
        /// 日志记录结点。
        /// </summary>
    public sealed class LogNode : MemoryObject
        {
            private DateTime m_LogTime;
            private int m_LogFrameCount;
            private LogType m_LogType;
            private string m_LogMessage;
            private string m_StackTrack;
            private string m_DisplayText;

            /// <summary>
            /// 初始化日志记录结点的新实例。
            /// </summary>
            public LogNode()
            {
                m_LogTime = default(DateTime);
                m_LogFrameCount = 0;
                m_LogType = LogType.Error;
                m_LogMessage = null;
                m_StackTrack = null;
                m_DisplayText = null;
            }

            /// <summary>
            /// 获取日志时间。
            /// </summary>
            public DateTime LogTime
            {
                get
                {
                    return m_LogTime;
                }
            }

            /// <summary>
            /// 获取日志帧计数。
            /// </summary>
            public int LogFrameCount
            {
                get
                {
                    return m_LogFrameCount;
                }
            }

            /// <summary>
            /// 获取日志类型。
            /// </summary>
            public LogType LogType
            {
                get
                {
                    return m_LogType;
                }
            }

            /// <summary>
            /// 获取日志内容。
            /// </summary>
            public string LogMessage
            {
                get
                {
                    return m_LogMessage;
                }
            }

            /// <summary>
            /// 获取日志堆栈信息。
            /// </summary>
            public string StackTrack
            {
                get
                {
                    return m_StackTrack;
                }
            }

            /// <summary>
            /// 获取列表里单行显示的日志文本。首次访问时生成并缓存，
            /// 避免列表每次重绑都重新格式化与替换换行。
            /// </summary>
            public string DisplayText
            {
                get
                {
                    if (m_DisplayText == null)
                    {
                        m_DisplayText = BuildDisplayText();
                    }

                    return m_DisplayText;
                }
            }

            private string BuildDisplayText()
            {
                string message = m_LogMessage ?? string.Empty;
                if (message.IndexOf('\n') >= 0 || message.IndexOf('\r') >= 0)
                {
                    message = message.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
                }

                return Utility.Text.Format("[{0:HH:mm:ss.fff}][{1}] {2}", m_LogTime.ToLocalTime(), m_LogFrameCount, message);
            }

            /// <summary>
            /// 创建日志记录结点。
            /// </summary>
            /// <param name="logType">日志类型。</param>
            /// <param name="logMessage">日志内容。</param>
            /// <param name="stackTrack">日志堆栈信息。</param>
            /// <returns>创建的日志记录结点。</returns>
            public static LogNode Create(LogType logType, string logMessage, string stackTrack)
            {
                LogNode logNode = MemoryPool.Acquire<LogNode>();
                logNode.m_LogTime = DateTime.UtcNow;
                logNode.m_LogFrameCount = Time.frameCount;
                logNode.m_LogType = logType;
                logNode.m_LogMessage = logMessage;
                logNode.m_StackTrack = stackTrack;
                return logNode;
            }

            /// <summary>
            /// 清理日志记录结点。
            /// </summary>
        public override void Clear()
            {
                m_LogTime = default(DateTime);
                m_LogFrameCount = 0;
                m_LogType = LogType.Error;
                m_LogMessage = null;
                m_StackTrack = null;
                m_DisplayText = null;
            }
        }
    }
}
