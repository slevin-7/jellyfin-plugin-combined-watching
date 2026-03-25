(function () {
    "use strict";

    function log(msg) {
        console.log("[CombinedWatching] " + msg);
    }

    function waitForAuth(cb, n) {
        n = n || 0;
        var api = window.ApiClient;
        if (api && api.accessToken && api.accessToken()) {
            cb(api);
        } else if (n < 60) {
            setTimeout(function () { waitForAuth(cb, n + 1); }, 500);
        }
    }

    function renameSections(title) {
        var done = false;
        var rename = function () {
            var els = document.querySelectorAll("[class*='sectionTitleContainer'], [class*='sectionTitle']");
            els.forEach(function (el) {
                var t = (el.textContent || "").trim();
                if (t.match(/weiterschauen|continue\s*watching|fortsetzen|resume/i)) {
                    var h = el.querySelector("h2, h3, button, .sectionTitle");
                    if (h && h.textContent.trim() !== title) {
                        h.textContent = title;
                        done = true;
                        log("Renamed section -> " + title);
                    }
                }
            });
        };

        var obs = new MutationObserver(function () {
            if (!done) rename();
        });
        obs.observe(document.body, { childList: true, subtree: true });
        setTimeout(rename, 2000);
    }

    waitForAuth(function (api) {
        var base = api.serverAddress() || window.location.origin;
        fetch(base + "/CombinedWatching/Config", {
            headers: { "Authorization": 'MediaBrowser Token="' + api.accessToken() + '"' }
        })
        .then(function (r) { return r.json(); })
        .then(function (cfg) {
            if (!cfg.enabled) {
                log("Disabled in config");
                return;
            }
            log("Active (limit=" + cfg.maxItems + ", title=" + cfg.sectionTitle + ")");
            renameSections(cfg.sectionTitle);
        })
        .catch(function () { log("Config fetch failed, using defaults"); });
    });
})();
