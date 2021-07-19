using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class Comment
    {
        public int ObjectId { get; set; }
        public int ObjectType { get; set; }
        public string CampaignName { get; set; }
        public string Url { get; set; }
        public bool IsMobile { get; set; }
        public int SiteId { get; set; }
    }
}