using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class OrderSuccess
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ColorName { get; set; }
        public decimal Price { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public string ProductCode { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string StoreAddress { get; set; }
        public int Method { get; set; }
        public string MethodName { get; set; }
        public bool IsPayment { get; set; }
        public string LinkRedirect { get; set; }
        public string Code { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsKey { get; set; }
        public string PhoneNumber { get; set; }
        public string CategoryName { get; set; }
        public string Syntax { get; set; }
        public bool IsPaymentSuccess { get; set; }
        public string CampaignName { get; set; }
        public string DateOffPaid { get; set; }
        public string DateReceived { get; set; }
        public string Url { get; set; }
    }
}