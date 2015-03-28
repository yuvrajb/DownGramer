using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DownGramer.App_Data.Entities
{
    public class AppSettings
    {
        public String Version = "1.0";
        public String DefaultImagePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\DownGramer\Downloads\Images\";
        public String DefaultVideoPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\DownGramer\Downloads\Videos\";
        public Boolean ScrollOnLoad = false;
    }
}
