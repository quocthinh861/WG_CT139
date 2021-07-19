using template.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using TGDD.Library.Caching;
using vng.pay123;
using thegioididong.business.helper;
using System.Configuration;
using System.Collections.Specialized;

namespace template.Controllers
{
    public class OrderController : BaseController
    {
        #region Đặt hàng PreOrder
        public ActionResult BoxUserOrder()
        {
            var products = BindingProduct();
            var dtmFrom = Program.FromDate;
            var dtmTo = Program.ToDate;
            var crmProgramIds = products.Any(p => p.IsLimitColor)
                ? string.Join(",", products.Select(p => p.CRMProgramIds))
                : string.Join(",", products.Select(p => p.CRMProgramId));

            var plus = InitProduct().Sum(p => p.QuantityPlus);
            int totalCount = 0;
            int totalPaid = 0;
            var customers = thegioididong.business.svc.SvcCrmCustomerRepo.Current.GetAllSaleOrderByProgramCRM(dtmFrom, dtmTo, "", crmProgramIds, PageSize, 1, -1, ref totalCount, ref totalPaid, IsRemoveCache);

            var objCustomer = new Customer()
            {
                Total = (Program.TotalOrder > 0 ? Program.TotalOrder : totalCount) + plus,
                TotalPaid = (Program.TotalPaid > 0 ? Program.TotalPaid : totalPaid) + plus,
                Customers = customers
            };

            return PartialView(objCustomer);
        }
        public ActionResult PopupUserOrder(FormCollection fCol)
        {
            var keyword = fCol["keyword"] == null ? string.Empty : fCol["keyword"].Trim();
            var page = fCol["page"] == null ? 1 : Convert.ToInt32(fCol["page"]);
            var status = fCol["cbPaid"] == null ? -1 : 1;
            var model = fCol["hdModel"] == null ? -1 : Convert.ToInt32(fCol["hdModel"]);

            var products = BindingProduct(IsRemoveCache);
            var plus = InitProduct().Sum(p => p.QuantityPlus);
            var dtmFrom = Program.FromDate;
            var dtmTo = Program.ToDate;
            var crmProgramIds = model == -1
                ? string.Join(",", products.Select(p => p.CRMProgramId))
                : products.FirstOrDefault(p => p.ProductId == model).CRMProgramId.ToString();

            if (products.Any(p => p.IsLimitColor)) crmProgramIds = string.Join(",", products.Select(p => p.CRMProgramIds));

            int totalCount = 0;
            int totalPaid = 0;
            var customers = thegioididong.business.svc.SvcCrmCustomerRepo.Current.GetAllSaleOrderByProgramCRM(dtmFrom, dtmTo, keyword, crmProgramIds, PageSize, page, status, ref totalCount, ref totalPaid);

            var objCustomer = new Customer()
            {
                Keyword = keyword,
                Page = page,
                PageSize = PageSize,
                IsPaid = status == 1,
                Total = totalCount + plus,
                TotalPaid = totalPaid + plus,
                Model = model,
                Customers = customers,
                Products = products
            };

            var objProduct = products.FirstOrDefault();
            if (string.IsNullOrEmpty(keyword) && Program.TotalOrder > 0 && Program.TotalPaid > 0)
            {
                objCustomer.Total = objCustomer.IsPaid ? Program.TotalPaid : Program.TotalOrder;
                objCustomer.TotalPaid = Program.TotalPaid;
            }
            else if (objProduct.IsSoldOut)
            {
                objCustomer.Total = objCustomer.IsPaid ? objProduct.TotalPaid : objProduct.TotalOrder;
                objCustomer.TotalPaid = objProduct.TotalPaid;
            }

            var total = objCustomer.IsPaid ? objCustomer.TotalPaid : objCustomer.Total;
            var totalRemain = total - (page * PageSize);
            objCustomer.IsShowViewMore = totalRemain > 0;
            objCustomer.TotalRemain = totalRemain;
            objCustomer.Index = total - ((page - 1) * PageSize);

            return PartialView(objCustomer);
        }
        #endregion

        #region Dùng chung   
        private int _promotionIdInstallment = 99999;
        private int _crmProgramInstallment = 11111;
        public ActionResult BoxOrder(int productId = -1, int method = -1)
        {
            var products = BindingProduct();
            var objProduct = productId == -1
                ? products.FirstOrDefault()
                : products.FirstOrDefault(p => p.ProductId == productId);
            if (objProduct == null) return ReturnJson("Sản phẩm không hợp lệ.");
            var isloginCTV = thegioididong.business.helper.Cookie.ManagerCookies.IsPartnerLogin();

            var objOrder = new Models.Order()
            {
                ProductId = productId,
                Product = objProduct,
                Products = products,
                Method = method,
                IsShowCaptcha = Session[SS_ORDER_CAMPAIGN] != null || CountOrder() > 0,
                IsMobile = IsMobileMode,
                IsSMS = IsSMS,
                UrlPath = UrlPath,
                IsOff = IsRunLive ? (IsSMS ? IsOffLaunching : IsOffPreOrder) : false,
                Text = IsRunPreOrder ? "Đặt cọc" : "Đặt hàng",
                IsRunPreOrder = IsRunPreOrder,
                PreOrderToDate = Program.ToDate,
                TotalOrder = productId == -1 ? products.Sum(p => p.TotalOrder) : objProduct.TotalOrder,
                TotalPaid = productId == -1 ? products.Sum(p => p.TotalPaid) : objProduct.TotalPaid,
                Cookie = thegioididong.business.helper.Cookie.ManagerCookies.GetCookieOrder(),
                Url = UrlCampaign,
                ProgramName = Program.ProgramName,
                UserNameCTV = isloginCTV != null ? (isloginCTV.UserName + " - " + isloginCTV.FullName) : string.Empty
            };

            return PartialView(objOrder);
        }
        public ActionResult QuickOrder(FormCollection fCol)
        {
            #region Kiểm tra thông tin nhập vào
            if (IsOffPreOrder) return ReturnJson("Chương trình đã kết thúc. Cảm ơn quý khách đã quan tâm.");
            if (fCol == null || fCol.AllKeys.Length == 0) return ReturnJson("Chức năng đang được bảo trì. Vui lòng thử lại.");

            int groupId = fCol["txtGroupId"] == null ? 0 : ConvertInt(fCol["txtGroupId"]);
            int productId = fCol["txtProductId"] == null ? 0 : ConvertInt(fCol["txtProductId"]);
            var productCode = fCol["txtProductCode"] == null ? "" : fCol["txtProductCode"];

            int gender = fCol["txtGender"] == null ? -1 : ConvertInt(fCol["txtGender"], false);
            var fullName = fCol["txtFullName"] == null ? string.Empty : fCol["txtFullName"];
            var phoneNumber = fCol["txtPhoneNumber"] == null ? string.Empty : fCol["txtPhoneNumber"];
            int method = fCol["txtMethod"] == null ? -1 : ConvertInt(fCol["txtMethod"]);
            int storeId = fCol["txtStoreId"] == null ? 0 : ConvertInt(fCol["txtStoreId"]);
            int promotionType = fCol["txtPromotion"] == null ? 0 : ConvertInt(fCol["txtPromotion"]);
            var type = fCol["hdType"] == null ? 0 : ConvertInt(fCol["hdType"]);
            var isReceivedHN = fCol["cbConfirmHN"] != null;
            var isReceivedHCM = fCol["cbConfirmHCM"] != null;

            int provinceId = fCol["hdProvinceId"] == null ? 0 : ConvertInt(fCol["hdProvinceId"]);
            var provinceName = fCol["hdProvinceName"] == null ? "" : fCol["hdProvinceName"];
            int districtId = fCol["hdDistrictId"] == null ? 0 : ConvertInt(fCol["hdDistrictId"]);
            var districtName = fCol["hdDistrictName"] == null ? "" : fCol["hdDistrictName"];
            var address = fCol["txtAddress"] == null ? string.Empty : fCol["txtAddress"];
            var otherRequest = fCol["txtOtherRequest"] == null ? string.Empty : fCol["txtOtherRequest"];

            var errors = new List<string>();
            if (InitProduct().FirstOrDefault().GroupId > 0)
            {
                if (groupId <= 0) errors.Add("o-group");
                if (productId <= 0) errors.Add("o-model");
            }
            else if (InitProduct().Count > 1 && productId <= 0)
            {
                errors.Add("o-model");
            }
            else if (productId <= 0) return ReturnJson("Sản phẩm không hợp lệ.");

            if (string.IsNullOrEmpty(productCode)) errors.Add("o-color");
            if (gender != 0 && gender != 1) errors.Add("o-gender");
            if (string.IsNullOrEmpty(fullName.Trim())) errors.Add("o-fullname");
            if (fullName.Trim().Length > 32) return ReturnJson("Họ tên tối đa 32 kí tự.");
            if (!thegioididong.business.helper.ValidateHelper.Current.IsValidVietNamPhoneNumber(phoneNumber))
                errors.Add("o-phonenumber");
            if (method != (int)MethodType.AtStore &&
                //method != (int)MethodType.AtHome &&
                method != (int)MethodType.VISA123PAY &&
                method != (int)MethodType.ATM &&
                method != (int)MethodType.Installment &&
                method != (int)MethodType.InstallmentCredit)
                errors.Add("o-method");

            if (method == (int)MethodType.AtHome)
            {
                if (provinceId <= 0) errors.Add("province");
                if (districtId <= 0) errors.Add("district");
                if (string.IsNullOrEmpty(address.Trim())) errors.Add("address");
            }
            else
            {
                if (storeId <= 0) errors.Add("o-store");
            }

            if (errors.Count > 0) return Json(new
            {
                status = -99,
                errors = errors
            }, JsonRequestBehavior.AllowGet);

            var product = BindingProduct().FirstOrDefault(p => p.ProductId == productId);
            if (product == null) return ReturnJson("Sản phẩm không hợp lệ.");

            if (product.IsSoldOut) return ReturnJson("Sản phẩm đã cháy hàng. Cảm ơn quý khách đã quan tâm");

            var objProduct = thegioididong.business.api.ApiProductRepo.Current.GetProductDetailById(productId, 3);
            if (objProduct == null) return ReturnJson("Sản phẩm không hợp lệ.");

            var productColors = product.Colors;
            if (productColors == null || productColors.Count == 0) return ReturnJson("Vui lòng chọn màu sản phẩm.");

            var objProductColor = productColors.FirstOrDefault(p => p.productCodeField == productCode);
            if (objProductColor == null) return ReturnJson("Màu sản phẩm không hợp lệ.");
            if (objProductColor.isExistField) return ReturnJson("Sản phẩm đã cháy hàng. Cảm ơn quý khách đã quan tâm");

            if (product.Price <= 0) return ReturnJson("#99 - Chức năng đang bảo trì. Quý khách vui lòng thử lại.");

            var objStore = thegioididong.business.api.ApiCategoryRepo.Current.GetStoreByID(storeId);
            if (objStore == null) return ReturnJson("Siêu thị không hợp lệ.");

            //var objProvince = thegioididong.business.api.ApiCategoryRepo.Current.GetProvinceByID(provinceId, IsRemoveCache);
            //if (objProvince == null) return ReturnJson("Tỉnh/thành phố không không hợp lệ.");

            //if (districtId > 0)
            //{
            //    var objDistrict = thegioididong.business.api.ApiCategoryRepo.Current.GetDistrictBOById(provinceId, districtId, IsRemoveCache);
            //    if (objDistrict == null) return ReturnJson("Quận/huyện không không hợp lệ.");
            //}

            var crmProgramId = product.IsLimitColor ? objProductColor.categoryIDField : product.CRMProgramId;

            #region Lưu thông tin COOKIE
            if (MvcApplication.IsTGDD())
            {
                var objCookie = thegioididong.business.helper.Cookie.ManagerCookies.GetCookieOrder();
                objCookie.Gender = gender;
                objCookie.FullName = fullName;
                objCookie.PhoneNumber = phoneNumber;
                objCookie.StoreId = storeId;
                objCookie.DistrictId = districtId;
                objCookie.ProvinceId = provinceId;
                thegioididong.business.helper.Cookie.ManagerCookies.SetCookieOrder(objCookie);
            }
            #endregion

            //Trả về link trả góp thẻ tín dụng
            if (method == (int)MethodType.InstallmentCredit)
                return ReturnJson(objProductColor.iconField, -88);

            #region Kiểm tra captcha
            if (Session[SS_ORDER_CAMPAIGN] != null || CountOrder() > 0)
            {
                var secretKey = "6LdjGgcaAAAAAHOVd3HSyxhivsneUZrs6Vkca2W0";
                var url = string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}", secretKey, fCol["g-recaptcha-response"], GetUserIP());
                dynamic result = thegioididong.business.helper.NetHelper.GetJSON(url);
                if (result == null || !result["success"]) return ReturnJson("Mã xác nhận không đúng.");
            }
            #endregion
            #endregion

            #region Tạo đơn hàng trên CRM
            thegioididong.business.ApiProduct.ProductBO productBO = objProduct;
            productBO.productCodeField = productCode;
            productBO.productErpPriceBOField = new thegioididong.business.ApiProduct.ProductErpPriceBO()
            {
                productCodeField = productCode,
                priceField = product.Price
            };

            //Sản phẩm
            List<thegioididong.business.ApiProduct.ProductBO> lProductToOrder = new List<thegioididong.business.ApiProduct.ProductBO>();
            lProductToOrder.Add(productBO);

            var note = "";
            if (IsRunPreOrder)
            {
                note = GetNotePreOrder(method, promotionType, product.ProductId, product.SalePrice);
            }
            else
            {
                note = string.Format("Đơn hàng chương trình {0} - {1}", CampaignName, GetMethodNameByType(method));
                if (!IsRunLive && (method == (int)MethodType.VISA123PAY || method == (int)MethodType.ATM))
                {
                    note = "IT TEST THANH TOÁN TRỰC TUYẾN - CC HỦY GIÚP NHÉ.";
                }
            }

            //Thông tin người dùng đặt mua
            int totalMoney = 0;
            var oCRMSaleOrder = new thegioididong.business.SvcCrm.Saleorderonline()
            {
                Gender = gender,
                Fullname = fullName,
                Phone = phoneNumber,
                Note = otherRequest,
                SaleOrderNote = note,
                Email = "",
                //SHIPCITY = provinceId,
                //SHIPSTATE = districtId,
                //ShippAddress = address,
                OUTPUTSTOREID = storeId,
                COUNTMINUTES = 0,
                ISAUTOTRANFERERP = 0,
                ProgramID = crmProgramId,
                SourceOrder = GetCookieOffer(),
                TIMEEXPECTDELIVERY = product.ToDateReceived,
                Companyid = SiteId,
                GENCOMPANYID = SiteId,
                CLIENTIP = GetUserIP()
            };
            var isloginCTV = thegioididong.business.helper.Cookie.ManagerCookies.IsPartnerLogin();
            if (isloginCTV != null)
            {
                oCRMSaleOrder.CreateUser = isloginCTV.UserName + " - " + isloginCTV.FullName;
                oCRMSaleOrder.DeviceID = 6;
            }
            //Thanh toán trực tuyến
            if (method == (int)MethodType.ATM || method == (int)MethodType.VISA123PAY)
            {
                totalMoney = IsRunPreOrder ? (int)product.SalePrice : (int)product.Price;
                oCRMSaleOrder.OrderWebTypeID = 1;
                oCRMSaleOrder.TOTALPAID = totalMoney;
                oCRMSaleOrder.ISAUTOTRANFERERP = 0;
                oCRMSaleOrder.TRANSACTIONTYPE = method == (int)MethodType.ATM
                    ? ConstantsHelper.PaymentType.OnePay_Atm
                    : ConstantsHelper.PaymentType.OnePay_Visa;
            }
            else if (method == (int)MethodType.AtStore)
            {
                oCRMSaleOrder.OrderWebTypeID = 1; //Nhận tại siêu thị
            }
            else if (method == (int)MethodType.AtHome)
            {
                oCRMSaleOrder.OrderWebTypeID = 2; //Nhận hàng tại nhà
            }
            else
            {
                oCRMSaleOrder.OrderWebTypeID = 3; //Trả góp
            }

            int iCustomer = -1;
            int iStatusLockPhone = -1;
            string crmErrorMessage = string.Empty;
            var buyWeb = !MvcApplication.IsMobileMode();
            long saleOrderId = thegioididong.business.svc.SvcCrmRepo.Current.InsertNewShoppingOrder(lProductToOrder, oCRMSaleOrder, ref iCustomer, ref iStatusLockPhone, ref crmErrorMessage, buyweb: buyWeb, isSendSMS: false, StatusID: StatusId, nDeposit: totalMoney);

            if (saleOrderId < 0) return ReturnJson(!string.IsNullOrWhiteSpace(crmErrorMessage) ? crmErrorMessage : "Tạo mới đơn hàng không thành công, vui lòng thử lại sau");
            #endregion

            #region Thông báo thành công
            var dateOffPaid = product.ToDate.ToString("dd/MM/yyyy");
            var dateReceived = string.Format("{0} - {1}.", product.FromDateReceived.ToString("dd/MM/yyyy"), product.ToDateReceived.ToString("dd/MM/yyyy"));
            if (product.IsNormalSale)
            {
                dateOffPaid = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy");
                dateReceived = string.Format("{0} ngày tính từ thời gian cọc", product.IsFrom2To7 ? "2-7" : "5-7");
            }
            var objOrderSuccess = new OrderSuccess()
            {
                ProductId = productId,
                ProductName = objProduct.productNameField,
                ProductCode = productCode,
                Price = product.Price,
                SalePrice = product.SalePrice,
                FullName = fullName,
                Gender = gender == 1 ? "anh" : "chị",
                Method = method,
                MethodName = GetMethodNameByType(method),
                ColorName = product.Colors.FirstOrDefault(p => p.productCodeField == productCode).colorNameField,
                CampaignName = CampaignName,
                DateOffPaid = dateOffPaid,
                DateReceived = dateReceived,
                Url = UrlCampaign,
                StoreAddress = method == 3
                    ? string.Format("{0}, {1}, {2}", address, districtName, provinceName)
                    : string.IsNullOrEmpty(objStore.webAddressField) ? objStore.storeAddressField : objStore.webAddressField
            };
            #endregion

            #region Thanh toán VISA, MASTER, ATM
            if (method == (int)MethodType.ATM || method == (int)MethodType.VISA123PAY)
            {
                method = method == (int)MethodType.ATM
                    ? ConstantsHelper.PaymentType.OnePay_Atm
                    : ConstantsHelper.PaymentType.OnePay_Visa;
                var linkRedirect = CheckPaymentOrder(saleOrderId.ToString(), totalMoney.ToString(), fullName, phoneNumber, method);
                objOrderSuccess.IsPayment = true;
                objOrderSuccess.LinkRedirect = linkRedirect;
                Session[SS_PAYMENT_CAMPAIGN] = objOrderSuccess;
                return PartialView("OrderSuccess", objOrderSuccess);
            }
            #endregion

            #region Send SMS
            //Gửi SMS cho người dùng           
            var genderName = gender == 1 ? "Anh" : "Chi";
            var lastName = fullName.Contains(" ")
                ? fullName.Substring(fullName.LastIndexOf(" ")).Trim()
                : fullName.Trim();
            if (lastName.Length > 9) lastName = lastName.Substring(0, 9);
            var hotline = Hotline().Replace(".", "");
            var sms = IsTGDD ? "TGDD" : "Dienmayxanh";

            if (IsSMS) //Đặt hàng SMS
            {
                var code = saleOrderId.ToString().Substring(saleOrderId.ToString().Length - 4);
                var cuphap = IsTGDD ? "TGDD" : "DMX";
                var templateId = 362;
                var pointId = 38;
                var noteSMS = "";
                var keyAuthen = "121f44e9-9b9a-43e1-b91d-99a7023d162b";
                var error = "";
                Dictionary<string, string> smsText = new Dictionary<string, string>
                {
                    {"{Giới tính}", genderName},
                    {"{Ten KH}", lastName},
                    {"{tên sản phẩm}", product.ProductName},
                    {"{giá sản phẩm}", thegioididong.business.helper.ViewHelper.ShowNumber((int)product.SalePrice)},
                    {"{cú pháp}", cuphap},
                    {"{mã code}", code},
                    {"{số điện thoại tổng đài}", hotline},
                };
                var isSMSSuccess = thegioididong.business.svc.SvcCrmSmsRepo.Current.SendSMSByTemplate(phoneNumber, JsonConvert.SerializeObject(smsText), templateId, pointId, noteSMS, keyAuthen, ref error);

                #region Thông báo thành công
                objOrderSuccess.Code = code;
                objOrderSuccess.Syntax = cuphap;
                objOrderSuccess.DeliveryDate = DateTime.Now.AddDays(2);
                //objOrderSuccess.AvatarUrl = product.AvatarUrl;
                //objOrderSuccess.IsKey = product.IsKey;
                objOrderSuccess.PhoneNumber = phoneNumber;
                //objOrderSuccess.DiscountPrice = product.DiscountPrice;
                objOrderSuccess.CategoryName = objProduct.productCategoryBOField.categoryNameField;
                #endregion
            }
            //else //Dành cho preorder
            //{
            //    if (!IsOnLaunching)
            //    {
            //        if (Is9H30To23H59)
            //        {
            //            //var SMSContent = string.Format("Cam on {0} {1} da dat mua {3}.Nhan vien se goi lai truoc 10:00 sang mai de xac nhan don hang. CT ket thuc som neu du so luong.TD{2}", genderName, lastName, hotline, CampaignName);
            //            //string strSendSMS = thegioididong.business.svc.SvcCrmSmsRepo.Current.SendSMS(phoneNumber, SMSContent, sms, "WEB", "SMS don hang chuong trinh PREORDER " + CampaignName, 1);

            //            var templateId = 363;
            //            var pointId = 38;
            //            var noteSMS = "";
            //            var keyAuthen = "02169033-5b5c-4b9f-8299-7ef7c74d11bb";
            //            var error = "";
            //            Dictionary<string, string> smsText = new Dictionary<string, string>
            //            {
            //                {"{Giới tính}", genderName},
            //                {"{Ten KH}", lastName},
            //                {"{tên sản phẩm}", product.ProductName},
            //                {"{số điện thoại tổng đài}", hotline},
            //            };
            //            var isSMSSuccess = thegioididong.business.svc.SvcCrmSmsRepo.Current.SendSMSByTemplate(phoneNumber, JsonConvert.SerializeObject(smsText), templateId, pointId, noteSMS, keyAuthen, ref error);
            //        }
            //        else if (Is0H00To7H30)
            //        {
            //            //var SMSContent = string.Format("Cam on {0} {1} da dat mua {3}.Nhan vien se goi lai truoc 10:00 de xac nhan don hang. CT ket thuc som neu du so luong.TD{2}", genderName, lastName, hotline, CampaignName);
            //            //string strSendSMS = thegioididong.business.svc.SvcCrmSmsRepo.Current.SendSMS(phoneNumber, SMSContent, sms, "WEB", "SMS don hang chuong trinh PREORDER " + CampaignName, 1);
            //            var templateId = 364;
            //            var pointId = 38;
            //            var noteSMS = "";
            //            var keyAuthen = "8f5a741f-a113-49ed-86bc-1aad4f7f07a4";
            //            var error = "";
            //            Dictionary<string, string> smsText = new Dictionary<string, string>
            //            {
            //                {"{Giới tính}", genderName},
            //                {"{Ten KH}", lastName},
            //                {"{tên sản phẩm}", product.ProductName},
            //                {"{số điện thoại tổng đài}", hotline},
            //            };
            //            var isSMSSuccess = thegioididong.business.svc.SvcCrmSmsRepo.Current.SendSMSByTemplate(phoneNumber, JsonConvert.SerializeObject(smsText), templateId, pointId, noteSMS, keyAuthen, ref error);
            //        }
            //    }
            //}
            #endregion

            #region Hiển thị captcha khi đặt lần 2
            var val = 1;
            Session[SS_ORDER_CAMPAIGN] = val;
            var cacheKeyOrder = SS_ORDER_CAMPAIGN + GetUserIP();
            CacheHelper.Add(cacheKeyOrder, val.ToString(), DateTime.Now.AddMinutes(30));
            #endregion

            return PartialView(IsSMS ? "OrderSuccessSMS" : "OrderSuccess", objOrderSuccess);
        }
        public ActionResult GetCaptchaImage(string prefix)
        {
            Session[SS_CAPTCHA_CAMPAIGN] = thegioididong.business.helper.CaptchaImage.GenerateRandomCode();
            FileContentResult img = null;
            var ci = new thegioididong.business.helper.CaptchaImage(Session[SS_CAPTCHA_CAMPAIGN].ToString(), 90, 32, "Times New Roman");
            MemoryStream mem = new MemoryStream();
            ci.Image.Save(mem, ImageFormat.Jpeg);
            img = this.File(mem.GetBuffer(), "image/Jpeg");
            return img;
        }
        public ActionResult BoxStore(int provinceId = 3, int districtId = -1, int method = -1)
        {
            var provinces = thegioididong.business.api.ApiCategoryRepo.Current.GetAllProvince();
            if (provinces != null) provinces = provinces.Where(p => p.provinceIDField > 0).OrderBy(p => p.orderIndexField).ToList();
            var objProvince = provinces.FirstOrDefault(p => p.provinceIDField == provinceId);

            var districts = thegioididong.business.api.ApiCategoryRepo.Current.GetListDistrictByProvince(provinceId).ToList();
            var objDistrict = districts.FirstOrDefault(p => p.districtIDField == districtId);
            if (objDistrict == null) districtId = -1;

            var total = 0;
            var listStore = thegioididong.business.api.ApiCategoryRepo.Current.SearchStoreAPI("", provinceId, districtId, -1, ref total, IsRemoveCache);

            var stores = listStore == null
                ? null
                : listStore.Where(p => p.storeTypeIDField != 2 && p.storeIDField != 1151 &&
                ((!string.IsNullOrEmpty(p.webAddressField) && !p.webAddressField.Contains("Aeon Mall Bình Tân")) ||
                (!string.IsNullOrEmpty(p.storeAddressField) && !p.storeAddressField.Contains("Aeon Mall Bình Tân")))).ToList();

            var objStore = new Models.Store()
            {
                Provinces = provinces,
                Districts = districts,
                Stores = stores.OrderBy(o => o.siteIDField).ThenBy(o => o.webAddressField).ThenBy(t => t.storeAddressField).ToList(),
                ProvinceId = provinceId,
                ProvinceName = objProvince == null ? "" : objProvince.provinceNameField,
                DistrictId = districtId,
                DistrictName = objDistrict == null ? "" : objDistrict.districtNameField,
                Cookie = thegioididong.business.helper.Cookie.ManagerCookies.GetCookieOrder(),
                Method = method
            };

            return PartialView(objStore);
        }

        public string ListProvince()
        {
            List<thegioididong.business.ApiCategory.ProvinceBO> listProvince = thegioididong.business.api.ApiCategoryRepo.Current.GetAllProvince();
            string htmlOption = "<option value='{0}'>{1}</option>";
            string htmlOptionSelected = "<option value='{0}' selected='selected'>{1}</option>";
            var htmlProvince = string.Format(htmlOption, -1, "Chọn tỉnh/thành phố");
            var objCookie = thegioididong.business.helper.Cookie.ManagerCookies.GetCookieOrder();
            foreach (var item in listProvince.Where(p => p.provinceIDField > 0).OrderBy(p => p.orderIndexField))
            {
                htmlProvince += string.Format(
                    (objCookie.ProvinceId == -1 && item.provinceIDField == 3) ||
                    (objCookie.ProvinceId == item.provinceIDField)
                        ? htmlOptionSelected : htmlOption,
                    item.provinceIDField,
                    item.provinceNameField);
            }
            return htmlProvince;
        }
        public string ListDistrictByProvince(int provinceId)
        {
            thegioididong.business.ApiCategory.DistrictBO[] lstAllDistrict = thegioididong.business.api.ApiCategoryRepo.Current.GetListDistrictByProvince(provinceId);
            //lstAllDistrict = lstAllDistrict.Where(x => x.storeCountField > 0).ToArray();
            string htmlOption = "<option value='{0}'>{1}</option>";
            string htmlOptionSelected = "<option value='{0}' selected='selected'>{1}</option>";
            var htmlDistrict = string.Format(htmlOption, -1, "Chọn quận huyện");
            var objCookie = thegioididong.business.helper.Cookie.ManagerCookies.GetCookieOrder();
            foreach (var item in lstAllDistrict)
            {
                if (item.districtIDField == 50) continue;
                htmlDistrict += string.Format(
                    objCookie.DistrictId > 0 && objCookie.DistrictId == item.districtIDField
                        ? htmlOptionSelected : htmlOption,
                    item.districtIDField,
                    item.districtNameField);
            }
            return htmlDistrict;
        }
        public string ListStoreByDistrict(int provinceId, int districtId)
        {
            int total = 0;
            var listStore = thegioididong.business.api.ApiCategoryRepo.Current.SearchStoreAPI("", provinceId, districtId, -1, ref total, IsRemoveCache).ToList();
            string htmlOption = "<span data-value='{0}'>{1}<b>{2}{3}</b></span>";
            if (listStore == null || listStore.Count == 0) return string.Format(htmlOption, -1, "", "Không tìm thấy siêu thị");

            listStore = listStore.Where(p => p.storeTypeIDField != 2
                && p.storeIDField != 1151 &&
                ((!string.IsNullOrEmpty(p.webAddressField) && !p.webAddressField.Contains("Aeon Mall Bình Tân")) ||
                (!string.IsNullOrEmpty(p.storeAddressField) && !p.storeAddressField.Contains("Aeon Mall Bình Tân")))).ToList();

            var htmlStore = "";
            foreach (var item in listStore)
            {
                htmlStore += string.Format(htmlOption, item.storeIDField, "<i></i>", item.siteIDField == 2 ? "ĐMX - " : "", string.IsNullOrEmpty(item.webAddressField) ? item.storeAddressField : item.webAddressField);
            }
            return htmlStore;
        }
        public string ListStoreByProduct(int productId, string productCode, int provinceId, int districtId)
        {
            var stores = CheckStoreByFilter(provinceId, productId, districtId, productCode);
            string htmlOption = "<span data-value='{0}'><i></i><b>{1} {2}</b></span>";
            if (stores == null || stores.Count == 0)
                return "<span data-value=\"-1\"><b>Không tìm thấy siêu thị còn hàng</b></span>";

            var htmlStore = "";
            foreach (var item in stores)
            {
                htmlStore += string.Format(htmlOption, item.StoreID, item.StoreAddress, "(" + item.Quantity + " sản phẩm)");
            }
            return htmlStore;
        }
        private List<thegioididong.business.SvcCategory.StoreBO> CheckStoreByFilter(int iProvince, int iProductID, int iDistrict, string sProCode)
        {
            string sCacheKey = string.Format("CK_CheckStoreByFilter_BY_FILTER_BY_PRODUCT_V3_{0}_PROVINCE_{1}_DIS_{2}_CODE_{3}", iProductID, iProvince, iDistrict, sProCode);
            var sReturn = System.Web.HttpContext.Current.Cache.Get(sCacheKey) as List<thegioididong.business.SvcCategory.StoreBO>;

            if (sReturn != null && sReturn.Count > 0) return sReturn;

            thegioididong.business.SvcCategory.ProvinceBO[] lstProvinceBO = thegioididong.business.svc.SvcCategoryRepo.Current.GetStoreInProvinceByProductID(iProductID, sProCode, 1);

            if (lstProvinceBO == null || lstProvinceBO.Length == 0) return null;

            var lproInStock = lstProvinceBO.ToList().Find(x => x.ProvinceID == iProvince);
            if (lproInStock == null) return null;

            var lDistrictInStock = lproInStock.DistrictBOList;
            if (lDistrictInStock == null) return null;

            var dis = iDistrict == -1
                ? lDistrictInStock.Where(d => d.StoreBOList.Where(l => l.Quantity > 0).ToList().Count > 0).ToList()
                : lDistrictInStock.Where(d => d.DistrictID == iDistrict).ToList();

            if (dis == null || dis.Count == 0) return null;

            var stores = new List<thegioididong.business.SvcCategory.StoreBO>();
            foreach (var districtBO in dis)
            {
                var storeList = districtBO.StoreBOList.Where(l => l.Quantity > 0).ToList();
                foreach (var storeBO in storeList)
                {
                    stores.Add(storeBO);
                }
            }

            if (stores != null && stores.Count > 0)
            {
                System.Web.HttpContext.Current.Cache.Add(sCacheKey, stores, null, DateTime.Now.AddMinutes(1), TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Normal, null);
            }

            return stores;
        }
        private int ConvertInt(string s, bool isDefaulZero = true)
        {
            var res = -1;
            var flag = Int32.TryParse(s, out res);
            return flag ? res : (isDefaulZero ? 0 : -1);
        }

        #region Thanh toán trực tuyến
        //private List<string> _phoneTest = new List<string>() { "0935990179", "0933413050", "0938727300", "0938727600" };
        public ActionResult Pay123()
        {
            if (Session[SS_PAYMENT_CAMPAIGN] == null) Response.Redirect(UrlCampaign, true);
            var objOS = Session[SS_PAYMENT_CAMPAIGN] as OrderSuccess;

            if (!Request["status"].Equals("1"))
            {
                if (Request["transactionID"] != null)
                {
                    int lngOrderID = 0;
                    int.TryParse(Request["transactionID"], out lngOrderID);
                    SendSMSErrorPay(lngOrderID);
                }
                objOS.IsPaymentSuccess = false;
                Session[SS_PAYMENT_CAMPAIGN] = objOS;
                Response.Redirect(UrlCampaign);
                return null;
            }

            #region Lấy thông tin thanh toán và trạng thái
            var pg = new Pay123();
            var result = pg.QueryOrderAndVerify(Request["transactionID"], GetUserIP(), Request["time"], Request["status"], Request["ticket"]);
            var json_serializer = new JavaScriptSerializer();
            object[] o = (object[])json_serializer.DeserializeObject(result);
            var paymentResult = o[0].Equals("1") && o[2].Equals("1");
            #endregion
            #region Thanh toán thành công
            if (paymentResult)
            {
                var pay123TransId = o[1].ToString();
                var orderId = Convert.ToInt64(Request["transactionID"].ToString());
                var orderInfo = string.Format("Thanh toan don hang {0} so tien {1}", orderId, o[3].ToString());
                var flagUpdate = thegioididong.business.svc.SvcCrmRepo.Current.CrmSaleOnlineUpdatePaymentStatus(orderId, 1, pay123TransId, string.Empty, orderInfo);
                if (flagUpdate > 0)
                {
                    var objOrder = thegioididong.business.svc.SvcCrmRepo.Current.GetDetailOrderById(orderId);
                    var emailContent = string.Format("Pay123 thanh cong: {0}, Thời gian {1}, {2}, {3}, {4}, {5}",
                        orderId,
                        DateTime.Now.ToString(),
                        orderInfo,
                        pay123TransId,
                        Request.Url.ToString(),
                        objOrder == null ? "null" : Newtonsoft.Json.JsonConvert.SerializeObject(objOrder)
                    );
                    thegioididong.business.helper.EmailHelper.SendMailError(emailContent);
                }
                else
                {
                    new WebLibs.SystemMessage(CampaignName + " > TGDD > ORDERCONTROLLER.Pay123 > CrmSaleOnlineUpdatePaymentStatus", Request.Url.OriginalString, "Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + orderId);
                    thegioididong.business.helper.EmailHelper.SendMailError("Pay123 - Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + orderId + " Thời gian " + DateTime.Now.ToString() + orderInfo + " " + pay123TransId + " " + Request.Url.ToString());
                }

                objOS.IsPaymentSuccess = true;
                Session[SS_PAYMENT_CAMPAIGN] = objOS;
                Response.Redirect(UrlCampaign);
                return null;
            }
            #endregion
            #region Thanh toán trực tuyến thất bại
            if (Request["transactionID"] != null)
            {
                int lngOrderID = 0;
                int.TryParse(Request["transactionID"], out lngOrderID);
                SendSMSErrorPay(lngOrderID);
            }
            objOS.IsPaymentSuccess = false;
            Session[SS_PAYMENT_CAMPAIGN] = objOS;
            Response.Redirect(UrlCampaign);
            return null;
            #endregion
        }
        public ActionResult VNPay()
        {
            if (Session[SS_PAYMENT_CAMPAIGN] == null) Response.Redirect(UrlCampaign, true);
            var objOS = Session[SS_PAYMENT_CAMPAIGN] as OrderSuccess;

            #region Lấy thông tin thanh toán và trạng thái
            var UrlCallBack = "/";
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_Api_Url = ConfigurationManager.AppSettings["vnpay_api_url"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
            mwg.payment.service.IPayMent paymwg = new mwg.payment.service.VNPay(UrlCallBack, vnp_Url, vnp_Api_Url, vnp_TmnCode, vnp_HashSecret);
            NameValueCollection qscoll = HttpUtility.ParseQueryString(Request.Url.Query);
            var result = paymwg.GetPaymentResult(qscoll);
            bool paymentSuccess = false;
            if (result.Status == mwg.payment.model.PayStatus.Success)
                paymentSuccess = true;
            #endregion

            #region Thanh toán thành công
            if (paymentSuccess)
            {
                var orderInfo = string.Format("Thanh toan don hang {0}", result.OrderId);
                var flagUpdate = thegioididong.business.svc.SvcCrmRepo.Current.CrmSaleOnlineUpdatePaymentStatus(result.OrderId, 1, result.TransactionNoString, string.Empty, orderInfo);
                if (flagUpdate > 0)
                {
                    var objOrder = thegioididong.business.svc.SvcCrmRepo.Current.GetDetailOrderById(result.OrderId);
                    var emailContent = string.Format("VNPay thanh cong: {0}, Thời gian {1}, {2}, {3}, {4}, {5}",
                        result.OrderId,
                        DateTime.Now.ToString(),
                        orderInfo,
                        result.TransactionNoString,
                        Request.Url.ToString(),
                        objOrder == null ? "null" : JsonConvert.SerializeObject(objOrder)
                    );
                    EmailHelper.SendMailError(emailContent);
                }
                else
                {
                    new WebLibs.SystemMessage(CampaignName + " > TGDD > ORDERCONTROLLER.VNPay > CrmSaleOnlineUpdatePaymentStatus", Request.Url.OriginalString, "Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId);
                    EmailHelper.SendMailError("VNPay - Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId + " Thời gian " + DateTime.Now.ToString() + orderInfo + " " + result.TransactionNoString + " " + Request.Url.ToString());
                }
                objOS.IsPaymentSuccess = true;
                Session[SS_PAYMENT_CAMPAIGN] = objOS;
                Response.Redirect(UrlCampaign);
                return null;
            }
            #endregion

            #region Thanh toán trực tuyến thất bại
            SendSMSErrorPay((int)result.OrderId);
            objOS.IsPaymentSuccess = false;
            Session[SS_PAYMENT_CAMPAIGN] = objOS;
            Response.Redirect(UrlCampaign);
            return null;
            #endregion
        }
        public ActionResult AlepayGateway()
        {
            if (Session[SS_PAYMENT_CAMPAIGN] == null) Response.Redirect(UrlCampaign, true);
            var objOS = Session[SS_PAYMENT_CAMPAIGN] as OrderSuccess;

            #region Lấy thông tin thanh toán và trạng thái
            var apiKeyNormal = ConfigurationManager.AppSettings["alepay_apiKeyNormal"] ?? "SfCKCzq9LOLUFopxZHsSLotu53BwOB";
            var publicKeyNormal = ConfigurationManager.AppSettings["alepay_publicKeyNormal"] ?? "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDA0VVu0so+wNSSOITvB4zfwIygOMYDvocvdgRIC/cwSvLUEhEkTdI/ZZYgL/isHYbcu8E20hhSUP8sv0XXEe17UrAv1UaFsz0Aa7ulKLlZvg+RYdalior7X5KN73+L/ANxJ4gWotjLjYn1qFKi+RsOevmhSpz7nkM3AsBozmgpFQIDAQAB";
            var checksumKeyNormal = ConfigurationManager.AppSettings["alepay_checksumKeyNormal"] ?? "sJFeha8i3OFVQIT504D0IBfx9ouZF4";
            var apiURLNormal = ConfigurationManager.AppSettings["alepay_apiURLNormal"] ?? "https://alepay.vn";
            var paymwg = new mwg.payment.service.AlepayGateway(apiKeyNormal, publicKeyNormal, checksumKeyNormal, apiURLNormal);
            NameValueCollection qscoll = HttpUtility.ParseQueryString(Request.Url.Query);
            var result = paymwg.GetPaymentResult(qscoll);
            bool paymentSuccess = false;
            if (result.Status == mwg.payment.model.PayStatus.Success)
                paymentSuccess = true;
            #endregion

            #region Thanh toán thành công
            if (paymentSuccess)
            {
                var orderInfo = string.Format("Thanh toan don hang {0}", result.OrderId);
                var flagUpdate = thegioididong.business.svc.SvcCrmRepo.Current.CrmSaleOnlineUpdatePaymentStatus(result.OrderId, 1, result.TransactionNoString, string.Empty, orderInfo);
                if (flagUpdate > 0)
                {
                    var objOrder = thegioididong.business.svc.SvcCrmRepo.Current.GetDetailOrderById(result.OrderId);
                    var emailContent = string.Format("AlepayGateway thanh cong: {0}, Thời gian {1}, {2}, {3}, {4}, {5}",
                        result.OrderId,
                        DateTime.Now.ToString(),
                        orderInfo,
                        result.TransactionNoString,
                        Request.Url.ToString(),
                        objOrder == null ? "null" : JsonConvert.SerializeObject(objOrder)
                    );
                    EmailHelper.SendMailError(emailContent);
                }
                else
                {
                    new WebLibs.SystemMessage(CampaignName + " > TGDD > ORDERCONTROLLER.AlepayGateway > CrmSaleOnlineUpdatePaymentStatus", Request.Url.OriginalString, "Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId);
                    EmailHelper.SendMailError("AlepayGateway - Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId + " Thời gian " + DateTime.Now.ToString() + orderInfo + " " + result.TransactionNoString + " " + Request.Url.ToString());
                }
                objOS.IsPaymentSuccess = true;
                Session[SS_PAYMENT_CAMPAIGN] = objOS;
                Response.Redirect(UrlCampaign);
                return null;
            }
            #endregion

            #region Thanh toán trực tuyến thất bại
            SendSMSErrorPay((int)result.OrderId);
            objOS.IsPaymentSuccess = false;
            Session[SS_PAYMENT_CAMPAIGN] = objOS;
            Response.Redirect(UrlCampaign);
            return null;
            #endregion
        }
        public ActionResult ZaloGateway()
        {
            if (Session[SS_PAYMENT_CAMPAIGN] == null) Response.Redirect(UrlCampaign, true);
            var objOS = Session[SS_PAYMENT_CAMPAIGN] as OrderSuccess;

            #region Lấy thông tin thanh toán và trạng thái
            var AppId = Convert.ToInt32(ConfigurationManager.AppSettings["payment.zalo.appid"]);
            var Key1 = ConfigurationManager.AppSettings["payment.zalo.key1"];
            var Key2 = ConfigurationManager.AppSettings["payment.zalo.key2"];
            var ApiBaseAddress = ConfigurationManager.AppSettings["payment.zalo.api_base_url"];
            var GatewayApiBaseAddress = ConfigurationManager.AppSettings["payment.zalo.gateway"];
            var paymwg = new mwg.payment.service.ZaloPay(AppId, Key1, Key2, ApiBaseAddress, GatewayApiBaseAddress);
            NameValueCollection qscoll = HttpUtility.ParseQueryString(Request.Url.Query);
            var result = paymwg.GetPaymentResult(qscoll);
            bool paymentSuccess = false;
            if (result.Status == mwg.payment.model.PayStatus.Success)
                paymentSuccess = true;
            #endregion

            #region Thanh toán thành công
            if (paymentSuccess)
            {
                var orderInfo = string.Format("Thanh toan don hang {0}", result.OrderId);
                var flagUpdate = thegioididong.business.svc.SvcCrmRepo.Current.CrmSaleOnlineUpdatePaymentStatus(result.OrderId, 1, result.TransactionNoString, string.Empty, orderInfo);
                if (flagUpdate > 0)
                {
                    var objOrder = thegioididong.business.svc.SvcCrmRepo.Current.GetDetailOrderById(result.OrderId);
                    var emailContent = string.Format("ZaloGateway thanh cong: {0}, Thời gian {1}, {2}, {3}, {4}, {5}",
                        result.OrderId,
                        DateTime.Now.ToString(),
                        orderInfo,
                        result.TransactionNoString,
                        Request.Url.ToString(),
                        objOrder == null ? "null" : JsonConvert.SerializeObject(objOrder)
                    );
                    EmailHelper.SendMailError(emailContent);
                }
                else
                {
                    new WebLibs.SystemMessage(CampaignName + " > TGDD > ORDERCONTROLLER.ZaloGateway > CrmSaleOnlineUpdatePaymentStatus", Request.Url.OriginalString, "Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId);
                    EmailHelper.SendMailError("ZaloGateway - Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId + " Thời gian " + DateTime.Now.ToString() + orderInfo + " " + result.TransactionNoString + " " + Request.Url.ToString());
                }
                objOS.IsPaymentSuccess = true;
                Session[SS_PAYMENT_CAMPAIGN] = objOS;
                Response.Redirect(UrlCampaign);
                return null;
            }
            #endregion

            #region Thanh toán trực tuyến thất bại
            SendSMSErrorPay((int)result.OrderId);
            objOS.IsPaymentSuccess = false;
            Session[SS_PAYMENT_CAMPAIGN] = objOS;
            Response.Redirect(UrlCampaign);
            return null;
            #endregion
        }
        public ActionResult OnePay()
        {
            if (Session[SS_PAYMENT_CAMPAIGN] == null) Response.Redirect(UrlCampaign, true);
            var objOS = Session[SS_PAYMENT_CAMPAIGN] as OrderSuccess;

            #region Lấy thông tin thanh toán và trạng thái
            string vpc_PaymentURL = ConfigurationManager.AppSettings["payment.onepay.vpc_PaymentURL"];
            string vpc_QueryDRURL = ConfigurationManager.AppSettings["payment.onepay.vpc_QueryDRURL"];
            string vpc_SecureHash = ConfigurationManager.AppSettings["payment.onepay.vpc_SecureHash"];
            string vpc_Merchant = ConfigurationManager.AppSettings["payment.onepay.vpc_Merchant"];
            string vpc_AccessCode = ConfigurationManager.AppSettings["payment.onepay.vpc_AccessCode"];
            var paymwg = new mwg.payment.service.OnePay(vpc_PaymentURL, vpc_QueryDRURL, vpc_Merchant, vpc_SecureHash, vpc_AccessCode);
            var qscoll = HttpUtility.ParseQueryString(Request.Url.Query);
            var result = paymwg.GetPaymentResult(qscoll);
            var paymentSuccess = result.Status == mwg.payment.model.PayStatus.Success;
            #endregion

            #region Thanh toán thành công
            if (paymentSuccess)
            {
                var orderInfo = string.Format("Thanh toan don hang {0}", result.OrderId);
                var flagUpdate = thegioididong.business.svc.SvcCrmRepo.Current.CrmSaleOnlineUpdatePaymentStatus(result.OrderId, 1, result.TransactionNoString, string.Empty, orderInfo);
                if (flagUpdate > 0)
                {
                    var objOrder = thegioididong.business.svc.SvcCrmRepo.Current.GetDetailOrderById(result.OrderId);
                    var emailContent = string.Format("OnePay thanh cong: {0}, Thời gian {1}, {2}, {3}, {4}, {5}",
                        result.OrderId,
                        DateTime.Now.ToString(),
                        orderInfo,
                        result.TransactionNoString,
                        Request.Url.ToString(),
                        objOrder == null ? "null" : JsonConvert.SerializeObject(objOrder)
                    );
                    EmailHelper.SendMailError(emailContent);
                }
                else
                {
                    new WebLibs.SystemMessage(CampaignName + " > TGDD > ORDERCONTROLLER.OnePay > CrmSaleOnlineUpdatePaymentStatus", Request.Url.OriginalString, "Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId);
                    EmailHelper.SendMailError("OnePay - Lỗi cập nhật trạng thái thanh toán trực tuyến OrderID:" + result.OrderId + " Thời gian " + DateTime.Now.ToString() + orderInfo + " " + result.TransactionNoString + " " + Request.Url.ToString());
                }
                objOS.IsPaymentSuccess = true;
                Session[SS_PAYMENT_CAMPAIGN] = objOS;
                Response.Redirect(UrlCampaign);
                return null;
            }
            #endregion

            #region Thanh toán trực tuyến thất bại
            SendSMSErrorPay((int)result.OrderId);
            objOS.IsPaymentSuccess = false;
            Session[SS_PAYMENT_CAMPAIGN] = objOS;
            Response.Redirect(UrlCampaign);
            return null;
            #endregion
        }
        private string CheckPaymentOrder(string strOrderId, string totalMoney, string fullName, string phoneNumber, int paymentMethod)
        {
            var returnURL = IsTGDD
                ? DomainName + UrlPath + "/Order/Pay123"
                : DomainName + "/thanh-toan-don-hang/123pay";
            var strOrderInfo = string.Format("Thanh toan don hang {0} so tien {1}", strOrderId, totalMoney);
            if (paymentMethod > 0)
                switch (paymentMethod)
                {
                    case (int)MethodType.ATM:
                        #region pay123
                        Pay123 pg = new Pay123();

                        object[] result = pg.CreateOrder(strOrderId, totalMoney, GetUserIP(), fullName, "", "U", "",
                   phoneNumber, "", "Thanh toan don hang " + strOrderId + " so tien " + totalMoney, returnURL, returnURL, returnURL, "");
                        if (result[0].ToString().Equals("1")) return result[2].ToString();
                        break;
                    #endregion
                    case (int)MethodType.VISA123PAY:
                        #region Visa pay123
                        Pay123 pgv = new Pay123();
                        object[] resultv = pgv.CreateOrder(strOrderId, "123PCC", totalMoney, GetUserIP(), fullName, "", "U", "",
                   phoneNumber, "", "Thanh toan don hang " + strOrderId + " so tien " + totalMoney, returnURL, returnURL, returnURL, "");
                        if (resultv[0].ToString().Equals("1")) return resultv[2].ToString();
                        break;
                    #endregion
                    case ConstantsHelper.PaymentType.VNpay_Atm:
                    case ConstantsHelper.PaymentType.VNpay_Visa:
                        #region VNPay
                        var urlCallBack = DomainName + UrlPath + "/Order/VNPay";
                        var vnp_Url = ConfigurationManager.AppSettings["vnp_Url"] ?? "";
                        var vnp_Api_Url = ConfigurationManager.AppSettings["vnpay_api_url"] ?? "";
                        var vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCodeGateWay"] ?? "";
                        var vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"] ?? "";
                        if (!IsTGDD)
                        {
                            vnp_Url = ConfigurationManager.AppSettings["VNPAY_URL"];
                            vnp_Api_Url = ConfigurationManager.AppSettings["VNPAY_API_URL"];
                            vnp_TmnCode = ConfigurationManager.AppSettings["VNP_TMNCODE"];
                            vnp_HashSecret = ConfigurationManager.AppSettings["VNP_HASHSECRET"];
                        }

                        mwg.payment.service.IPayMent paymwg = new mwg.payment.service.VNPay(urlCallBack, vnp_Url, vnp_Api_Url, vnp_TmnCode, vnp_HashSecret);
                        var bankCode = paymentMethod == ConstantsHelper.PaymentType.VNpay_Visa ? "INTCARD" : "VNBANK";
                        var OrderInfo = new mwg.payment.model.OrderInfo()
                        {
                            OrderId = decimal.Parse(strOrderId),
                            Amount = decimal.Parse(totalMoney),
                            UrlCallBack = urlCallBack,
                            IpAddress = GetUserIP(),
                            BankCode = bankCode,
                            OrderDescription = "VNPay - Thanh toan don hang " + strOrderId + " so tien " + totalMoney.ToString()
                        };
                        var pResult = paymwg.CreatePaymentURL(OrderInfo);
                        if (pResult.IsError == false)
                            return pResult.UrlRedirect;
                        break;

                    //case TransactionTypes.VNPayATM:
                    //case TransactionTypes.VNPayQR:
                    //    #region VNPay
                    //    HttpContext.Current.Session["SalesOrder" + so.Id] = so;
                    //    string vnp_Url = ConfigHelper.GetSettingValue<string>("VNPAY_URL");
                    //    string vnp_Api_Url = ConfigHelper.GetSettingValue<string>("VNPAY_API_URL");
                    //    string vnp_TmnCode = so.TransactionType == TransactionTypes.VNPayATM ? ConfigHelper.GetSettingValue<string>("VNP_TMNCODE") : ConfigHelper.GetSettingValue<string>("VNP_TMNCODEQR");
                    //    string vnp_HashSecret = ConfigHelper.GetSettingValue<string>("VNP_HASHSECRET");
                    //    var vnPay = new mwg.payment.service.VNPay("", vnp_Url, vnp_Api_Url, vnp_TmnCode, vnp_HashSecret);
                    //    info = new mwg.payment.model.OrderInfo();
                    //    info.OrderId = so.Id;
                    //    info.OrderDescription = "Thanh toan don hang " + so.Id;
                    //    info.Amount = DmxView.RoundDigital(so.SummaryTotal, 1);
                    //    info.IpAddress = Personalize.GetClientIp();
                    //    info.CreatedDate = DateTime.Now;
                    //    info.BankCode = so.TransactionType == TransactionTypes.VNPayATM ? "VNBANK" : "VNPAYQR";
                    //    info.UrlCallBack = ConfigHelper.GetSettingValue<string>("VNPAY_RETURN_URL");
                    //    var ret = vnPay.CreatePaymentURL(info);
                    //    return ret.UrlRedirect;
                    //    #endregion
                    #endregion
                    case ConstantsHelper.PaymentType.Alepay_Visa:
                    case ConstantsHelper.PaymentType.Alepay_Atm:
                        #region ALEPay 
                        var urlCallBackAlePay = DomainName + UrlPath + "/Order/AlepayGateway";

                        if (string.IsNullOrEmpty(fullName))
                            fullName = ViewHelper.GetRandomOnlyWord(4) + " " + ViewHelper.GetRandomOnlyWord(4);
                        if (fullName.Trim().Contains(" ") == false)
                            fullName = ViewHelper.GetRandomOnlyWord(4) + " " + fullName;
                        #region Ngân Lượng GateWay
                        var rqNew = new alepay_checkout_v2.RequestDataClient
                        {
                            amount = totalMoney.ToString(),
                            currency = "VND",
                            orderDescription = strOrderInfo,
                            totalItem = "1",
                            buyerName = fullName,
                            buyerEmail = "",
                            buyerPhone = phoneNumber,
                            buyerAddress = "Lầu 5 Etown 2, 364 Cộng Hòa, Q.Tân Bình, TP.Hồ Chí Minh",
                            buyerCity = "TP.Hồ Chí Minh",
                            buyerCountry = "Việt Nam",
                            orderCode = strOrderId,
                            paymentHours = "48",
                            allowDomestic = false,
                            installment = false,
                            checkoutType = "1"
                        };
                        if (paymentMethod == ConstantsHelper.PaymentType.Alepay_Atm)
                        {
                            rqNew.checkoutType = "4";
                            rqNew.allowDomestic = true;
                            rqNew.paymentMethod = "ATM_ON";
                        }
                        rqNew.month = 0;
                        rqNew.bankCode = "";
                        rqNew.returnUrl = urlCallBackAlePay + "?ran=" + Guid.NewGuid() + "&i=" + ViewHelper.Encrypt(strOrderId) + "&m=0" + ViewHelper.Encrypt("0");
                        rqNew.cancelUrl = urlCallBackAlePay + "?ran=" + Guid.NewGuid() + "&i=" + ViewHelper.Encrypt(strOrderId) + "&m=0" + ViewHelper.Encrypt("0");

                        var apiKeyNormal = ConfigurationManager.AppSettings["alepay_apiKeyNormal"] ?? "SfCKCzq9LOLUFopxZHsSLotu53BwOB";
                        var publicKeyNormal = ConfigurationManager.AppSettings["alepay_publicKeyNormal"] ?? "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDA0VVu0so+wNSSOITvB4zfwIygOMYDvocvdgRIC/cwSvLUEhEkTdI/ZZYgL/isHYbcu8E20hhSUP8sv0XXEe17UrAv1UaFsz0Aa7ulKLlZvg+RYdalior7X5KN73+L/ANxJ4gWotjLjYn1qFKi+RsOevmhSpz7nkM3AsBozmgpFQIDAQAB";
                        var checksumKeyNormal = ConfigurationManager.AppSettings["alepay_checksumKeyNormal"] ?? "sJFeha8i3OFVQIT504D0IBfx9ouZF4";
                        var apiURLNormal = ConfigurationManager.AppSettings["alepay_apiURLNormal"] ?? "https://alepay.vn";
                        mwg.payment.service.IPayMent paymwgAlepay = new mwg.payment.service.AlepayGateway(apiKeyNormal, publicKeyNormal, checksumKeyNormal, apiURLNormal);

                        #region Điện máy XANH
                        if (!IsTGDD)
                        {
                            apiKeyNormal = ConfigurationManager.AppSettings["AlepayCC_APIKEY"];
                            publicKeyNormal = ConfigurationManager.AppSettings["AlepayCC_PUBLICKEY"];
                            checksumKeyNormal = ConfigurationManager.AppSettings["AlepayCC_CHECKSUMKEY"];
                            apiURLNormal = ConfigurationManager.AppSettings["AlepayCC_API_URL"];
                        }
                        #endregion

                        var orderInfoAlepay = new mwg.payment.model.OrderInfo()
                        {
                            OrderId = decimal.Parse(strOrderId),
                            Amount = decimal.Parse(totalMoney),
                            UrlCallBack = rqNew.returnUrl,
                            IpAddress = GetUserIP(),
                            OrderDescription = "Alepay - Thanh toan don hang " + strOrderId + " so tien " + totalMoney.ToString(),
                            ExtData = rqNew
                        };
                        var pResultAlepay = paymwgAlepay.CreatePaymentURL(orderInfoAlepay);
                        if (pResultAlepay.IsError == false)
                            return pResultAlepay.UrlRedirect;
                        break;
                    #endregion
                    #endregion
                    case ConstantsHelper.PaymentType.ZaloGateway_Atm:
                    case ConstantsHelper.PaymentType.ZaloGateway_Visa:
                        #region zalo gate way
                        var urlCallBackZalo = DomainName + UrlPath + "/Order/ZaloGateway";
                        var AppId = Convert.ToInt32(ConfigurationManager.AppSettings["payment.zalo.appid"]);
                        var Key1 = ConfigurationManager.AppSettings["payment.zalo.key1"];
                        var Key2 = ConfigurationManager.AppSettings["payment.zalo.key2"];
                        var ApiBaseAddress = ConfigurationManager.AppSettings["payment.zalo.api_base_url"];
                        var GatewayApiBaseAddress = ConfigurationManager.AppSettings["payment.zalo.gateway"];
                        mwg.payment.service.IPayMent paymwgZaloPay = new mwg.payment.service.ZaloPay(AppId, Key1, Key2, ApiBaseAddress, GatewayApiBaseAddress);
                        var OrderInfoZaloPay = new mwg.payment.model.OrderInfo()
                        {
                            OrderId = decimal.Parse(strOrderId),
                            Amount = decimal.Parse(totalMoney),
                            UrlCallBack = urlCallBackZalo,
                            IpAddress = GetUserIP(),
                            BankCode = paymentMethod == ConstantsHelper.PaymentType.ZaloGateway_Visa ? "CC" : "",
                            PhoneNumber = phoneNumber,
                            OrderDescription = "ZaloGateway - Thanh toan don hang " + strOrderId + " so tien " + totalMoney.ToString()
                        };
                        var pResultZaloPay = paymwgZaloPay.CreatePaymentURL(OrderInfoZaloPay);
                        if (pResultZaloPay.IsError == false)
                            return pResultZaloPay.UrlRedirect;
                        #endregion
                        break;
                    case ConstantsHelper.PaymentType.OnePay_Atm:
                    case ConstantsHelper.PaymentType.OnePay_Visa:
                        #region onepay
                        var urlCallBackOnePay = DomainName + UrlPath + "/Order/OnePay";
                        var vpc_PaymentURL = System.Configuration.ConfigurationManager.AppSettings["payment.onepay.vpc_PaymentURL"];
                        var vpc_QueryDRURL = System.Configuration.ConfigurationManager.AppSettings["payment.onepay.vpc_QueryDRURL"];
                        var vpc_SecureHash = System.Configuration.ConfigurationManager.AppSettings["payment.onepay.vpc_SecureHash"];
                        var vpc_Merchant = System.Configuration.ConfigurationManager.AppSettings["payment.onepay.vpc_Merchant"];
                        var vpc_AccessCode = System.Configuration.ConfigurationManager.AppSettings["payment.onepay.vpc_AccessCode"];
                        var paymwgOnePay = new mwg.payment.service.OnePay(vpc_PaymentURL, vpc_QueryDRURL, vpc_Merchant, vpc_SecureHash, vpc_AccessCode);
                        var orderInfoOnePay = new mwg.payment.model.OrderInfo()
                        {
                            OrderId = decimal.Parse(strOrderId),
                            Amount = decimal.Parse(totalMoney),
                            UrlCallBack = urlCallBackOnePay,
                            IpAddress = GetUserIP(),
                            BankCode = ConstantsHelper.PaymentType.OnePay_Atm == paymentMethod ? "ATM" : "CC",
                            PhoneNumber = phoneNumber,
                            OrderDescription = "OnePay-DH " + strOrderId + " so tien " + totalMoney.ToString()
                        };

                        var pResultOnePay = paymwgOnePay.CreatePaymentURL(orderInfoOnePay);
                        if (pResultOnePay.IsError == false) return pResultOnePay.UrlRedirect;
                        #endregion
                        break;
                }
            return "";
        }
        private void SendSMSErrorPay(int iOrderId)
        {
            if (iOrderId <= 0) return;

            var sOrderCrmId = iOrderId.ToString();
            var result = thegioididong.business.svc.SvcProductRepo.Current.GetSaleOrderIpAdd(sOrderCrmId);
            if (result != null && result.Rows.Count > 0 && result.Rows[0]["ISSENDMAIL"].ToString() != "1")
            {
                var PhoneNumber = result.Rows[0]["PhoneNumber"].ToString();
                if (string.IsNullOrEmpty(PhoneNumber)) return;
                string sendSms = string.Format("Thanh toan online cho don hang {0} that bai. Moi thac mac vui long lien he TD {1} de duoc ho tro", sOrderCrmId, Hotline());
                string smsResult = thegioididong.business.svc.SvcCrmSmsRepo.Current.SendSMS(PhoneNumber, sendSms, "TGDD", "WEB", "Loi Pay Error", 1);
                thegioididong.business.svc.SvcProductRepo.Current.SaleOrderUpdateSendMail(sOrderCrmId, 1);
            }
        }
        #endregion

        private string GetPromotionText(int promotionType, int productId)
        {
            var objProduct = BindingProduct().FirstOrDefault(p => p.ProductId == productId);
            if (objProduct.Promotions == null || objProduct.Promotions.Count == 0) return string.Empty;
            var promotionText = "";
            if (objProduct.Promotions != null && objProduct.Promotions.Any())
            {
                if (promotionType > 0)
                {
                    promotionText += objProduct.Promotions.FirstOrDefault(p => p.PromotionId == promotionType).PromotionName;
                }
                else
                {
                    promotionText += string.Join(" + ", objProduct.Promotions.Select(p => p.PromotionName));
                }
            }
            return promotionText;
        }
        private string GetNotePreOrder(int method, int promotionType, int productId, decimal totalMoney)
        {
            var note = string.Format("Đơn hàng chương trình {0}", CampaignName);
            note += " - " + GetMethodNameByType(method);
            if (method == (int)MethodType.AtStore)
            {
                note += " - CC tạo đơn hàng & mời khách đặt cọc tại siêu thị";
            }
            else if (method == (int)MethodType.AtHome)
            {
                note += string.Format(" - CC tạo đơn hàng & xác nhận địa chỉ khách, NVST đến nhà khách giao phiếu cọc & thu {0} (liên hệ anh Nhanh để rõ hơn quy trình thu cọc tại nhà)", thegioididong.business.helper.ViewHelper.FormatCurrency(totalMoney));
            }
            else if (method == (int)MethodType.Installment)
            {
                var product = BindingProduct().FirstOrDefault(p => p.ProductId == productId);
                note += string.Format(" - mua TRẢ GÓP {0} - CC tạo đơn hàng & mời khách đặt cọc tại siêu thị khách làm hồ sơ", product.IsInstallment0Percent ? "0%" : "CÓ LÃI SUẤT");
            }
            var promotionText = GetPromotionText(promotionType, productId).ToUpper();
            if (!string.IsNullOrEmpty(promotionText)) note += " - Khuyến mãi " + promotionText;
            if (!IsRunLive && (method == (int)MethodType.VISA123PAY || method == (int)MethodType.ATM))
            {
                note = "IT TEST THANH TOÁN TRỰC TUYẾN - CC HỦY GIÚP NHÉ. - " + note;
            }

            return note;
        }
        private int CountOrder()
        {
            var cacheKeyOrder = SS_ORDER_CAMPAIGN + GetUserIP();
            var count = CacheHelper.Get(cacheKeyOrder) as string;
            return string.IsNullOrEmpty(count) ? 0 : int.Parse(count);
        }
        #endregion
    }
}
