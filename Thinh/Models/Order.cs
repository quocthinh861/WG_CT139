using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class Order
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public List<Product> Products { get; set; }
        public int Method { get; set; }
        public bool IsShowCaptcha { get; set; }
        public bool IsMobile { get; set; }
        public string UrlPath { get; set; }
        public bool IsOff { get; set; }
        public string Text { get; set; }
        public bool IsRunPreOrder { get; set; }
        public DateTime PreOrderToDate { get; set; }
        public int TotalOrder { get; set; }
        public int TotalPaid { get; set; }
        public bool IsSMS { get; set; }
        public thegioididong.business.helper.Cookie.CookieOrder Cookie { get; set; }
        public string Url { get; set; }
        public string ProgramName { get; set; }
        public string UserNameCTV { get; set; }

    }
}