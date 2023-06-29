using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CSWPF.Directory;

public static class Log
    {
        private static object _semaphore = new object();

        public static bool IsDebug { get; set; }

        public static string Folder { get; set; }

        public static void WriteException(Exception ex)
        {
            if (!IsDebug)
            {
                return;
            }

            try
            {
                Write(string.Empty);
                Write("*** EXCEPTION ***");
                do
                {
                    WriteExceptionContent(ex);
                    ex = ex.InnerException;
                    if (ex != null)
                    {
                        Write("--------------------");
                    }
                }
                while (ex != null);
                Write("*** EXCEPTION ***");
                Write(string.Empty);
            }
            catch
            {
            }
        }

        private static void WriteExceptionContent(Exception ex)
        {
            Write(ex.Message);
            Write(ex.StackTrace);
        }

        public static void WriteExceptionShort(Exception ex)
        {
            if (IsDebug)
            {
                try
                {
                    Write("*** EXCEPTION ***");
                    Write(ex.Message);
                    Write("*** EXCEPTION ***");
                }
                catch
                {
                }
            }
        }

        public static void Write(string msg)
        {
            if (!IsDebug)
            {
                return;
            }

            lock (_semaphore)
            {
                try
                {
                    int managedThreadId = Thread.CurrentThread.ManagedThreadId;
                    FileInfo fileInfo = ((!string.IsNullOrEmpty(Folder)) ? new FileInfo(Folder) : new FileInfo(Application.ExecutablePath));
                    string text = DateTime.Now.Year.ToString("00") + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00") + ".log";
                    using StreamWriter streamWriter = File.AppendText(fileInfo.Directory?.ToString() + "\\" + text);
                    Encoding uTF = Encoding.UTF8;
                    msg = $"[Th:{managedThreadId}] {msg}";
                    byte[] bytes = uTF.GetBytes(msg);
                    streamWriter.Write(DateTime.Now.ToLongTimeString() + "   " + uTF.GetString(bytes) + "\n");
                    streamWriter.Flush();
                }
                catch
                {
                }
                finally
                {
                    _semaphore = new object();
                }
            }
        }

        public static void Write(string[] msg)
        {
            if (!IsDebug)
            {
                return;
            }

            lock (_semaphore)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(Application.ExecutablePath);
                    string text = DateTime.Now.Year.ToString("00") + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00") + ".log";
                    using StreamWriter streamWriter = File.AppendText(fileInfo.Directory?.ToString() + "\\" + text);
                    Encoding uTF = Encoding.UTF8;
                    foreach (string s in msg)
                    {
                        byte[] bytes = uTF.GetBytes(s);
                        streamWriter.Write(DateTime.Now.ToLongTimeString() + "   " + uTF.GetString(bytes) + "\n");
                    }

                    streamWriter.Flush();
                }
                catch
                {
                }
                finally
                {
                    _semaphore = new object();
                }
            }
        }
    }