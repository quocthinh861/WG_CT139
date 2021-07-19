// CALLING AJAX
function POSTAjax(url, dat, befHandle, sucHandle, errHandle, asy) {
    $.ajax({
        async: asy,
        url: url,
        data: dat,
        type: 'POST',
        cache: false,
        beforeSend: function () {
            befHandle();
        },
        success: function (e) {
            sucHandle(e);
        },
        error: function () {
            errHandle();
        }
    });
}

function SendError404(url) {
    var data = { strURL: url };
    POSTAjax("/aj/Common/SendMailError404/", data, function () { }, function (e) {
        if (e == 1) {
            alert("Bạn đã gửi email báo lỗi cho TGDD");
        } else {
            alert("Lỗi gửi email. Vui lòng thử lại sau.");
        }
    }, function (e) { }, true);
}
