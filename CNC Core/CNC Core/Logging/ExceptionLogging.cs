using System;
using System.IO;

namespace CNC.Core.Logging
{
    /// <summary>  
    /// Summary description for ExceptionLogging  
    /// </summary>  
    public static class ExceptionLogging
    {

        private static String ErrorlineNo, Errormsg, extype, ErrorLocation;

        public static void SendErrorToText(Exception ex, string customMessage)
        {
            var line = Environment.NewLine + Environment.NewLine;
            if(ex ==  null) return;
            ErrorlineNo = ex.StackTrace == null ? "" : ex.StackTrace.Substring(ex.StackTrace.Length - 7, 7);
            Errormsg = ex.GetType().Name.ToString();
            extype = ex.GetType().ToString();
            string filepath =  System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorLog" + System.IO.Path.DirectorySeparatorChar);
            ErrorLocation = ex.Message.ToString();

            try
            {
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                filepath = filepath + DateTime.Today.ToString("dd-MM-yy") + ".txt";   //Text File Name
                if (!File.Exists(filepath))
                {
                    File.Create(filepath).Dispose();
                }
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    string error = "Log Written Date:" + " " + DateTime.Now.ToString() + line + "Error Line No :" + " " + ErrorlineNo + line + "Error Message:" + " " + Errormsg + line + "Exception Type:" + " " + extype + line + "Error Location :" + " " + ErrorLocation + line + "Custom Message:" + " " + customMessage;
                    sw.WriteLine("-----------Exception Details on " + " " + DateTime.Now.ToString() + "-----------------");
                    sw.WriteLine("-------------------------------------------------------------------------------------");
                    sw.WriteLine(error);
                    sw.WriteLine("--------------------------------*End*------------------------------------------");
                    sw.WriteLine(line);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

    }
}
