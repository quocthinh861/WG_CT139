using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinh.Models
{
    [Serializable]
    public class Store
    {
        public List<thegioididong.business.ApiCategory.ProvinceBO> Provinces { get; set; }
        public List<thegioididong.business.ApiCategory.DistrictBO> Districts { get; set; }
        public List<thegioididong.business.ApiCategory.StoreBO> Stores { get; set; }
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        public thegioididong.business.helper.Cookie.CookieOrder Cookie { get; set; }
        public int Method { get; set; }
        public List<Models.Product> Products { get; set; }
        public int Type { get; set; }
    }
}