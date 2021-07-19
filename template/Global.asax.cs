using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;

namespace template
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
        protected void Application_BeginRequest()
        {
            #region Https Redirect
            string sourceUrl = Request.Url.ToString();
            string userAgent = Request.ServerVariables["HTTP_USER_AGENT"];
            if (!Request.IsLocal && !sourceUrl.Contains("beta.")
                && !sourceUrl.Contains("alpha.")
                && !sourceUrl.Contains("test.")
                && !sourceUrl.Contains("thegioididong.vn")
                && !sourceUrl.Contains("staging.")
                && (userAgent != null && !userAgent.Contains("Haproxy")))
            {
                string xProto = Request.Headers["X-Proto"];
                if (string.IsNullOrEmpty(xProto) || xProto != "https")
                {
                    Response.RedirectPermanent(sourceUrl.Replace("http://", "https://"));
                }
            }
            #endregion

            #region SetCookie Device
            string domainValue = ".tgdd2015.com";
            if (Request.Url.ToString().Contains(".thegioididong.com"))
            {
                domainValue = ".thegioididong.com";
            }
            else if (Request.Url.ToString().Contains(".trananh.vn"))
            {
                domainValue = ".trananh.vn";
            }
            else if (Request.Url.ToString().Contains(".tag.com"))
            {
                domainValue = ".tag.com";
            }
            else if (Request.Url.ToString().Contains(".dienmayxanh.com"))
            {
                domainValue = ".dienmayxanh.com";
            }
            else if (Request.Url.ToString().Contains(".dienmay.com"))
            {
                domainValue = ".dienmay.com";
            }
            else if (Request.Url.ToString().Contains(".dmx.com"))
            {
                domainValue = ".dmx.com";
            }
            else if (Request.Url.ToString().Contains(".vuivui.com"))
            {
                domainValue = ".vuivui.com";
            }
            else if (Request.Url.ToString().Contains(".vv.com"))
            {
                domainValue = ".vv.com";
            }

            // lưu cookie phiên bản khi chuyển từ bản mobile
            var name = GetRequestName();
            var cookieName = GetCookieName();
            if (Request.QueryString[name] != null)
            {
                switch (Request.QueryString[name])
                {
                    case "desktop":
                        {
                            var versionCookie = new HttpCookie(cookieName, "DESKTOP")
                            {
                                Expires = DateTime.Now.AddDays(30),
                                Domain = domainValue
                            };
                            Response.Cookies.Add(versionCookie);
                            Response.Redirect(
                                Request.Url.ToString()
                                    .Replace(string.Format("?{0}=desktop", name), string.Empty)
                                    .Replace(string.Format("&{0}=desktop", name), string.Empty));

                        }
                        break;
                    case "full":
                        {
                            var versionCookie = new HttpCookie(cookieName, "DESKTOP")
                            {
                                Expires = DateTime.Now.AddDays(30),
                                Domain = domainValue
                            };
                            Response.Cookies.Add(versionCookie);
                            Response.Redirect(
                                Request.Url.ToString()
                                    .Replace(string.Format("?{0}=full", name), string.Empty)
                                    .Replace(string.Format("&{0}=full", name), string.Empty));

                        }
                        break;
                    case "mobile":
                        {

                            var versionCookie = new HttpCookie(cookieName, "MOBILE")
                            {
                                Expires = DateTime.Now.AddDays(30),
                                Domain = domainValue
                            };
                            Response.Cookies.Add(versionCookie);
                            Response.Redirect(
                                Request.Url.ToString()
                                    .Replace(string.Format("?{0}=mobile", name), string.Empty)
                                    .Replace(string.Format("&{0}=mobile", name), string.Empty));
                        }
                        break;
                }
            }
            #endregion

            #region SetCookie Site
            if (Request.QueryString["site"] == null) return;
            var siteName = ((int)Site.TGDD).ToString();
            if (Request.QueryString["site"] == "dmx") siteName = ((int)Site.DMX).ToString();
            var siteCookie = new HttpCookie("CK_SITE", siteName)
            {
                Expires = DateTime.Now.AddDays(30),
                Domain = domainValue
            };
            Response.Cookies.Add(siteCookie);
            Response.Redirect(Request.Url.ToString().Replace("?site=tgdd", string.Empty)
                                                    .Replace("?site=dmx", string.Empty)
                                                    .Replace("?site=ta", string.Empty)
                                                    .Replace("?site=vv", string.Empty)
                                                    .Replace("&site=tgdd", string.Empty)
                                                    .Replace("&site=dmx", string.Empty)
                                                    .Replace("&site=ta", string.Empty)
                                                    .Replace("&site=vv", string.Empty));
            #endregion
        }
        protected void Application_Error(Object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            var httpException = exception as HttpException;
            if (httpException != null && !Request.IsLocal)
            {
                Response.Clear();
                Server.ClearError();
                Response.TrySkipIisCustomErrors = true;
                IController redirectController = new Controllers.CommonController();
                var routeData = new RouteData();
                try
                {
                    routeData.Values.Add("controller", "Common");
                    routeData.Values.Add("action", "CatchAll404");
                    redirectController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
                }
                catch
                {
                    redirectController = new Controllers.CommonController();
                    routeData = new RouteData();
                    routeData.Values.Add("controller", "Common");
                    routeData.Values.Add("action", "Default404");
                    redirectController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
                }
            }
        }

        #region Support
        public static bool ContextCheck(HttpContextBase objHttpContextBase)
        {
            return System.Web.HttpContext.Current.Request.Url.ToString().Contains("www.thegioididong") ? IsMobile() : IsMobileMode();
        }

        public static bool IsMobileMode()
        {
            if (IsFromMobileApp()) return true;

            var request = HttpContext.Current.Request;
            var contextBase = new HttpContextWrapper(HttpContext.Current);
            // Auto detect
            if (request.QueryString["viewtype"] != null && request.QueryString["viewtype"].ToString().ToLower() == "mobile")
                return true;
            var cookieName = GetCookieName();
            if (request.Cookies[cookieName] != null &&
               request.Cookies[cookieName].Value == "MOBILE")
            {
                if (!request.Browser.IsMobileDevice)
                {
                    contextBase.ClearOverriddenBrowser();
                }
                contextBase.SetOverriddenBrowser(BrowserOverride.Mobile);
                return true;
            }
            if (request.Cookies[cookieName] != null &&
                request.Cookies[cookieName].Value == "DESKTOP")
            {
                if (request.Browser.IsMobileDevice)
                {
                    contextBase.ClearOverriddenBrowser();
                }
                contextBase.SetOverriddenBrowser(BrowserOverride.Desktop);
                return false;
            }

            string u = request.ServerVariables["HTTP_USER_AGENT"];
            var b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if ((u != null && u.Length >= 4 && (b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)))) ||
                request.Browser.IsMobileDevice)
            {
                contextBase.SetOverriddenBrowser(BrowserOverride.Mobile);
                return true;
            }
            contextBase.SetOverriddenBrowser(BrowserOverride.Desktop);
            return false;
        }
        public static bool IsFromMobileApp()
        {
            string u = HttpContext.Current.Request.ServerVariables["HTTP_USER_AGENT"];
            var content = HttpContext.Current;

            if (content != null && content.Session != null && content.Session["TGDD_IsFromMobileApp"] != null)
                return true;

            if (!string.IsNullOrEmpty(u) && u.ToLower().Contains("tgdd-mobile-app"))
            {
                if (content != null && content.Session != null)
                {
                    content.Session["TGDD_IsFromMobileApp"] = 1;
                }
                return true;

            }
            if (content != null && content.Session != null)
            {
                content.Session.Remove("TGDD_IsFromMobileApp");
            }
            return false;
        }
        public static bool IsMobile()
        {
            var request = HttpContext.Current.Request;
            var contextBase = new HttpContextWrapper(HttpContext.Current);
            var u = request.ServerVariables["HTTP_USER_AGENT"];
            var b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if ((b.IsMatch(u) || v.IsMatch(u.Substring(0, 4))) || request.Browser.IsMobileDevice)
            {
                contextBase.SetOverriddenBrowser(BrowserOverride.Mobile);
                return true;
            }
            contextBase.SetOverriddenBrowser(BrowserOverride.Desktop);
            return false;
        }
        public static bool IsTGDD()
        {
            return SiteId == (int)Site.TGDD;
        }
        public static bool IsDMX()
        {
            return SiteId == (int)Site.DMX;
        }
        public static string FrontEndUrl
        {
            get
            {
                if (IsDMX()) return "https://www.dienmayxanh.com";

                return "https://www.thegioididong.com";
            }
        }
        public static int SiteId
        {
            get
            {
                var request = System.Web.HttpContext.Current.Request;
                if (request.Cookies["CK_SITE"] == null)
                {
                    var url = request.Url.ToString();
                    if (url.Contains(".dmx.com") || url.Contains(".dienmay.com") || url.Contains(".dienmayxanh.com"))
                        return (int)Site.DMX;
                    return (int)Site.TGDD;
                }
                var siteId = 1;
                var isParse = int.TryParse(request.Cookies["CK_SITE"].Value, out siteId);
                return isParse ? siteId : (int)Site.TGDD;
            }
        }
        public static string GetCookieName()
        {
            return IsTGDD() ? "CK_TGDD_WEB_VERSION" : "DMX_View";
        }
        public static string GetRequestName()
        {
            return IsTGDD() ? "sclient" : "view";
        }
        public enum Site
        {
            TGDD = 1,
            DMX = 2
        }
        #endregion
    }
}
