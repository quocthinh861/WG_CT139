using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace template.Models
{
    [Serializable]
    public class ProductCodeBO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Code { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public int CRMProgramId { get; set; }
    }
}