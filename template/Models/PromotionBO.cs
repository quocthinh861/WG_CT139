using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace template.Models
{
    [Serializable]
    public class PromotionBO
    {
        public int PromotionId { get; set; }
        public int ProductId { get; set; }
        public string PromotionName { get; set; }
        public string PromotionImageDesk { get; set; }
        public string PromotionImageMobile { get; set; }
        public string Url { get; set; }
        public int OrderBy { get; set; }
        public bool IsChoose { get; set; }
    }
}