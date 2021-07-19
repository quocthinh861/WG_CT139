$(document).ready(function () {
    AddFacebookLikeBox();
    ImgLazyLoad();
    ScrollTo();
    WowInit();

    $(window).scroll(function () {
        LoadJsComment();
        //lazy load
        if ($("img.lazy").length > 0)
            $("img.lazy").lazyload();
        if ($(this).scrollTop() > 90) {
            $('.sticky').addClass('fixed');
        } else {
            $('.sticky').removeClass('fixed');
        }
    });
});

function GetAllFormData(form) {
    var unindexed_array = $(form).serializeArray();
    var indexed_array = {};

    $.map(unindexed_array, function (n, i) {
        indexed_array[n['name']] = n['value'];
    });
    return indexed_array;
}
function AddFacebookLikeBox() {
    setTimeout(function () {
        $('.like-fanpage').each(function () {
            var element = $(this);
            var width = element.data('width') == undefined ? '150' : element.data('width');
            var layout = element.data('layout') == undefined ? 'button_count' : element.data('layout');
            var url = element.data('url') == undefined ? 'https://www.facebook.com/thegioididongcom' : element.data('url');
            element.html('<iframe src="//www.facebook.com/plugins/like.php?href=' + url + '&amp;width&amp;layout=' + layout + '&amp;action=like&amp;show_faces=false&amp;share=true&amp;height=35" scrolling="no" frameborder="0" style="border:none; overflow:hidden; width:' + width + 'px; height:40px;" allowTransparency="true"></iframe>')
        });
    }, 5000);
}

function CopyTextToClipboard(text) {
    var textArea = document.createElement("textarea");

    // Place in top-left corner of screen regardless of scroll position.
    textArea.style.position = 'fixed';
    textArea.style.top = 0;
    textArea.style.left = 0;

    // Ensure it has a small width and height. Setting to 1px / 1em
    // doesn't work as this gives a negative w/h on some browsers.
    textArea.style.width = '2em';
    textArea.style.height = '2em';

    // We don't need padding, reducing the size if it does flash render.
    textArea.style.padding = 0;

    // Clean up any borders.
    textArea.style.border = 'none';
    textArea.style.outline = 'none';
    textArea.style.boxShadow = 'none';

    // Avoid flash of white box if rendered for any reason.
    textArea.style.background = 'transparent';


    textArea.value = text;

    document.body.appendChild(textArea);

    textArea.select();

    try {
        var successful = document.execCommand('copy');
        var msg = successful ? 'successful' : 'unsuccessful';
        console.log('Copying text command was ' + msg);
    } catch (err) {
        console.log('Oops, unable to copy');
    }

    document.body.removeChild(textArea);
}
function AddColorBox(idResult, objectId, ajaxResult) {
    if (typeof (ajaxResult) == 'object') {
        if (ajaxResult.status == -1) {
            alert(ajaxResult.error);
        }
    } else {
        if (ajaxResult == null || ajaxResult == '') return;
        if ($(idResult).length == 0) {
            $('body').addClass('hidden').append(ajaxResult);
            //$('section, footer').addClass('fixbody');
            $(objectId).colorbox({
                inline: true, closeButton: false, maxHeight: '90%', fixed: true,
                onClosed: function () {
                    $(idResult).remove();
                    $('body').removeClass('hidden');
                    $('section, footer').removeClass('fixbody');
                }
            });
            $(objectId).trigger('click').remove();
        } else {
            $(idResult).replaceWith(ajaxResult);
            $.colorbox.resize({ width: $(idResult).width() + 'px' });
            $(objectId).remove();
        }
        //$(window).on('resize', function () {
        //    $.colorbox.resize({ width: $(idResult).width() + 'px' });
        //});
    }
}
function BindGallery(relName) {
    $('.' + relName).colorbox({ rel: relName, innerWidth: "92%", innerHeight: "80%" });
}
function ImgLazyLoad() {
    $("img.lazy, img.imgU").lazyload({
        effect: "fadeIn"
    });
}
function ScrollTo() {
    $('.scrollto').on('click', function (e) {
        e.preventDefault();
        var tmp = $(this).attr('href');
        var height = $('.sticky').height();
        var minus = $('.sticky').hasClass('fixed') ? height : height * 2;
        var top = $(this).data('top') == undefined ? 0 : $(this).data('top');
        var top = $('' + tmp).offset().top - minus - top;

        $('body,html').animate({ scrollTop: top }, 500);
    });
}
function ScrollBar() {
    $('.scrollbar').perfectScrollbar();
}
function WowInit() {
    var wow = new WOW({
        offset: 50,        // distance to the element when triggering the animation (default is 0)
        mobile: true       // trigger animations on mobile devices (default is true)
    });
    wow.init();
}

var flsc = false;
function LoadJsComment() {
    if (flsc == true) return;
    if (typeof cmtaddcommentclick == 'undefined') {
        //Chỉ load một lần thôi
        flsc = true;
        var tgddc = document.createElement('script');
        tgddc.type = 'text/javascript';
        tgddc.async = true;
        tgddc.src = jsCommentUrl;
        (document.getElementsByTagName('head')[0] || document.getElementsByTagName('body')[0]).appendChild(tgddc);

        setTimeout(function () {
            //cmtInitEvent();
        }, 800);
    }
}

//#region Tracking TGDD
if (isTGDD == 1) {
    function getQuerystring(key, default_) {
        if (default_ == null) default_ = "";
        key = key.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
        var regex = new RegExp("[\\?&]" + key + "=([^&#]*)");
        var qs = regex.exec(window.location.href);
        if (qs == null)
            return default_;
        else
            return qs[1];
    }

    var tgddctr_urlroot = '//www.thegioididong.com/tracking';
    var sr = document.createElement("script");
    sr.type = 'text/javascript';
    sr.src = '//wurfl.io/wurfl.js';
    sr.async = true;
    sr.defer = true;
    document.getElementsByTagName('head')[0].appendChild(sr);

    window.onload = function () {
        var isMobile = 0;
        var sFactory = '';
        var sDevice = '';
        try {
            if (WURFL.is_mobile == true)
                isMobile = 1;
            sFactory = WURFL.form_factor;
            sDevice = WURFL.complete_device_name;
        } catch (ex) {
        }
        var currURL = encodeURIComponent(document.URL);
        var keyword = encodeURIComponent(getQuerystring("key"));
        var categoryid = -1;
        var SessionId = '';
        //CategoryID
        if (typeof (GL_CATEGORYID) != 'undefined')
            categoryid = GL_CATEGORYID;
        if (categoryid == -1 && $('#hdCategoryID').length > 0)
            categoryid = $('#hdCategoryID').val();
        //productID
        var iProductid = -1;
        if (typeof (ProductID) != 'undefined')
            iProductid = ProductID;
        if ((iProductid == 0 || iProductid == -1) && typeof (GL_ProductID) != 'undefined')
            iProductid = GL_ProductID;
        if (iProductid == -1 && typeof (productid) != 'undefined')
            iProductid = productid;
        //Keyword
        if (keyword == undefined)
            keyword = '';
        //Urlreferrer
        var urlrefer = encodeURIComponent(document.referrer);
        if (urlrefer == undefined)
            urlrefer = '';
        if (typeof (mysessionid) != 'undefined')
            SessionId = mysessionid;
        //Screen width
        var iHeight = window.innerHeight;
        var iWidth = window.innerWidth;
        var sScreenSize = iWidth + 'x' + iHeight;

        var urlpara = tgddctr_urlroot + "/tracking/load?urlrefer=" + urlrefer
            + "&key=" + keyword
            + "&categoryid=" + categoryid
            + "&productid=" + iProductid
            + "&sessionid=" + SessionId
            + "&pageurl=" + currURL
            + "&ismobile=" + isMobile
            + "&device=" + sDevice
            + "&screen=" + sScreenSize
            + "&factory=" + sFactory;
        var imgload = "<img width=\"0\" heigth=\"0\" id=\"imgtrack\" src=\"" + urlpara + "\" />";
        $("body").append(imgload);
    };
}
//#endregion