using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace template.Models
{
    [Serializable]
    public class ProductBO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ProgramId { get; set; }
        public int CRMProgramId { get; set; }
        public int Quantity { get; set; }
        public decimal ExpectedPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DepositMoney { get; set; }
        public int OrderBy { get; set; }
        public int GroupId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsShow { get; set; }
        public int QuantityPlus { get; set; }
        public string BgDesk { get; set; }
        public string BgMobile { get; set; }
        public string TitleDesk { get; set; }
        public string TitleMobile { get; set; }
        public string ImageDesk { get; set; }
        public string ImageMobile { get; set; }
        public bool IsChangePhase { get; set; }
    }
}