using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class HomeVM
    {
        public Product Product { get; set; }
        public List<Product> Products { get; set; }
        public List<KVSBO> KVSs { get; set; }
        public List<GalleryBO> Gallerys { get; set; }
        public int TotalOrder { get; set; }
        public int TotalPaid { get; set; }
        public bool IsRemoveCache { get; set; }
        public bool IsShowStore { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
    }
}