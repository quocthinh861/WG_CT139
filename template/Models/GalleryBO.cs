using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace template.Models
{
    [Serializable]
    public class GalleryBO
    {
        public int GalleryId { get; set; }
        public int ProgramId { get; set; }
        public string ThumbImage { get; set; }
        public string ZoomImage { get; set; }
        public int OrderBy { get; set; }
    }
}