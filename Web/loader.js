(function () {
    var baseUrl = "/CombinedWatching/Web/";
    var cacheBust = "?v=" + Date.now();

    var css = document.createElement("link");
    css.rel = "stylesheet";
    css.href = baseUrl + "plugin.css" + cacheBust;
    document.head.appendChild(css);

    var script = document.createElement("script");
    script.src = baseUrl + "plugin.js" + cacheBust;
    document.head.appendChild(script);
})();
