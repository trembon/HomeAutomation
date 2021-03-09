$(function () {
    // clicks on cameras
    $('.camera-block a').click(function (e) {
        e.preventDefault();
        showImageInOverlay(this.href, true);
    });

    // click on lights
    $('.light-block a').click(function (e) {
        e.preventDefault();
        var $t = $(this);

        var url = '/api/powerswitch/turnon?id=';
        if ($t.data('isOn') === 1) {
            url = '/api/powerswitch/turnoff?id=';
        }

        $.getJSON(url + $t.data('id'), function (result) {
            $t.data('isOn', result.isOn ? 1 : 0);
            if (result.isOn) {
                $t.find('.indicator').removeClass('fas').addClass('far');
            } else {
                $t.find('.indicator').removeClass('far').addClass('fas');
            }
        });
    });

    // create updating clock
    updateClock();
    setInterval(updateClock, 1000);

    // bind telldus command dropdown
    $('a[data-telldus-method]').click(function (e) {
        var deviceId = $(this).closest('tr').data('deviceId');
        var command = $(this).data('telldusMethod');
        $.getJSON('/api/telldus/send?deviceId=' + deviceId + '&command=' + command, function (data) {
            alert('Command result: ' + data);
        });
        e.preventDefault();
    });
});

function showImageInOverlay(imageUrl, reload) {
    var $overlay = $('<div class="overlay"><span class="fa fa-times"></span><div class="img" style="background-image: url(' + imageUrl + "?_=" + new Date().getTime() + ')"></div></div>');

    $overlay.css('top', ($('nav.navbar').outerHeight() + $(document).scrollTop()) + 'px');
    $overlay.css('height', 'calc(100% - ' + $('nav.navbar').outerHeight() + 'px');

    $('body').css('overflow', 'hidden');
    $overlay.appendTo('body');

    var preload = null;
    if (reload) {
        preload = new Image();
        preload.onload = function () {
            $overlay.find('.img').css('background-image', 'url("' + preload.src + '")');
            preload.src = imageUrl + "?_=" + new Date().getTime();
        }
        preload.src = imageUrl + "?_=" + new Date().getTime();
    }

    $overlay.find('.fa').click(function (e) {
        e.preventDefault();
        $overlay.remove();
        $('body').css('overflow', 'visible');

        if (preload !== null) {
            preload.onload = null;
        }
    });
}

function updateClock() {
    var date = new Date();
    var year = date.getFullYear();
    var month = padZero(date.getMonth() + 1);
    var day = padZero(date.getDate());
    var hour = padZero(date.getHours());
    var minute = padZero(date.getMinutes());
    $('#clock').text(year + '-' + month + '-' + day + ' ' + hour + ':' + minute);
}

function padZero(number) {
    if (number > 0 && number < 10)
        return '0' + number;

    return number;
}