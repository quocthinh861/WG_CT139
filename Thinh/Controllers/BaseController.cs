using dienmayxanh.lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using TGDD.Library.Caching;
using thegioididong.business.ext;
using thegioididong.business.ext.v2020;
using thegioididong.business.extensions;
using thegioididong.business.helper;
using thegioididong.business.viewModels;

namespace Thinh.Controllers
{
    public class BaseController : Controller
    {
        #region Lấy thông tin sản phẩm
        private static int _programId = 139;
        private static bool _isChangePhase = false;
        public static int GameType = 169;
        /// <summary>
        /// Danh sách sản phẩm show chữ GIÁ TỐT
        /// </summary>
        public static List<int> ProductIdsText = new List<int>() { 162326 };
        /// <summary>
        /// Danh sách sản phẩm giao hàng từ 5-7 ngày
        /// </summary>
        private List<int> ProductIdsEx = new List<int>() { };

        public List<Models.Product> BindingProduct(bool isRemoveCache = false)
        {
            var cacheKey = string.Format("{0}{1}_PRODUCT", UrlPath, SiteId);
            if (isRemoveCache) System.Web.HttpContext.Current.Cache.Remove(cacheKey);
            var products = System.Web.HttpContext.Current.Cache.Get(cacheKey) as List<Models.Product>;
            if (products != null && products.Count > 0) return products;

            var tempProducts = InitProduct(isRemoveCache);
            var productIds = string.Join(",", tempProducts.Select(p => p.ProductId));
            var pros = thegioididong.business.api.ApiProductRepo.Current.GetListProductByListIDLDPOnly(productIds, isRemoveCache);

            var res = new List<Models.Product>();
            foreach (var item in pros)
            {
                #region Kiểm tra thông tin sản phẩm
                if (item == null) continue;
                //if (item.productErpPriceBOField == null || item.productErpPriceBOField.priceField <= 0) continue;
                //if (thegioididong.business.ext.ExtProduct.GetTypeProductNotGame(item, IsRemoveCache) > 0) continue;
                //if (!(item.productErpPriceBOField.webStatusIdField != 9 && new List<int> { 2, 3, 4, 6, 8 }.Contains(item.productErpPriceBOField.webStatusIdField))) { continue; }
                #endregion

                var objProduct = new Models.Product();

                #region Thông tin sản phẩm cơ bản
                var url = SEOHelper.GenSEOProductUrl(item);
                if (MvcApplication.IsDMX()) url = url.Replace("/dtdd/", "/dien-thoai/");

                objProduct.ProductId = item.productIDField;
                objProduct.ProductName = item.Name();
                objProduct.ProductShortName = !string.IsNullOrEmpty(item.shortNameField) ? item.shortNameField : item.productNameField;
                objProduct.NavigationUrl = url;
                objProduct.BuyUrl = string.Format("{0}?m=-1", UrlCampaign);
                objProduct.ShortSpecification = item.GetShortSpecification().Replace("<ul>", "<ul class=\"parameter\">").Replace("ibsim", "ibsim hide");
                objProduct.FullSpecification = GetFullSpec(item);
                objProduct.InstallmentUrl = string.Format("{0}?m=6", UrlCampaign);
                objProduct.InstallmentUrlMain = "/tra-gop" + url;
                objProduct.InstallmentUrlCreditCard = "/tra-gop" + url + "?m=credit";
                objProduct.SiteId = SiteId;

                #region Xóa link trong thông số kĩ thuật
                if (!IsTGDD) {
                    objProduct.ShortSpecification = RemoveHtmlTag(objProduct.ShortSpecification);
                    //objProduct.FullSpecification = RemoveHtmlTag(objProduct.FullSpecification);
                }
                #endregion
                #endregion

                #region Lấy thông tin khai báo sản phẩm
                var pro = tempProducts.FirstOrDefault(p => p.ProductId == item.productIDField);
                if (pro == null) continue;
                objProduct.DisplayOrder = pro.OrderBy;
                objProduct.CRMProgramId = pro.CRMProgramId;
                objProduct.Quantity = pro.Quantity;
                objProduct.ViewPrice = pro.DiscountPrice;
                objProduct.SalePrice = pro.DepositMoney;
                objProduct.IsShow = pro.IsShow;
                objProduct.IsChangePhase = pro.IsChangePhase;

                #region Lấy thông tin sản phẩm cho iPhone
                if (pro.GroupId > 0)
                {
                    objProduct.GroupId = pro.GroupId;
                    objProduct.FirstName = pro.FirstName;
                    objProduct.LastName = pro.LastName;
                    objProduct.IsShow = pro.IsShow;
                }
                #endregion
                #endregion

                #region Lấy thông tin khai báo màu
                if (objProduct.Colors == null || objProduct.Colors.Count == 0)
                    objProduct.Colors = thegioididong.business.api.ApiProductRepo.Current.GetProductColorByProductID(objProduct.ProductId, IsRemoveCache).OrderByDescending(p => p.productCodeField).ToList();
                if (objProduct.Colors != null && objProduct.Colors.Count > 0)
                {
                    var colors = new List<thegioididong.business.ApiProduct.ProductColorBO>();
                    var productCodes = InitProductCode(isRemoveCache);
                    foreach (var objColor in objProduct.Colors)
                    {
                        var objCode = productCodes.FirstOrDefault(p => p.Code.Trim() == objColor.productCodeField.Trim());
                        if (objCode == null) continue;

                        objColor.isExistField = false;
                        objColor.statusField = objCode.Quantity; //K có cột tương ứng nên lấy cột Status xài tạm để gắn số lượng
                        objColor.categoryIDField = objCode.CRMProgramId; //K có cột tương ứng nên lấy cột categoryId xài tạm để gắn mã CRM
                        if (objCode.CRMProgramId > 0)
                        {
                            var crmProgramIds = new List<int>() { objCode.CRMProgramId };
                            var salePrograms = thegioididong.business.svc.SvcCrmRepo.Current.GetSaleProgramInfoByListID(crmProgramIds.ToArray(), IsRemoveCache);
                            if (salePrograms != null && salePrograms.Any())
                            {
                                var objSaleProgram = salePrograms.FirstOrDefault();
                                objColor.isExistField = (objCode.Quantity - (objSaleProgram.MaxQuantity - objSaleProgram.CurrentQuantity)) <= 0;
                            }
                        }

                        objColor.pictureField = objCode.Image;
                        objColor.iconField = string.Format("{0}&c={1}", objProduct.InstallmentUrlCreditCard, objColor.productCodeField);
                        colors.Add(objColor);
                    }
                    objProduct.Colors = colors;
                    objProduct.IsLimitColor = colors.Any(p => p.statusField > 0);
                    //objProduct.CRMProgramIds = objProduct.CRMProgramId.ToString() + (objProduct.IsLimitColor ? "," + string.Join(",", colors.Select(p => p.categoryIDField)) : "");
                    objProduct.CRMProgramIds = objProduct.IsLimitColor ? string.Join(",", colors.Select(p => p.categoryIDField)) : objProduct.CRMProgramId.ToString();
                }
                #endregion

                #region Lấy thông tin khai báo khuyến mãi
                var promotions = InitPromotion(isRemoveCache).Where(p => p.ProductId == objProduct.ProductId);
                if (promotions != null && promotions.Any())
                    objProduct.Promotions = promotions.OrderBy(p => p.OrderBy).ToList();
                #endregion

                #region Lấy danh sách khách hàng
                if (IsSMS)
                {
                    int totalSuccess = 0;
                    int totalRecord = 0;
                    int totalSMS = 0;
                    var listOrder = thegioididong.business.svc.SvcCrmCustomerRepo.Current.GetOrderByPromotionSMS("", 0, 1, PageSize, objProduct.CRMProgramId.ToString(), LaunchingFromDate, LaunchingToDate, ref totalSuccess, ref totalRecord, ref totalSMS, isRemoveCache);
                    objProduct.TotalOrder = totalRecord;
                    objProduct.TotalPaid = totalSMS;
                    //Sản phẩm preorder
                }
                else
                {
                    int totalCount = 0;
                    int totalRecordPaid = 0;
                    var listUser = thegioididong.business.svc.SvcCrmCustomerRepo.Current.GetAllSaleOrderByProgramCRM(Program.FromDate, Program.ToDate, "", objProduct.CRMProgramId.ToString(), PageSize, 1, -1, ref totalCount, ref totalRecordPaid, isRemoveCache);
                    objProduct.TotalOrder = (Program.TotalOrder > 0 ? Program.TotalOrder : totalCount) + pro.QuantityPlus;
                    objProduct.TotalPaid = (Program.TotalPaid > 0 ? Program.TotalPaid : totalRecordPaid) + pro.QuantityPlus;
                }
                #endregion

                #region Giao diện màn hình đầu
                objProduct.BgDesk = pro.BgDesk;
                objProduct.BgMobile = pro.BgMobile;
                objProduct.TitleDesk = pro.TitleDesk;
                objProduct.TitleMobile = pro.TitleMobile;
                objProduct.ImageDesk = pro.ImageDesk;
                objProduct.ImageMobile = pro.ImageMobile;
                #endregion

                #region Xử lý dữ liệu
                objProduct.Price = item.productErpPriceBOField == null || item.productErpPriceBOField.priceField == 0
                     ? pro.ExpectedPrice
                     : item.productErpPriceBOField.priceField;
                objProduct.HisPrice = item.productErpPriceBOField == null ? 0 : item.productErpPriceBOField.priceField;
                if (objProduct.ViewPrice > 0 && item.productErpPriceBOField != null)
                {
                    objProduct.Price = item.productErpPriceBOField.priceField - objProduct.ViewPrice;
                }
                if (objProduct.Promotions != null && objProduct.Promotions.Any())
                    objProduct.IsInstallment0Percent = objProduct.Promotions.Any(p => p.PromotionName.ToLower().Contains("trả góp 0%"));
                objProduct.IsInstallmentCredit = Program.IsInstallmentCredit;
                objProduct.IsInstallment0PercentCredit = Program.IsInstallment0PercentCredit;

                #region Thời gian nhận hàng
                if (pro.Quantity == 27) //Từ 2-7 ngày
                {
                    objProduct.IsFrom2To7 = true;
                    objProduct.DeliveryName = "2-7";
                    pro.Quantity = 99999;
                }
                else if (pro.Quantity == 57) //Từ 5-7 ngày
                {
                    objProduct.IsFrom5To7 = true;
                    objProduct.DeliveryName = "5-7";
                    pro.Quantity = 99999;
                }
                else if (Program.IsNormalSelling)  //Sản phẩm online only | bán thường
                {
                    objProduct.IsOnlineOnly = true;
                    objProduct.BuyUrl = string.Format("/cart/them-vao-gio-hang?productid={0}", objProduct.ProductId);
                    objProduct.InstallmentUrl = "/tra-gop" + url;
                }
                #endregion

                objProduct.IsNormalSale = pro.Quantity == 99999 || ProductIdsEx.Any(x => x == objProduct.ProductId);
                objProduct.Text = ProductIdsText.Any(x => x == objProduct.ProductId) ? "tốt" : "bán";

                var totalRemain = objProduct.Quantity - objProduct.TotalPaid;
                objProduct.TotalRemain = totalRemain > 0 ? totalRemain : 0;
                objProduct.IsSoldOut = totalRemain < 1;
                if (objProduct.IsSoldOut) objProduct.TotalPaid = objProduct.Quantity;

                objProduct.IsOn = IsOnPreOrder;
                objProduct.IsOff = IsOffPreOrder;
                objProduct.FromDate = Program.FromDate;
                objProduct.ToDate = Program.ToDate;
                objProduct.FromDateReceived = Program.FromDateReceived;
                objProduct.ToDateReceived = Program.ToDateReceived;
                #endregion

                res.Add(objProduct);
            }

            if (res != null && res.Count > 0) {
                res = res.OrderByDescending(p => p.Promotions != null).ThenBy(p => p.DisplayOrder).ToList();
            }

            System.Web.HttpContext.Current.Cache.Add(cacheKey, res, null, DateTime.Now.AddSeconds(30), TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Normal, null);

            return res;
        }
        public static Models.ProgramBO Program
        {
            get
            {
                var objProgram = thegioididong.business.svc.SvcGameRepo.Current.PreOrderProgramGetById(_programId, IsRemoveCache);
                objProgram.MetaTitle = ReplaceDomainName(objProgram.MetaTitle);
                objProgram.MetaDescription = ReplaceDomainName(objProgram.MetaDescription);

                return new Models.ProgramBO()
                {
                    ProgramId = objProgram.ProgramId,
                    ProgramName = objProgram.ProgramName,
                    Url = objProgram.Url,
                    UrlRedirect = objProgram.RedirectUrl,
                    IsShowDetail = objProgram.IsDetailShowWeb,
                    IsInstallmentCredit = objProgram.IsAlepayinstallment,
                    IsInstallment0PercentCredit = objProgram.IsFreeAlepayInstallment,
                    HtmlId = objProgram.HtmlId,
                    KeywordNew = objProgram.NewsKeyword,
                    VideoId = objProgram.YoutubeId,
                    CommentId = objProgram.CommentId,
                    FromDate = objProgram.BeginDate,
                    ToDate = objProgram.EndDate,
                    FromDateReceived = objProgram.ReceivedBeginDate,
                    ToDateReceived = objProgram.ReceivedEndDate,
                    CRMProgramIdTeaser = objProgram.TeaserCRMId,
                    TotalOrder = objProgram.SaleOrderCount,
                    TotalPaid = objProgram.DepositCount,
                    MetaTitle = objProgram.MetaTitle,
                    MetaDescription = objProgram.MetaDescription,
                    MetaKeyword = objProgram.MetaKeyword,
                    Thumbnail = objProgram.ThumbnailImage,
                    BackgroundColor = objProgram.BackGroundColor,
                    TextColor = objProgram.TextColor, 
                    StoreIdList = objProgram.StoreIdList,
                    BackgroundVideoDesk = objProgram.BackgroundVideoDesk,
                    BackgroundVideoMobi = objProgram.BackgroundVideoMobi,
                    CSSDesk = objProgram.CSSDesk,
                    CSSMobi = objProgram.CSSMobi,
                    RegisterBeginDate = objProgram.Registerbegindate,
                    RegisterEndDate = objProgram.Registerenddate,
                    IsNormalSelling = objProgram.IsNormalSelling
                };
            }
        }
        public List<Models.ProductBO> InitProduct(bool isRemoveCache = false)
        {
            var products = thegioididong.business.svc.SvcGameRepo.Current.PreOrderProductListByProgramId(_programId, isRemoveCache);
            if (products == null || !products.Any()) return null;

            //Lấy sản phẩm theo site
            var siteId = (int)MvcApplication.Site.TGDD;
            products = products.Where(p => p.SiteId == siteId).ToList();
            if (products == null || !products.Any()) return null;

            var res = new List<Models.ProductBO>();
            foreach (var item in products.OrderBy(p => p.DisplayOrder)) {
                res.Add(new Models.ProductBO() {
                    Id = item.ProgramProductId,
                    ProgramId = item.ProgramId,
                    ProductId = item.ProductId,
                    CRMProgramId = item.CRMId,
                    Quantity = item.Quantity,
                    ExpectedPrice = item.ExpectedPrice,
                    DiscountPrice= item.DiscountPrice,
                    DepositMoney = item.DepositMoney,
                    OrderBy = item.DisplayOrder,
                    QuantityPlus = item.MoreDepositQuantity,

                    //Cấu hình dung lượng
                    GroupId = item.GroupId,
                    FirstName = item.GroupProductName,
                    LastName = item.CapacityName,
                    IsShow = item.IsShowWeb,

                    //Cấu hình giao diện
                    BgDesk = item.BackgroundDesktopImage,
                    BgMobile = item.BackgroundMobileImage,
                    ImageDesk = item.ProductDesktopImage,
                    ImageMobile = item.ProductMobileImage,
                    TitleDesk = item.TitleDesktopImage,
                    TitleMobile = item.TitleMobileImage
                });
            }

            #region Chuyển ngữ cảnh giai đoạn 2
            if (_isChangePhase)
            {
                var phase1Products = res.Where(p => p.OrderBy < 10).ToList();
                var phase2Products = res.Where(p => p.OrderBy > 10).ToList();
                if (phase2Products != null && phase2Products.Any())
                {
                    return phase2Products;
                }
                return phase1Products;
            }
            #endregion

            return res.Where(p => p.OrderBy < 10).ToList();
        }
        public List<Models.ProductCodeBO> InitProductCode(bool isRemoveCache = false)
        {
            var products = InitProduct(isRemoveCache);
            var productCodes = thegioididong.business.svc.SvcGameRepo.Current.PreOrderProductCodeListByProgramId(_programId, IsRemoveCache);
            var res = new List<Models.ProductCodeBO>();
            if (productCodes == null || !productCodes.Any()) return res;

            var idx = 0;
            foreach (var item in productCodes) {
                var objProduct = products.FirstOrDefault(p => p.Id == item.ProgramProductId);
                if (objProduct == null) continue;

                var image = item.Image;
                var crmProgramId = -1;
                var quantity = -1;
                if (image.Contains("#"))
                {
                    var arr = image.Split('#');
                    if (arr.Length > 0) image = arr[0];
                    if (arr.Length > 1) int.TryParse(arr[1], out crmProgramId);
                    if (arr.Length > 2) int.TryParse(arr[2], out quantity);
                }

                res.Add(new Models.ProductCodeBO()
                {
                    Id = idx++,
                    ProductId = objProduct.ProductId,
                    Code = item.ProductCode,
                    Image = image,
                    Quantity = quantity,
                    CRMProgramId = crmProgramId
                });
            }
            return res;
        }
        public List<Models.PromotionBO> InitPromotion(bool isRemoveCache = false)
        {
            var products = InitProduct(isRemoveCache);
            var promotions = thegioididong.business.svc.SvcGameRepo.Current.PreOrderPromotionListByProgramId(_programId, IsRemoveCache);
            var res = new List<Models.PromotionBO>();
            if (promotions == null || !promotions.Any()) return res;
            foreach (var item in promotions)
            {
                var objProduct = products.FirstOrDefault(p => p.Id == item.ProgramProductId);
                if (objProduct == null) continue;
                res.Add(new Models.PromotionBO() {
                    PromotionId = item.ProgramPromotionId,
                    ProductId = objProduct.ProductId,
                    PromotionName = item.PromotionName,
                    PromotionImageDesk = item.DesktopImage,
                    PromotionImageMobile = item.MobileImage,
                    Url = item.Url,
                    OrderBy = item.DisplayOrder,
                    IsChoose = item.IsChoicePromotion
                });
            }
            return res.OrderBy(p => p.OrderBy).ToList();
        }
        public List<Models.KVSBO> InitKVS(bool isRemoveCache = false)
        {
            var res = new List<Models.KVSBO>();
            var kvss = thegioididong.business.svc.SvcGameRepo.Current.PreOrderKVSSearch(_programId, isRemoveCache);
            if (kvss == null || !kvss.Any()) return res;
            foreach (var item in kvss)
            {
                res.Add(new Models.KVSBO()
                {
                    KVSId = item.SpecId,
                    ProgramId = item.ProgramId,
                    Title = item.Title,
                    Content = item.ContentDescription,
                    OrderBy = item.DisplayOrder,
                    ImageDesk = item.DesktopImage,
                    ImageMobile = item.MobileImage,
                    Location = item.ShowWebImagePosition,
                    BgColor = item.BackgroundColor,
                    BgImageDesk = item.BackgroundDesktopImage,
                    Height = item.BackgroundHeight,
                    BgImageMobile = item.BackgroundMobileImage
                });
            };

            return res.OrderBy(p => p.OrderBy).ToList();
        }
        public List<Models.GalleryBO> InitGallery(bool isRemoveCache = false)
        {
            var res = new List<Models.GalleryBO>();
            var galleries = thegioididong.business.svc.SvcGameRepo.Current.PreOrderGallerySearch(_programId, isRemoveCache);
            if (galleries == null || !galleries.Any()) return res;
            foreach (var item in galleries)
            {
                res.Add(new Models.GalleryBO()
                {
                    GalleryId = item.GalleryId,
                    ThumbImage = item.ThumbImage,
                    ZoomImage = item.ZoomImage,
                    OrderBy = item.DisplayOrder
                });
            }
            return res.OrderBy(p => p.OrderBy).ToList();
        }
        private string RemoveHtmlTag(string input, string tagName = "a")
        {
            var regex = new Regex(string.Format("<{0}[^>].*?>(?<key>.*?)</{0}>", tagName), RegexOptions.Singleline);
            foreach (Match m in regex.Matches(input))
            {
                var val = m.ToString();
                if (val.ToLower().Contains("mua sim") && MvcApplication.IsDMX()) continue;
                input = input.Replace(val, m.Groups["key"].Value);
            }
            return MvcApplication.IsDMX() ? input.Replace("//www.thegioididong.com/", "//www.dienmayxanh.com/") : input;
        }
        private static string ReplaceDomainName(string text)
        {
            var newText = "";
            if (MvcApplication.IsDMX()) newText = "Điện máy XANH";

            return string.IsNullOrEmpty(newText)
                ? text
                : text.Replace("thế giới di động", newText)
                      .Replace("thegioididong.com", newText)
                      .Replace("thegioididong", newText)
                      .Replace("Thế Giới Di Động", newText)
                      .Replace("Thegioididong.com", newText)
                      .Replace("Thegioididong", newText);
        }
        private string GetFullSpec(thegioididong.business.ApiProduct.ProductBO objProduct)
        {
            var productID = objProduct.productIDField;
             
            ViewModelSpecificationParams result = new ViewModelSpecificationParams
            {
                ProductID = productID,
                ProductImageUrl = SEOHelper.GenImageKitUrl(objProduct.productCategoryBOField.categoryIDField, objProduct.productIDField, objProduct.specificationimageField)
            };

            // lay danh sach cac group thuoc tinh san pham
            var grpbo = thegioididong.business.api.ApiProductRepo.Current.GetListProductGroupByCategoryID(
                    objProduct.productCategoryBOField.categoryIDField,
                    thegioididong.business.helper.ConfigHelper.LangID,
                    thegioididong.business.helper.ConfigHelper.SiteID,
                    IsRemoveCache
                );

            if (grpbo == null) return string.Empty;

            // lay tat cac ca thuoc tinh san pham
            var productDetail = thegioididong.business.api.ApiProductRepo.Current.GetProductDetailByProductID(productID, IsRemoveCache);
            if (productDetail == null)
                productDetail = new List<thegioididong.business.ApiProduct.ProductDetailBO>();

            // khoi tao cac group thuoc tinh san pham
            foreach (var group in grpbo)
            {
                SpecificationParamsGroup specGroup = new SpecificationParamsGroup
                {
                    GroupName = group.groupNameField,
                    GroupId = group.groupIDField,
                    DisplayOrder = group.displayOrderField
                };

                result.lstParams.Add(specGroup);
            }

            // them cac thuoc tinh san pham vao dung loai group
            foreach (var prop in productDetail)
            {
                foreach (var specGroup in result.lstParams)
                {
                    if (specGroup.GroupId == prop.groupIDField && prop.CheckShowByPropValueInFullSpec())
                    {
                        var isOff = false;
                        var html = thegioididong.business.helper.ConfigHelper.GetHtmlInfoByID(1514);
                        if (!string.IsNullOrEmpty(html))
                        {
                            //html += ",ad2066020";
                            for (int i = 0; i < html.Split(',').Length; i++)
                            {
                                if (prop.propValueField.Trim().Equals(html.Split(',')[i]))
                                {
                                    isOff = true;
                                    break;
                                }
                            }
                        }
                        if (!isOff)
                        {
                            // them param vao group neu chua co, nguoc lai them cac value cua param do
                            var findParam = specGroup.lstParams.FirstOrDefault(f => f.PropId == prop.propertyIDField);
                            var link = new SpecificationParamValueLink
                            {
                                PropValue = prop.propValueField,
                                PropValueLink = prop.propValueLinkField
                            };
                            if (findParam != null && findParam.Links.Count > 0)
                            {
                                findParam.Links.Add(link);
                            }
                            else
                            {
                                var param = new SpecificationParam
                                {
                                    PropId = prop.propertyIDField,
                                    PropName = prop.propertyNameField,
                                    PropUrl = prop.propUrlField,
                                    DisplayOrder = prop.propertyDisplayOrderField,
                                };
                                param.Links.Add(link);
                                specGroup.lstParams.Add(param);
                            }
                        }
                    }
                }
            }

            return RenderPartialViewToString("~/Views/Common/_PartialFullSpec.cshtml", result);
        }
        #endregion

        #region Khai báo chung cho mỗi campaign
        public bool IsOrder = true;
        public int PageSize = 20;
        public static string UrlPath = Program.Url;
        public int SiteId = MvcApplication.SiteId;
        public static bool IsTGDD = MvcApplication.IsTGDD();
        public static string FrontEndUrl = MvcApplication.FrontEndUrl;
        public static string UrlCampaign
        {
            get
            {
                if (!IsRunLive) return DomainName + UrlPath;
                if (!Program.IsShowDetail) return FrontEndUrl + UrlPath;
                var categoryUrl = "/dien-thoai";
                if (MvcApplication.IsTGDD()) categoryUrl = "/dtdd";
                return FrontEndUrl + categoryUrl + Program.Url;
            }
        }
        public static string Hotline(bool isPhone = false)
        {
            var hotline = "1800.1060";
            if (MvcApplication.IsDMX()) hotline = "1800.1061";
            return isPhone ? "tel:" + hotline.Replace(".", "") : hotline;
        }

        #region Cấu hình comment
        public int ObjectId = Program.CommentId;
        public int ObjectType = 5; //Dành riêng cho campaign
        public string CampaignName = Program.ProgramName;
        #endregion

        /// <summary>
        /// Gen HTML thẻ metadata
        /// </summary>
        /// <returns></returns>
        public static string GenMetaTags()
        {
            var thumbnail = "";
            if (!string.IsNullOrEmpty(Program.Thumbnail)) {
                var arr = Program.Thumbnail.Split('|');
                if (arr.Length > 0 && MvcApplication.IsTGDD()) { thumbnail = arr[0]; }
                else if (arr.Length > 1 && MvcApplication.IsDMX()) { thumbnail = arr[1]; }
            }
            return ViewHelper.MetaTags(Program.MetaTitle, Program.MetaDescription, Program.MetaKeyword, thumbnail, UrlCampaign);
        }

        #region Cấu hình đặt hàng
        /// <summary>
        /// Campaign đặt hàng có SMS hay không?
        /// </summary>
        public static bool IsSMS = false;

        /// <summary>
        /// Trạng thái đơn hàng: 1: Chờ xử lý || 2: Đang xử lý || 3: Tiềm năng || 4: Không bắt máy || 5: Hủy
        /// </summary>
        public int StatusId = 1;// IsRunLive && !IsSMS ? 1 : 3;

        //Session đặt hàng cho từng campaign (nhớ đổi)
        public string SS_ORDER_CAMPAIGN = string.Format("SS_ORDER_PREORDER_{0}", _programId);
        public string SS_PAYMENT_CAMPAIGN = string.Format("SS_PAYMENT_PREORDER_{0}", _programId);
        public string SS_CAPTCHA_CAMPAIGN = string.Format("SS_CAPTCHA_PREORDER_{0}", _programId);
        public static string SS_GAME_CAMPAIGN = string.Format("SS_GAME_CAMPAIGN_{0}", _programId);

        /// <summary>
        /// Các hình thức thanh toán
        /// </summary>
        public enum MethodType
        {
            /// <summary>
            /// Nhận hàng/cọc tại siêu thị
            /// </summary>
            AtStore = 1,
            /// <summary>
            /// Không sử dụng
            /// </summary>
            VISA = 2,
            /// <summary>
            /// Nhận hàng/cọc tại nhà
            /// </summary>
            AtHome = 3,
            /// <summary>
            /// Thanh toán trực tuyến ATM
            /// </summary>
            ATM = 4,
            /// <summary>
            /// Thanh toán trực tuyến VISA của 123Pay
            /// </summary>
            VISA123PAY = 5,
            /// <summary>
            /// Trả góp
            /// </summary>
            Installment = 6,
            /// <summary>
            /// Trả góp thẻ tín dụng
            /// </summary>
            InstallmentCredit = 7
        }
        public string GetMethodNameByType(int methodType)
        {
            var text = IsRunPreOrder ? "Đặt cọc" : "Nhận hàng";
            if (methodType == (int)MethodType.AtStore) return text + " tại siêu thị";
            if (methodType == (int)MethodType.AtHome) return text + " tại nhà";
            if (methodType == (int)MethodType.Installment) return "Trả góp";

            return "Thanh toán trực tuyến";
        }

        #region Cấu hình ngày bắt đầu + kết thúc đặt hàng/game
        //Dùng chung cho game/đặt hàng SMS
        public static DateTime FromDate = new DateTime(2017, 03, 31, 00, 00, 00);
        public static DateTime ToDate = new DateTime(2017, 04, 04, 00, 00, 00);

        //Dùng cho giai đoạn preorder
        public static DateTime LaunchingFromDate = new DateTime(2029, 06, 15, 23, 59, 59);
        public static DateTime LaunchingToDate = new DateTime(2029, 06, 30, 23, 59, 59);

        public static bool IsOn
        {
            get { return DateTime.Now > FromDate || (System.Web.HttpContext.Current.Request["view"] != null && System.Web.HttpContext.Current.Request["view"] == "on"); }
        }
        public static bool IsOff
        {
            get { return DateTime.Now > ToDate || (System.Web.HttpContext.Current.Request["view"] != null && System.Web.HttpContext.Current.Request["view"] == "off"); }
        }

        //Giai đoạn preorder
        public static bool IsOnPreOrder
        {
            get
            {
                return DateTime.Now > Program.FromDate || (System.Web.HttpContext.Current.Request.QueryString["view"] != null && System.Web.HttpContext.Current.Request.QueryString["view"] == "on-preorder");
            }
        }
        public static bool IsOffPreOrder
        {
            get
            {
                return DateTime.Now > Program.ToDate || (System.Web.HttpContext.Current.Request.QueryString["view"] != null && System.Web.HttpContext.Current.Request.QueryString["view"] == "off-preorder");
            }
        }
        public static bool IsRunPreOrder
        {
            get { return IsOnPreOrder && !IsOffPreOrder; }
        }

        //Giai đoạn bán chính thức
        public static bool IsOnLaunching
        {
            get
            {
                return DateTime.Now > LaunchingFromDate || (System.Web.HttpContext.Current.Request.QueryString["view"] != null && System.Web.HttpContext.Current.Request.QueryString["view"] == "on-launching");
            }
        }
        public static bool IsOffLaunching
        {
            get
            {
                return DateTime.Now > LaunchingToDate || (System.Web.HttpContext.Current.Request.QueryString["view"] != null && System.Web.HttpContext.Current.Request.QueryString["view"] == "off-launching");
            }
        }

        //Đơn hàng ngoài giờ
        public static bool Is9H30To23H59
        {
            get
            {
                var fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 21, 30, 00);
                var toDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                return fromDate <= DateTime.Now && DateTime.Now <= toDate;
            }
        }
        public static bool Is0H00To7H30
        {
            get
            {
                var fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00);
                var toDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 7, 30, 00);
                return fromDate <= DateTime.Now && DateTime.Now <= toDate;
            }
        }
        #endregion

        #endregion
        #endregion

        #region Khai báo chung sử dụng tất cả campaign
        /// <summary>
        /// Lấy ra ID tỉnh thành dựa vào cookies
        /// </summary>
        public int ProvinceId
        {
            get
            {
                return 3;
            }
        }

        public bool IsMobileMode
        {
            get
            {
                return MvcApplication.IsMobileMode() || IsFromMobileApp;
            }

        }
        /// <summary>
        /// Dùng để biết có phải mở bằng app trên điện thoại không
        /// </summary>
        public bool IsFromMobileApp
        {
            get
            {
                return MvcApplication.IsFromMobileApp();
            }
        }
        public static bool IsRemoveCache
        {
            get
            {
                return System.Web.HttpContext.Current.Request["ClearCache"] != null;
            }
        }

        public string GetUserIP()
        {
            if (System.Web.HttpContext.Current == null) return "127.0.0.1";
            var request = System.Web.HttpContext.Current.Request;
            var ip = (request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null
                     && request.ServerVariables["HTTP_X_FORWARDED_FOR"] != "")
                    ? request.ServerVariables["HTTP_X_FORWARDED_FOR"]
                    : request.ServerVariables["REMOTE_ADDR"];
            if (string.IsNullOrEmpty(ip))
                return "127.0.0.1";
            if (ip.Contains(","))
                ip = ip.Split(',')[0].Trim();
            return ip;
        }
        public string GetCookieOffer()
        {
            return System.Web.HttpContext.Current.Request.Cookies["_utm_thegioididong"] == null
                ? ""
                : System.Web.HttpContext.Current.Request.Cookies["_utm_thegioididong"].Value.ToString();
        }
        public string GetBrowserInfo()
        {
            var request = System.Web.HttpContext.Current.Request;
            if (request == null)
                return "";

            #region System

            String userAgent = request.UserAgent;
            var infoSystem = "";
            if (userAgent.IndexOf("Windows NT 6.3") > 0)
            {
                infoSystem = "Windows 8.1";
            }
            else if (userAgent.IndexOf("Windows NT 6.2") > 0)
            {
                infoSystem = "Windows 8";
            }
            else if (userAgent.IndexOf("Windows NT 6.1") > 0)
            {
                infoSystem = "Windows 7";
            }
            else if (userAgent.IndexOf("Windows NT 6.0") > 0)
            {
                infoSystem = "Windows Vista";
            }
            else if (userAgent.IndexOf("Windows NT 5.2") > 0)
            {
                infoSystem = "Windows Server 2003; Windows XP x64 Edition";
            }
            else if (userAgent.IndexOf("Windows NT 5.1") > 0)
            {
                infoSystem = "Windows XP";
            }
            else if (userAgent.IndexOf("Windows NT 5.01") > 0)
            {
                infoSystem = "Windows 2000, Service Pack 1 (SP1)";
            }
            else if (userAgent.IndexOf("Windows NT 5.0") > 0)
            {
                infoSystem = "Windows 2000";
            }
            else if (userAgent.IndexOf("Windows NT 4.0") > 0)
            {
                infoSystem = "Microsoft Windows NT 4.0";
            }
            else if (userAgent.IndexOf("Win 9x 4.90") > 0)
            {
                infoSystem = "Windows Millennium Edition (Windows Me)";
            }
            else if (userAgent.IndexOf("Windows 98") > 0)
            {
                infoSystem = "Windows 98";
            }
            else if (userAgent.IndexOf("Windows 95") > 0)
            {
                infoSystem = "Windows 95";
            }
            else if (userAgent.IndexOf("Windows CE") > 0)
            {
                infoSystem = "Windows CE";
            }
            else
            {
                infoSystem = "Others ";
            }

            #endregion

            return string.Format("Trình duyệt : {0}, Version : {1} , System : {2} ", request.Browser.Browser, request.Browser.Version, infoSystem);
        }
        public bool IsSSL()
        {
            string proto = Request.ServerVariables["X-Proto"];
            if (!string.IsNullOrEmpty(proto))
            {
                bool isSSL;
                if (bool.TryParse(proto, out isSSL))
                {
                    return isSSL;
                }
                return false;
            }
            return false;
        }
        public JsonResult ReturnJson(string error, int status = -1)
        {
            return Json(new
            {
                status = status,
                error = error
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Sử dụng kết hợp json
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual string RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = this.ControllerContext.RouteData.GetRequiredString("action");
            this.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = System.Web.Mvc.ViewEngines.Engines.FindPartialView(this.ControllerContext, viewName);
                var viewContext = new ViewContext(this.ControllerContext, viewResult.View, this.ViewData, this.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }
        public static string UrlRoot
        {
            get
            {
                var request = System.Web.HttpContext.Current.Request;
                return (request.Url.Scheme + "://" + request.Url.Host + ((request.Url.Port == 80 || request.Url.Port == 9000) ? "" : (":" + request.Url.Port)) + ((request.ApplicationPath == "/") ? "" : request.ApplicationPath));
            }
        }
        public static string DomainName
        {
            get { return UrlRoot.Replace("/dat-truoc", "").Replace(UrlPath, "/").TrimEnd('/'); }
        }
        public static bool IsRunLive
        {
            get
            {
                return System.Web.HttpContext.Current.Request.Url.ToString().Contains("www.thegioididong.com") ||
                    System.Web.HttpContext.Current.Request.Url.ToString().Contains("www.thegioididong.vn") ||
                   System.Web.HttpContext.Current.Request.Url.ToString().Contains("www.dienmayxanh.com") ||
                   System.Web.HttpContext.Current.Request.Url.ToString().Contains("www.vuivui.com");
            }

        }
        public void CreateCookie(string cookieName, string value)
        {
            var dateEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            var expires = string.IsNullOrEmpty(value) ? -1 : (dateEnd - DateTime.Now).TotalMinutes;
            var cookie = new HttpCookie(cookieName)
            {
                Value = value,
                Expires = DateTime.Now.AddMinutes(expires)
            };
            System.Web.HttpContext.Current.Response.Cookies.Add(cookie);
        }
        public thegioididong.business.SvcGame.GameUserBO ReloadUserInfo(int userId)
        {
            var objUser = thegioididong.business.svc.SvcGameRepo.Current.GameUserLoadInfo(userId);
            Session[SS_GAME_CAMPAIGN] = objUser;
            return objUser;
        }
        #endregion

        #region Header & footer TGDD + Điện máy XANH
        protected void GetGlobal()
        {
            dienmayxanh.lib.ConfigHelper.GetConfigurations();
            var global = dienmayxanh.adapter.WebApi.Instance.Get<dienmayxanh.adapter.models.GlobalContent>("Global");
            if (global != null)
            {
                var provinceName = dienmayxanh.adapter.models.Province.GetProvinceName(Personalize.CurrentPersonal.ProvinceId);
                if (IsMobileMode)
                {
                    provinceName = provinceName.Replace("TP.Hồ Chí Minh", "TP. HCM");
                }

                string header;
                if (Personalize.CurrentPersonal.Culture != CultureHelper.Culture1)
                {
                    header = global.Header.Replace("<span><i class=wico-down-mini></i></span>",
                        "<span>" + provinceName + " <i class=\"wico-down-mini\"></i></span>").Translate().TranslateUrl();
                }
                else
                {
                    header = global.Header.Replace("<span><i class=wico-down-mini></i></span>",
                        "<span>" + provinceName + " <i class=\"wico-down-mini\"></i></span>").Replace("class=menu-mnn", "class=menu-btnl").Translate().TranslateUrl();
                }

                ViewBag.Header = header;
                ViewBag.Footer = global.Footer;
                ViewBag.GlobalCss = global.Css;
                ViewBag.GlobalJs = global.Js;
            }
            else
            {
                Debug.WriteLine("Không gọi được Header, footer, global css & js");
            }
        }
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if ((MvcApplication.IsDMX()) && filterContext.Result is ViewResult)
            {
                var ns = filterContext.Controller?.GetType().Namespace;
                if (ns != null && !ns.StartsWith("dienmayxanh.webapi"))
                    GetGlobal();
            }
            base.OnActionExecuted(filterContext);
        }
        #endregion
    }
}