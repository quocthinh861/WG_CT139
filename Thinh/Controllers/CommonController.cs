using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Thinh.Controllers
{
    public class CommonController : BaseController 
    {
        public ActionResult BoxComment()
        {
            var objComment = new Models.Comment() {
                ObjectId = ObjectId,
                ObjectType = ObjectType,
                CampaignName = CampaignName,
                Url = UrlCampaign,
                IsMobile = IsMobileMode,
                SiteId = 1
            };
            return PartialView(objComment);
        }
        public ActionResult BoxNews()
        {
            var pageSize = 5;
            var total = 0;
            var res = new List<thegioididong.business.ApiNews.NewsBO>();
            var lstnews = thegioididong.business.api.ApiNewsRepo.Current.SearchNews(Program.KeywordNew, 999, 0, pageSize, ref total, IsRemoveCache);
            var max = 3;
            if (lstnews != null && lstnews.Count > max) {
                var count = lstnews.Count;
                var idx = new Random().Next(0, count == max ? 1 : count - max);
                var maxItem = count < max + 1 ? count : max;
                for (int i = 0; i < maxItem; i++) res.Add(lstnews[idx + i]);
            } else {
                res = lstnews;
            }

            #region Lấy video của tin tức khai báo trong tool
            var videoId = Program.VideoId;
            if (!string.IsNullOrEmpty(videoId)) {
                var arr = videoId.Split('|');
                videoId = arr[0];
            }
            ViewBag.VideoId = videoId;
            #endregion

            return PartialView(res);
        }
        public ActionResult BoxConfig(int productId = 0)
        {
            var products = BindingProduct();
            var product = productId == 0 ? products.FirstOrDefault() : products.FirstOrDefault(p => p.ProductId == productId);

            ViewBag.Products = products;
            return PartialView(product);
        }
        public ActionResult PopupConfig(int productId = 0)
        {
            var product = productId == 0 ? BindingProduct().FirstOrDefault() : BindingProduct().FirstOrDefault(p => p.ProductId == productId);
            return PartialView(product);
        }
        public ActionResult PopupHTML(int htmlId)
        {
            return PartialView(htmlId);
        }
        public ActionResult PopupVideo(string videoId, string name)
        {
            var objVideo = new Models.Video() { VideoId = videoId, Name = name };
            return PartialView(objVideo);
        }
        public ActionResult CatchAll404()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}
