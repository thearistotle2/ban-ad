const banad = {
    get: {
        baseUrl: function baseUrl () {
            const url = document.getElementsByTagName('body')[0].getAttribute('data-banad-url');
            return url.replace(/\/$/, '');
        },
        adElements: function adElements() {
            return document.querySelectorAll('[data-banad-id]');
        }
    },
    load: {
        ads: function ads() {
            banad.get.adElements().forEach(function setUpAd(ad) {
                // clear
                ad.innerHTML = '';
                
                ad.onmouseover = banad.ads.mouseover;
                ad.addEventListener('touchstart', banad.ads.mouseover);
                
                ad.onmouseout  = banad.ads.mouseout;
                window.addEventListener('touchstart', banad.ads.touchend);

                banad.load.css(ad, {
                    position: 'relative',
                });
                
                const base = banad.get.baseUrl();
                const id = ad.getAttribute('data-banad-id');
                
                const link = document.createElement('a');
                link.setAttribute('href', base + '/banad/out/' + id);
                const image = document.createElement('img');
                image.setAttribute('src', base + '/banad/ad/' + id);
                link.append(image);
                ad.append(link);

                const notice = document.createElement('div');
                notice.setAttribute('data-banad-notice-for', id);
                notice.innerHTML = '<a href="' + base + '/banad/advertise/' + id + '">advertise here</a>';
                banad.load.css(notice, {
                    position: 'absolute',
                    right: '0px',
                    bottom: '-0.25em',
                    'font-size': '0.8em',
                    '-webkit-user-select': 'none',
                    '-ms-user-select': 'none',
                    'user-select': 'none'
                });
                ad.append(notice);
            });
        },
        css: function css(element, style) {
            for (const property in style) {
                element.style[property] = style[property];
            }
        }
    },
    ads: {
        openNotice: function openNotice(ad) {
            const id = ad.getAttribute('data-banad-id');

            const notice = ad.querySelector('[data-banad-notice-for="' + id + '"]');
            banad.load.css(notice, {
                bottom: '-1.5em',
                'border-bottom': '1px solid black',
            });
        },
        closeNotice: function closeNotice(ad) {
            const id = ad.getAttribute('data-banad-id');

            const notice = ad.querySelector('[data-banad-notice-for="' + id + '"]');
            banad.load.css(notice, {
                bottom: '-0.25em',
                'border-bottom': null,
            });
        },
        
        mouseover: function mouseover() {
            banad.ads.openNotice(this);
        },
        mouseout: function mouseout() {
            banad.ads.closeNotice(this);
        },
        
        touchend: function touchend(target) {
            // Close any ads that aren't currently touched.
            banad.get.adElements().forEach(function closeNotice(ad) {
                const touched = target.path.includes(ad);
                if (!touched) {
                    banad.ads.closeNotice(ad);
                }
            });
        }
    }
};

banad.load.ads();
