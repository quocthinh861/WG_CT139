var installment = 6;
function OrderEvent(method) {
    $('.o-group>span').on('click', function () {
        $(this).parent().removeClass('o-error');
        $('.o-group>span.active').removeClass('active');
        $('.o-model>span.active').removeClass('active');
        $('.o-color>span.active').removeClass('active');
        $(this).addClass('active');
        var groupId = $(this).data('group');
        $('input[name=txtGroupId]').val(groupId);
        $('.price').html($(this).data('price'));

        $('input[name=txtProductId]').val('0');
        $('input[name=txtProductCode]').val('');
        $('.o-model>span').each(function () {
            if ($(this).data('group') == groupId) {
                $(this).removeClass('hide');
            } else { 
                $(this).addClass('hide');
            }
        });
        ActiveModel(groupId);

        $('.oprice').addClass('hide');

        $('.o-color>span').addClass('hide');
        $('.o-color>span.p' + $(this).data('productid')).removeClass('hide');
        $('input[name=txtProductCode]').val('');

        $('.o-img').attr('src', $(this).data('img'));

        $('.o-payment a.installment').attr('href', $(this).data('installment'));

        $('.o-model').removeClass('hide');
        $('.o-group>label').addClass('hide');
    });
    $('.o-model>span').on('click', function () {
        $(this).parent().removeClass('o-error');
        $('.o-model>span.active').removeClass('active');
        $('.o-color>span.active').removeClass('active');
        $(this).addClass('active');
        var productId = $(this).data('value');
        $('input[name=txtProductId]').val(productId);
        $('input[name=txtProductCode]').val('');

        //Reset khuyến mãi khi chọn sản phẩm mới
        $('input[name=txtPromotion]').val('0');
        $('.o-method>span').removeClass('hide');
        $('.o-promo>span').removeClass('active');

        //Cập nhật lại giá sản phẩm
        $('.price').html($(this).data('price'));
        $('#saleprice').html($(this).data('saleprice'));
        $('.hisprice').html($(this).data('hisprice'));
        $('.o-price').removeClass('hide');

        //Tự động active màu khi có 1 màu
        ActiveColor(productId);

        $('.o-promo').addClass('hide');
        $('.o-promo.promo' + productId).removeClass('hide');

        $('#link').attr('href', 'javascript:OpenPopupUserOrder(' + productId + ')');
        $('#totalOrder').html($(this).data('order'));
        $('#totalSMS').html($(this).data('sms'));

        $('.o-group>span').each(function () {
            if ($(this).hasClass('active')) { $('.oprice').removeClass('hide'); }
        });

        $('.o-img').attr('src', $(this).data('img'));

        $('.o-payment a.installment').attr('href', $(this).data('installment'));

        $('.o-model>label').addClass('hide');
    });
    $('.o-model>a').on('click', function () {
        $('.o-model>b').show();
        $(this).addClass('hide')
    });
    $('.o-color>span').on('click', function () {
        if ($(this).hasClass('active')) { return; }

        $(this).parent().removeClass('o-error');
        $('.o-color>span.active').removeClass('active');
        $(this).addClass('active');

        var method = $('input[name=txtMethod]').val();
        if ($(this).data('soldout') == "1") {
            $('.o-color>label.soldout').removeClass('hide');
            if (method == 1 || method == -1) {
                $('#btnSubmit').addClass('hide');
            } else {
                $('.order div.o-payment.step31').addClass('hide');
            }
            return;
        } else {
            if (method == 1 || method == -1) {
                $('#btnSubmit').removeClass('hide');
            } else {
                $('.order div.o-payment.step31').removeClass('hide');
            }
        }

        var val = $(this).data('value');
        $('input[name=txtProductCode]').val(val);

        //var provinceId = $('#ddlProvince option:selected').val();
        //var districtId = $('#ddlDistrict option:selected').val();
        //if (provinceId != undefined && districtId != undefined)
        //    ListStoreByDistrict();

        $('.o-img').attr('src', $(this).data('img'));

        $('.o-color>label').addClass('hide');
    });
    $('.o-gender>span').on('click', function () {
        $('.o-gender>span.active').removeClass('active');
        $(this).addClass('active');
        $('input[name=txtGender]').val($(this).data('value'));

        $('.o-gender>label').addClass('hide');
    });
    $('.o-promo>span').on('click', function () {
        $('.o-promo>span.active').removeClass('active');
        $(this).addClass('active');
        var val = $(this).data('value');
        $('input[name=txtPromotion]').val(val);

        //if (val == 350) {
        //    $('.o-payment div.fl').addClass('inline').removeClass('fl').find('a.button').html('TRẢ GÓP 0% QUA CÔNG TY TÀI CHÍNH<i>Cọc 1.000.000₫</i>');
        //    $('.o-payment div.fr').addClass('hide');
        //} else {
        //    $('.o-payment div.inline').addClass('fl').removeClass('inline').find('a.button').html('TRẢ GÓP QUA CÔNG TY TÀI CHÍNH<i>Cọc 1.000.000₫</i>');
        //    $('.o-payment div.fr').removeClass('hide');
        //}
    });
    $('.o-method>span').on('click', function () {
        if ($(this).hasClass('active')) { return; }
        $('.o-method>label').addClass('hide');
        $('.o-method>span.active').removeClass('active');
        $(this).addClass('active');
        var method = $(this).data('value');
        $('input[name=txtMethod]').val(method);
        LoadStores(3, -1, method, $(this));
        if (method == 1 || method == 6) {
            $('.o-store, .step30, .step32').removeClass('hide');
            $('.step31, .step33').addClass('hide');
            $('#btnSubmit').removeClass('hide').attr('href', 'javascript:QuickOrder(' + method + ')');
            $('#btnInstallment').removeClass('hide');
        } else if (method == 3) {
            $('.o-store, .step30, .step33').removeClass('hide');
            $('.step31, .step32').addClass('hide');
            $('#btnSubmit').removeClass('hide').attr('href', 'javascript:QuickOrder(3)');
            $('#btnInstallment').addClass('hide');
        } else {
            $('.step33').addClass('hide');
            $('.o-store, .step30, .step31, .step32').removeClass('hide');
            $('#btnSubmit').addClass('hide');
            $('#btnInstallment').addClass('hide');
        }

        $('input[name=txtStoreId]').val('-1');
        $('input[name=hdDistrictId]').val('-1');
        $('input[name=hdDistrictName]').val('');
    });
     
    $(document).on('click', '.o-store .province>span', function () {
        $('.district .select').hide();
        $(this).parent().find('div.select').slideToggle();
    });
    $(document).on('click', '.o-store .district>span', function () {
        $('.province .select').hide();
        $(this).parent().find('div.select').slideToggle();
    });
    $(document).on('keyup', '.o-store input[name=key]', function () {
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
    $(document).on('click', '.o-store .province .item>a', function () {
        var provinceId = $(this).data('value');
        var districtId = -1;
        var method = $('input[name=txtMethod]').val();
        $('input[name=hdProvinceId]').val(provinceId);
        $('input[name=hdProvinceName]').val($(this).html());
        $('input[name=hdDistrictId]').val(-1);
        $('input[name=hdDistrictName]').val('');
        LoadStores(provinceId, districtId, method);
    });
    $(document).on('click', '.o-store .district .item>a', function () {
        var provinceId = $('input[name=hdProvinceId]').val();
        var districtId = $(this).data('value');
        var method = $('input[name=txtMethod]').val();
        $('input[name=hdDistrictId]').val(districtId);
        $('input[name=hdDistrictName]').val($(this).html());
        if (method != 3) {
            LoadStores(provinceId, districtId, method);
        } else {
            $('.o-store .district>span').attr('data-value', districtId);
            $('.o-store .district>span').html($(this).html());
            $('.o-store .district .select').hide();
        }

        $('.o-store .district').removeClass('o-error');
        $('.o-store .district>label').addClass('hide');
    });
    $(document).on('click', '.o-store div.store>span', function () {
        $('.o-store div.store>span.active').removeClass('active');
        $(this).addClass('active');
        $('input[name=txtStoreId]').val($(this).data('value'));
        $('.o-store>label').addClass('hide');
    });
    $(document).on('keyup', '.o-gender>div>input', function () {
        var length = $(this).val().trim().length;
        if (length > 3) {
            $(this).parent().removeClass('o-error');
            $(this).parent().find('label').addClass('hide');
        } else {
            $(this).parent().addClass('o-error');
            $(this).parent().find('label').removeClass('hide');
        }
    });
    $(document).on('keyup', '.o-store>div>input', function () {
        var length = $(this).val().trim().length;
        if (length > 3) {
            $(this).parent().removeClass('o-error');
            $(this).parent().find('label').addClass('hide');
        } else {
            $(this).parent().addClass('o-error');
            $(this).parent().find('label').removeClass('hide');
        }
    });

    //Actice trả góp
    if (method == 6) {
        var flagCheck = false;
        $('.o-promo').each(function () {
            //Trường hợp chọn KM
            if ($(this).hasClass('choose')) {
                $(this).find('span').each(function () {
                    var val = $(this).data('value');
                    if (val == installment) { $(this).click(); flagCheck = true; }
                });
            } else {
                //Trường hợp khuyến mãi bình thường
                $('.o-method>span').each(function () {
                    var val = $(this).data('value');
                    if (val == 6) { $(this).click(); }
                });
            }
        });

        //Trường hợp khuyến mãi không có khuyến mãi
        if (!flagCheck) {
            $('.o-method>span').each(function () {
                var val = $(this).data('value');
                if (val == 6) { $(this).click(); }
            });
        }
    }

    //Auto check nếu sản phẩm có 1 màu
    var productId = $('input[name=txtProductId]').val();
    $('.o-model>span').each(function () {
        var productId = $(this).data('value');
        var $color = $('.o-color>span.p' + productId);
        if ($color.length == 1) { $color.addClass('active'); }
    });
    ActiveColor(productId);
    ActiveModel();

    //Auto check model
    if (productId > 0) {
        $('.o-model>span').each(function () {
            if ($(this).data('value') == productId) {
                $(this).trigger('click');
            }
        });
    }

    ScrollBar();
}
 
var flagOrder = false;
function Order(method, productId) {
    if (flagOrder) return;
    flagOrder = true;
    $('#dlding').show();

    $.ajax({
        type: "POST",
        cache: false,
        data: { productId: productId, method: method },
        url: rootUrl + '/Order/BoxOrder',
        success: function (e) {
            flagOrder = false;
            $('#dlding').fadeOut();
            if (e.status == -1) {
                alert(e.error);
                return;
            } else {
                $('#order').html(e);
                $('body,html').animate({ scrollTop: 0 }, 0);
                $('.page1, footer').addClass('hide');
                $('.page2').removeClass('hide');
                OrderEvent(method);
            }
        }
    });
}
function Back() {
    window.location.reload();
}
function ActiveColor(productId) {
    if (productId > 0) { $('.o-color').removeClass('hide'); }
    $('.o-color>span').addClass('hide');
    var $color = $('.o-color>span.p' + productId);
    $color.removeClass('hide');
    if ($color.length == 1) {
        $color.trigger('click');
        $('input[name=txtProductCode]').val($color.data('value'));
    }
}
function RemoveVietNamChar(str) {
    str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, "a");
    str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, "e");
    str = str.replace(/ì|í|ị|ỉ|ĩ/g, "i");
    str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, "o");
    str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, "u");
    str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, "y");
    str = str.replace(/đ/g, "d");

    str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, "A");
    str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, "E");
    str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, "I");
    str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, "O");
    str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, "U");
    str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, "Y");
    str = str.replace(/Đ/g, "D");

    return str;
}
function ActiveModel(groupId) {
    if ($('.o-group>span').length == 1) {
        $('.o-group').addClass('hide');
        $('.o-group>span').trigger('click');
    }
    var $model = $('.o-model>span[data-group=' + groupId + ']');
    if ($model.length == 1) {
        $model.trigger('click');
    }
}

var productga = {};
var flagQuickOrder = false;
function QuickOrder(method) {
    if (flagQuickOrder) return;
    flagQuickOrder = true;
    var data = GetAllFormData('#frmOrder');
    data = $.extend({}, data, { txtMethod: method });
    $('#dlding').show();

    $.ajax({
        type: "POST",
        cache: false,
        data: data,
        url: rootUrl + '/Order/QuickOrder',
        success: function (e) {
            flagQuickOrder = false;
            $('#dlding').fadeOut();
            $('.order label').addClass('hide');
            if (e.status == -99) {
                for (var i = 0; i < e.errors.length; i++) {
                    console.log(e.errors[i]);
                    $('.' + e.errors[i]).addClass('o-error');
                    $('.' + e.errors[i] + '>label').removeClass('hide');
                }
            } else if (e.status == -88) {
                window.location.href = e.error;
                return;
            } else if (e.status == -1) {
                alert(e.error);
                return;
            } else {
                $('.content-77').addClass('success');
                $('#frmOrder').replaceWith(e);
                $('body,html').animate({ scrollTop: 0 }, 500);
            }
        }
    });
}

var flagOpenPopupUserOrder = false;
function OpenPopupUserOrder(hdModel) {
    if (flagOpenPopupUserOrder) return false;
    flagOpenPopupUserOrder = true;

    $('#dlding').show();
    $.ajax({
        type: "POST",
        cache: false,
        data: { hdModel: hdModel },
        url: rootUrl + '/Order/PopupUserOrder',
        success: function (e) {
            $('#dlding').fadeOut();
            flagOpenPopupUserOrder = false;
            AddColorBox('#popup', '#lpopup', e);
            ScrollBar();
        }
    });
}
var flagSearchUserOrder = false;
function SearchUserOrder() {
    if (flagSearchUserOrder) return false;
    flagSearchUserOrder = true;

    $('#dlding').show();
    var data = GetAllFormData('#frmSearchUserOrder');
    data = $.extend({}, data, { page: 1 });
    $.ajax({
        type: "POST",
        cache: true,
        beforeSend: function () { },
        data: data,
        url: rootUrl + '/Order/PopupUserOrder',
        success: function (e) {
            $('#dlding').fadeOut();
            flagSearchUserOrder = false;
            $('#popup').replaceWith(e);
            $.colorbox.resize();
            ScrollBar();
            return false;
        }
    })
    return false;
}
var flagShowMoreUserOrder = false;
function ShowMoreUserOrder(page) {
    if (flagShowMoreUserOrder) return false;
    flagShowMoreUserOrder = true;

    var data = GetAllFormData($('#frmSearchUserOrder'));
    data = $.extend({}, data, { page: page });
    $.ajax({
        type: "POST",
        cache: false,
        beforeSend: function () { $('#dlding').show(); },
        data: data,
        url: rootUrl + '/Order/PopupUserOrder',
        success: function (e) {
            $('#dlding').fadeOut();
            flagShowMoreUserOrder = false;
            $('#popup table').append($(e).find('table tr').not('.trh'));
            if ($(e).find('a.showmore').length == 0) {
                $('#popup a.showmore').remove();
                $.colorbox.resize();
            } else {
                $('#popup a.showmore').replaceWith($(e).find('a.showmore'));
            }
            ScrollBar();
        }
    })
}

var flagOpenPopupUserSMS = false;
function OpenPopupUserSMS(hdProductId, cbSMS) {
    if (flagOpenPopupUserSMS) return;
    flagOpenPopupUserSMS = true;

    $('#dlding').show();
    $.ajax({
        type: "POST",
        cache: false,
        data: { hdProductId: hdProductId, cbSMS: cbSMS },
        url: rootUrl + '/Order/PopupUserSMS',
        success: function (e) {
            $('#dlding').fadeOut();
            flagOpenPopupUserSMS = false;
            AddColorBox('#popup', '#lpopup', e);
            ScrollBar();
        }
    });
}
var flagSearchUserSMS = false;
function SearchUserSMS() {
    if (flagSearchUserSMS) return false;
    flagSearchUserSMS = true;

    $('#dlding').show();
    var data = GetAllFormData('#frmSearchUserSMS');
    data = $.extend({}, data, { page: 1 });
    $.ajax({
        type: "POST",
        cache: true,
        beforeSend: function () { },
        data: data,
        url: rootUrl + '/Order/PopupUserSMS',
        success: function (e) {
            $('#dlding').fadeOut();
            flagSearchUserSMS = false;
            $('#popup').replaceWith(e);
            $.colorbox.resize();
            ScrollBar();
            return false;
        }
    })
    return false;
}
var flagShowMoreUserSMS = false;
function ShowMoreUserSMS(page) {
    if (flagShowMoreUserSMS) return false;
    flagShowMoreUserSMS = true;

    var data = GetAllFormData($('#frmSearchUserSMS'));
    data = $.extend({}, data, { page: page });
    $.ajax({
        type: "POST",
        cache: false,
        beforeSend: function () { $('#dlding').show(); },
        data: data,
        url: rootUrl + '/Order/PopupUserSMS',
        success: function (e) {
            $('#dlding').fadeOut();
            flagShowMoreUserSMS = false;
            $('#popup table').append($(e).find('table tr').not('.trh'));
            if ($(e).find('a.showmore').length == 0) {
                $('#popup a.showmore').remove();
                $.colorbox.resize();
            } else {
                $('#popup a.showmore').replaceWith($(e).find('a.showmore'));
            }
            ScrollBar();
        }
    })
}

var flagLoadStores = false;
function LoadStores(provinceId, districtId, method, $this)
{
    if (flagLoadStores) return;
    flagLoadStores = true;

    $('div.select').hide();
    $('.o-store .overlay').removeClass('hide');

    $.ajax({
        type: "POST",
        cache: true,
        beforeSend: function () { $('#dlding').show(); },
        data: { provinceId: provinceId, districtId: districtId, method: method },
        url: rootUrl + '/Order/BoxStore',
        success: function (e) {
            flagLoadStores = false;
            $('#dlding').fadeOut();
            if (e != null && e != '') {
                $('.o-method .o-line').remove();

                if ($this != undefined) {
                    $('.o-store').remove();
                    $this.after(e);
                } else {
                    $('.o-store .overlay').addClass('hide');
                    $('.o-store').replaceWith(e);
                    if ($('.store').find('div').html() == undefined) {
                        $('.store.scrollbar').perfectScrollbar();
                    } else {
                        $('.store.scrollbar').scrollTop(0).perfectScrollbar('update');
                    }
                }
                ScrollBar();

                $('input[name=hdProvinceId]').val($('.o-store .province>span').data('value'));
                $('input[name=hdProvinceName]').val($('.o-store .province>span').html());
            }
        }
    });
}