using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace template.Controllers
{
    //[SessionState(System.Web.SessionState.SessionStateBehavior.ReadOnly)]
    public class HomeController : BaseController
    {
        #region View PAGE
        public ActionResult Index(string url = "")
        {
            #region Lấy số lượng đơn hàng
            var products = BindingProduct(IsRemoveCache);
            var objProduct = products.FirstOrDefault();
            var totalOrder = Program.TotalOrder;
            var totalPaid = Program.TotalPaid;
            if (totalOrder == 0 && totalPaid == 0) {
                var plus = InitProduct().Sum(p => p.QuantityPlus);
                var dtmFrom = Program.FromDate;
                var dtmTo = Program.ToDate;
                var crmProgramIds = string.Join(",", products.Select(p => p.CRMProgramIds));
                var listUser = thegioididong.business.svc.SvcCrmCustomerRepo.Current.GetAllSaleOrderByProgramCRM(dtmFrom, dtmTo, "", crmProgramIds, PageSize, 1, -1, ref totalOrder, ref totalPaid, IsRemoveCache);

                totalOrder += plus;
                totalPaid += plus;
            }
            #endregion

            #region Cập nhật lại tổng số lượng đơn hàng khi kết thúc chương trình
            if (IsOffPreOrder && Program.TotalOrder == 0 && Program.TotalPaid == 0) {
                var objProgram = thegioididong.business.svc.SvcGameRepo.Current.PreOrderProgramGet(Program.ProgramId);
                objProgram.SaleOrderCount = totalOrder;
                objProgram.DepositCount = totalPaid;
                var flag = thegioididong.business.svc.SvcGameRepo.Current.PreOrderProgramUpdate(objProgram);
            }
            #endregion

            #region Redirect khi kết thúc chương trình 7 ngày
            if (IsOffPreOrder &&
                !string.IsNullOrEmpty(Program.UrlRedirect) &&
                Program.IsShowDetail &&
                (DateTime.Now - Program.ToDate).TotalDays > 7)
            {
                Response.Redirect(Program.UrlRedirect);
            }
            #endregion

            #region Thanh toán trực tuyến
            if (Session[SS_PAYMENT_CAMPAIGN] != null)
            {
                var objOS = Session[SS_PAYMENT_CAMPAIGN] as Models.OrderSuccess;
                Session[SS_PAYMENT_CAMPAIGN] = null;
                return View("~/Views/Order/Payment.cshtml", objOS);
            }
            #endregion

            var objHomeVM = new Models.HomeVM()
            {
                Product = objProduct,
                Products = products,
                TotalOrder = totalOrder,
                TotalPaid = totalPaid,
                KVSs = InitKVS(IsRemoveCache),
                Gallerys = InitGallery(IsRemoveCache),
                IsShowStore = !string.IsNullOrEmpty(Program.StoreIdList),
                BackgroundColor = Program.BackgroundColor,
                TextColor = Program.TextColor
            };

            return View(IsMobileMode ? "Order.M" : "Order", objHomeVM);
        }
        #endregion

        #region Danh sách siêu thị trải nghiệm
        public ActionResult BoxStore(int provinceId = 3, int districtId = -1, int type = 0)
        {
            var cacheKey = string.Format("{0}{1}{2}_BoxStore", provinceId, districtId, type);
            if (IsRemoveCache) System.Web.HttpContext.Current.Cache.Remove(cacheKey);
            var objStore = System.Web.HttpContext.Current.Cache.Get(cacheKey) as Models.Store;
            if (objStore != null) return PartialView(objStore);
            var provinces = thegioididong.business.api.ApiCategoryRepo.Current.GetAllProvince();
            if (provinces != null) provinces = provinces.Where(p => p.provinceIDField > 0).OrderBy(p => p.orderIndexField).ToList();
            var districts = provinceId == -1
                ? new List<thegioididong.business.ApiCategory.DistrictBO>()
                : thegioididong.business.api.ApiCategoryRepo.Current.GetListDistrictByProvince(provinceId).ToList();

            #region Lấy tồn kho tự động
            //var stores = new List<thegioididong.business.ApiCategory.StoreBO>();
            //if (provinceId > 0)
            //{
            //    var total = 0;
            //    var products = BindingProduct();
            //    foreach (var item in products)
            //    {
            //        var storesInStock = thegioididong.business.api.ApiCategoryRepo.Current.GetStoreInStock2016(item.ProductId, "", provinceId, districtId, 300, 0, ref total, IsRemoveCache);
            //        if (storesInStock == null || !storesInStock.Any()) continue;
            //        stores.AddRange(storesInStock);
            //    }
            //}

            //var objStore = new Models.Store()
            //{
            //    Provinces = provinces,
            //    Districts = districts,
            //    Stores = stores,
            //    ProvinceId = provinceId,
            //    DistrictId = districtId
            //};
            #endregion

            #region Hardcode
            var arrStoreId = Program.StoreIdList.Split('|');
            var total = 0;
            var stores = provinceId == -1
                    ? new List<thegioididong.business.ApiCategory.StoreBO>()
                    : thegioididong.business.api.ApiCategoryRepo.Current.SearchAllStoreAPI("", provinceId, districtId, -1, ref total, IsRemoveCache).ToList();
            var res = new List<thegioididong.business.ApiCategory.StoreBO>();
            var storeIdsShow = arrStoreId[type].Split(',').ToList();

            objStore = new Models.Store()
            {
                Provinces = provinces,
                Districts = districts,
                Stores = stores.Where(p => storeIdsShow.Any(x => x == p.storeIDField.ToString())).ToList(),
                ProvinceId = provinceId,
                DistrictId = districtId,
                ProvinceName = provinceId == -1 ? "" : provinces.FirstOrDefault(p => p.provinceIDField == provinceId).provinceNameField,
                DistrictName = districtId == -1 ? "" : districts.FirstOrDefault(p => p.districtIDField == districtId).districtNameField,
                Products = BindingProduct(IsRemoveCache),
                Type = type
            };
            #endregion
            System.Web.HttpContext.Current.Cache.Add(cacheKey, objStore, null, DateTime.Now.AddSeconds(30), TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Normal, null);
            return PartialView(objStore);
        }
        #endregion

        #region Đăng ký nhận thông tin KHUYẾN MÃI
        public ActionResult SubmitEmailSubscribe(FormCollection fCol)
        {
            if (fCol == null || fCol.AllKeys.Length == 0) return null;

            var gender = fCol["hdGender"] == null ? -1 : int.Parse(fCol["hdGender"].ToString());
            var fullName = fCol["txtFullName"] == null ? "" : fCol["txtFullName"];
            var phoneNumber = fCol["txtPhoneNumber"] == null ? "" : fCol["txtPhoneNumber"];

            if (gender == -1) return ReturnJson("Vui lòng chọn giới tính.");
            if (string.IsNullOrEmpty(fullName.Trim())) return ReturnJson("Vui lòng nhập họ tên.");
            if (!thegioididong.business.helper.ValidateHelper.Current.IsValidVietNamPhoneNumber(phoneNumber))
                return ReturnJson("Số điện thoại trống/không đúng định dạng.");

            var crmGroupId = Program.CRMProgramIdTeaser;
            var objCustomer = new thegioididong.business.SvcCrm.CRMCUSTOMER { GENDER = gender, FULLNAME = fullName, MAINMOBILE = phoneNumber };
            int checkJoin = thegioididong.business.svc.SvcCrmRepo.Current.CheckCRMCustomerJoinGame(objCustomer, crmGroupId);
            if (checkJoin == -1) return ReturnJson("Chức năng đang được bảo trì, vui lòng thử lại sau");
            if (checkJoin >= 1) return ReturnJson("Bạn đã đăng ký nhận thông tin rồi! Cảm ơn bạn.");

            bool checkInsertCustomer = thegioididong.business.svc.SvcCrmRepo.Current.InsertCRMCustomerGammingInfo(objCustomer, 31, crmGroupId);
            if (!checkInsertCustomer)
                return ReturnJson("Chức năng đang được bảo trì, vui lòng thử lại sau");

            return ReturnJson("Xin cảm ơn, chúng tôi sẽ gửi thông tin cho bạn ngay khi chương trình bắt đầu.", 1);
        }
        #endregion
    }
}