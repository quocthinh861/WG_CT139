$(document).ready(function () {
    Video();
    Default();
    ScrollBar();
    InitOwlSlider();
    LazyBG();
    SetTop();
});

function Default() {
    setTimeout(function () {
        $('section.news span.loading').remove();
        var $iframe = $('section.news iframe');
        $iframe.attr('src', $iframe.data('src'));

        $('.youtube').each(function () {
            var videoId = $(this).data('videoid');
            if (videoId.length > 11) {
                $(this).replaceWith('<iframe class="iframe" src="https://www.youtube.com/embed/videoseries?list=' + videoId + '&autoplay=1" frameborder="0" allow="encrypted-media" allowfullscreen=""></iframe>');
            }
        });
    }, 5000);

    $('.p-color>a').click(function () {
        $('.p-color>a.active').removeClass('active');
        var img = $(this).data('img');
        $('.p-color>a').each(function () {
            if ($(this).data('img') == img) {
                $(this).addClass('active');
            }
        })
        $(this).parent().parent().find('.p-img').attr('src', img);
    });
    $('.sub-gender>span').click(function () {
        $('.sub-gender>span.active').removeClass('active');
        $(this).addClass('active');
        $('input[name=hdGender]').val($(this).data('value'));
    });
    $('.config div.tab>a').click(function (e) {
        e.preventDefault();
        $('.config').addClass('hide');
        var $item = $('' + $(this).attr('href'));
        $item.removeClass('hide');
        $('.sticky a.cf').attr('href', $(this).attr('href'));
        var height = $('.sticky').html() != undefined ? $('.sticky').height() : 0;
        $('body,html').animate({ scrollTop: $item.offset().top - height }, 500);

        $item.find('.p-color').find('a:nth-child(2)').click();
        $item.find('img').each(function () {
            $(this).attr('src', $(this).data('src'));
            $(this).removeAttr('data-src');
        });
    });

    $(document).on('click', '.storeexp div.tab>a', function () {
        $('.storeexp div.tab>a.active').removeClass('active');
        $(this).addClass('active');
        $('input[name=hdType]').val($(this).data('type'));
        StoreListEXP();
    });
    $(document).on('click', '.storeexp .province>span', function () {
        $('.storeexp .district .select').hide();
        $(this).parent().find('div.select').slideToggle();
    });
    $(document).on('click', '.storeexp .district>span', function () {
        $('.storeexp .province .select').hide();
        $(this).parent().find('div.select').slideToggle();
    });
    $(document).on('keyup', '.storeexp input[name=key]', function () {
        var term = this.value;
        $(this).parent().next().find('a').each(function () {
            var v = $(this).text();
            var rEscape = /[\-\[\]{}()*+?.,\\\^$|#\s]/g;
            var regex = new RegExp(term.replace(rEscape, "\\$&"), 'gi');
            var regex2 = new RegExp(RemoveVietNamChar(term).replace(rEscape, "\\$&"), 'gi');
            if (v.search(regex) !== -1 || RemoveVietNamChar(v).search(regex) !== -1 || RemoveVietNamChar(v).search(regex2) !== -1) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });
    $(document).on('click', '#ddlProvinceEXP .item>a', function () {
        StoreListEXP($(this).data('value'), -1);
    });
    $(document).on('click', '#ddlDistrictEXP .item>a', function () {
        StoreListEXP($('input[name=hdProvinceEXPId]').val(), $(this).data('value'));
    });
    $(document).on('click', '.user-list .model>a', function () {
        $('.user-list .model>a.active').removeClass('active');
        $(this).addClass('active');
        $('input[name=hdModel]').val($(this).data('model'));
        SearchUserOrder();
    });
}
function Video() {
    $('.youtube').click(function () {
        $('.video').removeAttr('style');
        var videoId = $(this).data('videoid');
        if (videoId.length > 11) {
            $(this).replaceWith('<iframe class="iframe" src="https://www.youtube.com/embed/videoseries?list=' + videoId + '&autoplay=1" frameborder="0" allow="autoplay; encrypted-media" allowfullscreen=""></iframe>');
        } else {
            $(this).replaceWith('<iframe class="iframe" src="https://www.youtube.com/embed/' + videoId + '?rel=0&modestbranding=1&showinfo=0&autoplay=1" frameborder="0" allowfullscreen></iframe>');
        }
    });
}
function SetTop() {
    var pathname = window.location.href;
    if (pathname.indexOf('#') > 0) {
        var itemId = pathname.substring(pathname.indexOf('#'));
        if (itemId == '#dat-hang') { Order(-1, -1); window.history.replaceState('', '', rootUrl); }
        else if (itemId == '#tra-gop') { Order(6, -1); window.history.replaceState('', '', rootUrl); }
        else {
            var top = $(itemId).offset().top;
            var minus = $(itemId).data('top') == undefined ? 0 : $(itemId).data('top');
            $('body,html').animate({ scrollTop: top - minus }, 800);
        }
    }
    if (method != undefined && method != "-2") {
        if (method == "6") { Order(6, productId); }
        else { Order(-1, productId); }
    }
}
function Countdown() {
    //2016/01/08 9:59:59 AM 
    $('.clock').each(function () {
        var time = $(this).data('time');
        var showday = $(this).data('showday');
        var text = $(this).data('text');
        var reload = $(this).data('reload');
        var itemId = '#' + $(this).attr('id');

        $(itemId).countdown(time, function (event) {
            if (showday == 1) {
                var $this = $(this).html(event.strftime('' +
                    '<b>' + text + '</b>' +
                    '<span><b>%D</b><i>ngày</i></span><em>:</em>' +
                    '<span><b>%H</b><i>giờ</i></span><em>:</em>' +
                    '<span><b>%M</b><i>phút</i></span><em>:</em>' +
                    '<span><b>%S</b><i>giây</i></span>'));
            } else {
                var totalHours = event.offset.totalDays * 24 + event.offset.hours;
                var $this = $(this).html(event.strftime('' +
                    '<b>' + text + '</b>' +
                    '<span><b>' + totalHours + '</b><i>giờ</i></span><em>:</em>' +
                    '<span><b>%M</b><i>phút</i></span><em>:</em>' +
                    '<span><b>%S</b><i>giây</i></span>'));
            }
        });

        if (reload == 1) {
            $(itemId).countdown(time).on('finish.countdown', function (event) {
                window.location.reload();
            });
        }
    });
}
function LazyBG() {
    $(window).scroll(function () {
        var top = $(this).scrollTop();
        $('.lazybg').each(function () {
            var minus = $(this).offset().top - top;
            if (minus <= screen.height) {
                $(this).css('background-image', 'url(' + $(this).data('background') + ')');
                $(this).removeClass('lazybg');
                $(this).removeAttr('data-background');
            }
        });
    });
}

var submitEmailSubscribeFlag = false;
function SubmitEmailSubscribe() {
    if (submitEmailSubscribeFlag) return false;
    submitEmailSubscribeFlag = true;
    var data = GetAllFormData('#frmEmailSubscribe');

    $('#dlding').show();
    $.ajax({
        type: "POST",
        cache: false,
        data: data,
        url: rootUrl + "/Home/SubmitEmailSubscribe",
        success: function (e) {
            $('#dlding').fadeOut();
            submitEmailSubscribeFlag = false;
            if (e == null || e == '') {
                alert('Không thể xác thực thông tin, vui lòng thử lại sau');
                return false;
            }
            if (e.status == -1) {
                alert(e.error);
                return false;
            }

            if (e.status == 1) {
                alert(e.error);
                $('#frmEmailSubscribe input[name=txtFullName]').val('');
                $('#frmEmailSubscribe input[name=txtPhoneNumber]').val('');            }
        }
    })
    return false;
};

var flagPopupConfig = false;
function PopupConfig(productId) {
    if (flagPopupConfig) return;
    flagPopupConfig = true;
    $('#dlding').show();
    $.ajax({
        url: rootUrl + '/Common/PopupConfig',
        type: 'POST',
        data: { productId: productId },
        success: function (e) {
            flagPopupConfig = false;
            $('#dlding').fadeOut();
            AddColorBox('#popup', '#lpopup', e);
            ScrollBar();
        }
    });
}
var flagPopupHTML = false;
function PopupHTML(htmlId) {
    if (flagPopupHTML) return;
    flagPopupHTML = true;
    $('#dlding').show();
    $.ajax({
        url: rootUrl + '/Common/PopupHTML',
        type: 'POST',
        data: { htmlId: htmlId },
        success: function (e) {
            flagPopupHTML = false;
            $('#dlding').fadeOut();
            AddColorBox('#popup', '#lpopup', e);
            ScrollBar();
        }
    });
}
var flagPopupVideo = false;
function PopupVideo(videoId, name) {
    if (flagPopupVideo) return;
    flagPopupVideo = true;
    $('#dlding').show();
    $.ajax({
        url: rootUrl + '/Common/PopupVideo',
        type: 'POST',
        data: { videoId: videoId, name: name },
        success: function (e) {
            flagPopupVideo = false;
            $('#dlding').fadeOut();
            AddColorBox('#popup', '#lpopup', e);
        }
    });
}

function InitOwlSlider() {
    $(".owl-kvs").owlCarousel({
        singleItem: true,
        lazyLoad: true,
        navigation: true,
        navigationText: ["", ""],
        pagination: true
    });
    $(".owl-slider").owlCarousel({
        singleItem: true,
        lazyLoad: true,
        navigation: true,
        navigationText: ["", ""],
        pagination: false,
        afterMove: function (elem) {
            var idx = this.currentItem;
            $('.view-large').attr('href', 'javascript:OpenPhotoSwipe(' + idx + ')');
        }
    });
    $("#owl-news").owlCarousel({
        lazyLoad: true,
        navigation: true,
        navigationText: ["", ""],
        paginationNumbers: true,
        autoPlay: true,
        itemsCustom: [[1199, 4], [979, 4], [640, 3], [480, 2], [414, 2], [375, 2], [360, 2], [320, 2]]
    });
}
function OpenPhotoSwipe(idx) {
    if (typeof PhotoSwipe == 'undefined') {
        $.getScript(rootUrl + "/Scripts/photoswipe/photoswipe.min.js").done(function (script, textStatus) {
            $.getScript(rootUrl + "/Scripts/photoswipe/photoswipe-ui-default.min.js").done(function (script, textStatus) {
                InitPhotoSwipe(idx);
            });
        });
    } else {
        InitPhotoSwipe(idx);
    }
}
var flagInitPhotoSwipe = false;
function InitPhotoSwipe(idx) {
    if (flagInitPhotoSwipe) return;
    flagInitPhotoSwipe = true;

    var pswpElement = document.querySelectorAll('.pswp')[0];
    // build items array
    var items = [];
    $('.gallery a.item').each(function () {
        var $this = $(this);
        var item = { src: $this.data('src'), w: $this.data('width'), h: $this.data('height') };
        items.push(item);
    });

    // define options (if needed)
    var options = {
        index: idx,
        history: false,
        focus: false,
        showAnimationDuration: 0,
        hideAnimationDuration: 0,
        zoomEl: true
    };

    // Initializes and opens PhotoSwipe
    var gallery = new PhotoSwipe(pswpElement, PhotoSwipeUI_Default, items, options);
    gallery.init();
    gallery.listen('destroy', function () {
        flagInitPhotoSwipe = false;
    });
}

var flagStoreListEXP = false;
var StoreListEXP = function (provinceId, districtId) {
    if (flagStoreListEXP) return;
    flagStoreListEXP = true;

    $('#dlding').show();
    $('#ddlStoreEXP>span').remove();
    $('#ddlStoreEXP').html('<span class="no">Đang tải danh sách siêu thị...</span>')
    var type = $('input[name=hdType]').val() != undefined ? $('input[name=hdType]').val() : 0;

    $.ajax({
        type: "POST",
        cache: false,
        data: { provinceId: provinceId, districtId: districtId, type: type },
        url: rootUrl + '/Home/BoxStore',
        success: function (e) {
            flagStoreListEXP = false;
            $('#dlding').fadeOut();
            if (e != null && e != '') {
                $('#storeexp').replaceWith(e);
                ScrollBar();
            }
        }
    });
}