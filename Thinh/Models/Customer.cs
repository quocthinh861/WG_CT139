using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class Customer
    {
        public string Keyword { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool IsPaid { get; set; }
        public int Total { get; set; }
        public int TotalPaid { get; set; }
        public int Model { get; set; }
        public bool IsShowViewMore { get; set; }
        public int TotalRemain { get; set; }
        public int Index { get; set; }
        public List<thegioididong.business.SvcCrmCustomer.CRMSALEPROGRAMCUSTOMER> Customers { get; set; }
        public List<Models.Product> Products { get; set; }
    }
}