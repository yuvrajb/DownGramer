using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DownGramer.App_Data.Entities
{
    internal class LogException
    {
        public void EnterLog(Exception Ex)
        {
            lock (this)
            {
                String FileBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                FileBasePath += @"\DownGramer\Data\err_log.txt";

                FileStream LogFile = null;

                try
                {
                    if (File.Exists(FileBasePath))
                    {
                        LogFile = File.Open(FileBasePath, FileMode.Append, FileAccess.Write);
                    }
                    else
                    {
                        LogFile = File.Open(FileBasePath, FileMode.Create, FileAccess.Write);
                    }

                    StreamWriter Writer = new StreamWriter(LogFile);

                    String WriteData = DateTime.Now.ToString() + "\r\n" + Ex.ToString() + "\r\n============================================================================================================================\r\n\r\n";
                    Writer.WriteLine(WriteData);

                    Writer.Close();
                }
                finally
                {
                    if (LogFile != null)
                    {
                        LogFile.Close();
                    }
                }
            }
        }
    }
}
