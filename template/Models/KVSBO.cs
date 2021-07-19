using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace template.Models
{
    [Serializable]
    public class KVSBO
    {
        public int KVSId { get; set; }
        public int ProgramId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int OrderBy { get; set; }
        public string ImageDesk { get; set; }
        public string ImageMobile { get; set; }
        public int Location { get; set; }
        public string BgColor { get; set; }
        public string BgImageDesk { get; set; }
        public int Height { get; set; }
        public string BgImageMobile { get; set; }
    }
}