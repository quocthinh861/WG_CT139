using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class ProgramBO
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string Url { get; set; }
        public string UrlRedirect { get; set; }
        public bool IsShowDetail { get; set; }
        public bool IsInstallmentCredit { get; set; }
        public bool IsInstallment0PercentCredit { get; set; }
        public int HtmlId { get; set; }
        public string KeywordNew { get; set; }
        public string VideoId { get; set; }
        public int CommentId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime FromDateReceived { get; set; }
        public DateTime ToDateReceived { get; set; }
        public int CRMProgramIdTeaser { get; set; }
        public int TotalOrder { get; set; }
        public int TotalPaid { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeyword { get; set; }
        public string Thumbnail { get; set; }
        public bool IsChangePhase { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public string StoreIdList { get; set; }
        public string BackgroundVideoDesk { get; set; }
        public string BackgroundVideoMobi { get; set; }
        public string CSSDesk { get; set; }
        public string CSSMobi { get; set; }
        public DateTime RegisterBeginDate { get; set; }
        public DateTime RegisterEndDate { get; set; }
        public bool IsNormalSelling { get; set; }
    }
}