/*! 20-08-2014
* https://developers.google.com/analytics/devguides/collection/analyticsjs/enhanced-ecommerce
* https://developers.google.com/analytics/devguides/collection/analyticsjs/ecommerce
* Võ Nhật Nam */

//Multiple Tracker Support
var domainTracking = 'DesktopVersion.';
//Adding one or more products to a shopping cart.
function ga_openOrder(id, name, category, brand, variant, price, quantity) {
    ga_addProduct(id, name, category, brand, variant, price, quantity);
    ga_sendAction('add');
    ga('send', 'event', 'UX', 'click', 'open order form');
    if (window.location.href.toString().indexOf('_utm_recommend=1') != -1) {
        ga('send', 'event', 'UX', 'click', 'Recommend - Open Order Form');
        $('#wrap_cart .closepopup').click(function () {
            ga('send', 'event', 'UX', 'click', 'Recommend - Close Order Form');
        })
    }
}

//The sale of one or more products.
function ga_completeOrder(id, name, category, brand, variant, price, quantity) {
    ga('set', '&cu', 'VND');
    ga_addProduct(id, name, category, brand, variant, price, quantity);
    ga_sendAction('purchase', {
        'id': new Date(),
        'affiliation': 'Online store',
        'revenue': price,
        'tax': '0',
        'shipping': '0',
        'coupon': ''
    });
    ga('send', 'event', 'UX', 'click', 'complete order');
    if (window.location.href.toString().indexOf('_utm_recommend=1') != -1)
        ga('send', 'event', 'UX', 'click', 'Recommend - Order Form Complete');
    if (price > 0) {
        if (typeof dataLayer != 'undefined') {
            dataLayer.push({ 'event': 'addCartComplete' });
        }
    }
}

// A view of product details.
function ga_viewProduct(id, name, category, brand, variant, price, quantity) {
    ga_addProduct(id, name, category, brand, variant, price, quantity);
    ga_sendAction('detail');
    ga('send', 'pageview');
}

function ga_addProduct(id, name, category, brand, variant, price, quantity) {
    ga('ec:addProduct', {
        'id': id,
        'name': name,
        'category': category,
        'brand': brand,
        'variant': variant,
        'price': price,
        'quantity': quantity
    });
}

function ga_sendAction(action, data) {
    if (data != undefined) {
        ga('ec:setAction', action, data);
    }
    else {
        ga('ec:setAction', action);
    }
}

// Tracking Mua hàng tại nhà - tại siêu thị - trả góp
function ga_conversion(type) {
    if (typeof dataLayer != 'undefined') {
        switch (type) {
            case 1:
                dataLayer.push({ 'event': 'addCartCompleteAtHome' });
                break;
            case 2:
                dataLayer.push({ 'event': 'addCartCompleteAtStore' });
                break;
            case 3:
                dataLayer.push({ 'event': 'addCartCompleteAtInstallment' });
                break;
        }
    }
}
