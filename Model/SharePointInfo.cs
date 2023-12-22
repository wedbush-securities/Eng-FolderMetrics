using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Eng_FolderMetrics.Model
{
    public class SharePointInfo
    {
        public string? siteurl { get; set; }
        public string? username { get; set; }
        public SecureString? pass { get; set; }
        public string? fromfolder { get; set; }
        public string? tofolder { get; set; }
    }
}
