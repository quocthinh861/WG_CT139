using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace template.Models
{
    [Serializable]
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        /// <summary>
        /// Lay short Name, neu k co se gan lai ProductName
        /// </summary>
        public string ProductShortName { get; set; }
        public string BuyUrl { get; set; }
        public string InstallmentUrl { get; set; }
        public string InstallmentUrlMain { get; set; }
        public string InstallmentUrlCreditCard { get; set; }
        public string NavigationUrl { get; set; }
        public int CRMProgramId { get; set; }
        public int Quantity { get; set; }
        public int TotalOrder { get; set; }
        public int TotalPaid { get; set; }
        public int TotalRemain { get; set; }
        public decimal Price { get; set; }
        public decimal HisPrice { get; set; }
        public decimal ViewPrice { get; set; }
        // Gia du kien
        public decimal ExpectedPrice { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsShowExpectedPrice { get; set; }
        public string PromotionId { get; set; }
        public string SMSProductCode { get; set; }
        public bool IsOn { get; set; }
        public bool IsOff { get; set; }
        public bool IsInstallment0Percent { get; set; }
        public bool IsInstallmentCredit { get; set; }
        public bool IsInstallment0PercentCredit { get; set; }
        public List<thegioididong.business.ApiProduct.ProductColorBO> Colors { get; set; }
        public bool IsSoldOut { get; set; }
        public bool IsShow { get; set; }
        public int GroupId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<PromotionBO> Promotions { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        /// <summary>
        /// Ngày bắt đầu nhận hàng
        /// </summary>
        public DateTime FromDateReceived { get; set; }
        /// <summary>
        /// Ngày kết thúc nhận hàng
        /// </summary>
        public DateTime ToDateReceived { get; set; }
        public string ShortSpecification { get; set; }
        public string FullSpecification { get; set; }
        public string BgDesk { get; set; }
        public string BgMobile { get; set; }
        public string TitleDesk { get; set; }
        public string TitleMobile { get; set; }
        public string ImageDesk { get; set; }
        public string ImageMobile { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsChangePhase { get; set; }
        public int SiteId { get; set; }
        public string Text { get; set; }
        public bool IsNormalSale { get; set; }
        public bool IsFrom2To7 { get; set; }
        public bool IsFrom5To7 { get; set; }
        public string DeliveryName { get; set; }
        public bool IsOnlineOnly { get; set; }
        /// <summary>
        /// Sản phẩm có giới hạn màu theo mã CRM không?
        /// </summary>
        public bool IsLimitColor { get; set; }
        /// <summary>
        /// Danh sách mã CRM có giới hạn màu
        /// </summary>
        public string CRMProgramIds { get; set; }

    }
}