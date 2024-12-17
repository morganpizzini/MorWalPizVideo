!(function (t) {
    var e = {};
    function s(i) {
        if (e[i]) return e[i].exports;
        var l = (e[i] = { i: i, l: !1, exports: {} });
        return t[i].call(l.exports, l, l.exports, s), (l.l = !0), l.exports;
    }
    (s.m = t),
        (s.c = e),
        (s.d = function (t, e, i) {
            s.o(t, e) || Object.defineProperty(t, e, { enumerable: !0, get: i });
        }),
        (s.r = function (t) {
            "undefined" != typeof Symbol && Symbol.toStringTag && Object.defineProperty(t, Symbol.toStringTag, { value: "Module" }), Object.defineProperty(t, "__esModule", { value: !0 });
        }),
        (s.t = function (t, e) {
            if ((1 & e && (t = s(t)), 8 & e)) return t;
            if (4 & e && "object" == typeof t && t && t.__esModule) return t;
            var i = Object.create(null);
            if ((s.r(i), Object.defineProperty(i, "default", { enumerable: !0, value: t }), 2 & e && "string" != typeof t))
                for (var l in t)
                    s.d(
                        i,
                        l,
                        function (e) {
                            return t[e];
                        }.bind(null, l)
                    );
            return i;
        }),
        (s.n = function (t) {
            var e =
                t && t.__esModule
                    ? function () {
                        return t.default;
                    }
                    : function () {
                        return t;
                    };
            return s.d(e, "a", e), e;
        }),
        (s.o = function (t, e) {
            return Object.prototype.hasOwnProperty.call(t, e);
        }),
        (s.p = ""),
        s((s.s = 0));
})([
    function (t, e) {
        let s = document.querySelector('script[data-name="BMC-Widget"]'),
            i = window.innerHeight - 120;
        window.addEventListener("DOMContentLoaded", function () {
            new FontFace("Avenir Book", "url(https://cdn.buymeacoffee.com/bmc_widget/font/710789a0-1557-48a1-8cec-03d52d663d74.eot)")
                .load()
                .then(function (t) {
                    document.fonts.add(t);
                })
                .catch(function (t) { });
            let t = window.matchMedia("(min-width: 480px)"),
                e = document.createElement("div");
            (e.id = "bmc-wbtn"),
                (e.style.display = "flex"),
                (e.style.alignItems = "center"),
                (e.style.justifyContent = "center"),
                (e.style.width = "64px"),
                (e.style.height = "64px"),
                (e.style.background = s.dataset.color),
                (e.style.color = "white"),
                (e.style.borderRadius = "32px"),
                (e.style.position = "fixed"),
                "left" == s.dataset.position ? (e.style.left = s.dataset.x_margin + "px") : (e.style.right = s.dataset.x_margin + "px"),
                (e.style.bottom = s.dataset.y_margin + "px"),
                (e.style.boxShadow = "0 4px 8px rgba(0,0,0,.15)"),
                (e.innerHTML = '<img src="/images/logo-150.png" alt="Buy Me A Coffee" style="height: 36px; width: 36px; margin: 0; padding: 0;">'),
                (e.style.zIndex = "9999"),
                (e.style.cursor = "pointer"),
                (e.style.fontWeight = "600"),
                (e.style.transition = ".25s ease all");
            let l = document.createElement("div");
            (l.style.position = "fixed"), (l.style.top = "0"), (l.style.left = "0"), (l.style.width = "0"), (l.style.height = "0"), (l.style.background = "rgba(0, 0, 0, 0)"), (l.style.textAlign = "center"), (l.style.zIndex = "9999999");
            let n = document.createElement("div");
            l.appendChild(n),
                n.setAttribute("id", "bmc-close-btn"),
                (n.style.position = "fixed"),
                (n.style.alignItems = "center"),
                (n.style.justifyContent = "center"),
                (n.style.display = "flex"),
                (n.style.visibility = "hidden"),
                (n.style.borderRadius = "100px"),
                (n.style.width = "40px"),
                (n.style.height = "40px"),
                (n.innerHTML =
                    '<svg style="width: 16px;height:16px;" width="16" height="16" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">\n  <path d="M2.45156 27.6516L0.351562 25.5516L11.9016 14.0016L0.351562 2.45156L2.45156 0.351562L14.0016 11.9016L25.5516 0.351562L27.6516 2.45156L16.1016 14.0016L27.6516 25.5516L25.5516 27.6516L14.0016 16.1016L2.45156 27.6516Z" fill="#666"/>\n  </svg>\n  '),
                (n.style.top = "16px"),
                (n.style.right = "16px"),
                (n.style.zIndex = "9999999"),
                (n.onclick = function () { });
            let a = document.createElement("iframe");
            a.setAttribute("id", "bmc-iframe"),
                a.setAttribute("allow", "payment"),
                (a.title = "Buy Me a Coffee"),
                (a.style.position = "fixed"),
                (a.style.margin = "0"),
                (a.style.border = "0"),
                t.matches
                    ? ("left" == s.dataset.position ? (a.style.left = s.dataset.x_margin + "px") : (a.style.right = s.dataset.x_margin + "px"), (a.style.bottom = parseInt(s.dataset.y_margin, 10) + 72 + "px"))
                    : ((a.style.left = "0px"), (a.style.right = "0px"), (a.style.top = "0px"), (a.style.bottom = "32px")),
                (a.style.height = "0"),
                (a.style.opacity = "0"),
                t.matches
                    ? ((a.style.width = "calc(100% - 38px)"), (a.style.width = "420px"), (a.style.maxWidth = "420px"), (a.style.minHeight = `${i}px`), (a.style.maxHeight = `${i}px`))
                    : ((a.style.width = "calc(100% - 38px)"), (a.style.width = "100vw"), (a.style.maxWidth = "100vw"), (a.style.minHeight = "100%"), (a.style.maxHeight = "100%")),
                (a.style.borderRadius = "10px"),
                (a.style.boxShadow = "-6px 0px 30px rgba(13, 12, 34, 0.1)"),
                (a.style.background = "#fff"),
                (a.style.backgroundImage = "url(https://cdn.buymeacoffee.com/assets/img/widget/loader.svg)"),
                (a.style.backgroundPosition = "center"),
                (a.style.backgroundSize = "64px"),
                (a.style.backgroundRepeat = "no-repeat"),
                (a.style.zIndex = "999999"),
                (a.style.transition = "all .25s ease"),
                (a.style.transformOrigin = "right bottom"),
                (a.style.transform = "scale(0)"),
                (a.style.userSelect = "none");
            let o = document.createElement("div");
            (o.style.position = "fixed"),
                "left" == s.dataset.position ? (o.style.left = parseInt(s.dataset.x_margin, 10) + 84 + "px") : (o.style.right = parseInt(s.dataset.x_margin, 10) + 84 + "px"),
                (o.style.bottom = parseInt(s.dataset.y_margin, 10) - 2 + "px"),
                (o.style.transformOrigin = "right bottom"),
                (o.style.transform = "scale(0.7)"),
                (o.style.opacity = "0"),
                (o.style.transition = ".25s ease all"),
                (o.style.visibility = "hidden"),
                (o.style.background = "#ffffff"),
                (o.style.zIndex = "9999"),
                (o.innerText = s.dataset.message),
                (o.style.boxShadow = "0px 2px 5px rgba(0, 0, 0, 0.05), 0px 8px 40px rgba(0, 0, 0, 0.04), 0px 0px 2px rgba(0, 0, 0, 0.15)"),
                (o.style.padding = "16px 16px"),
                (o.style.borderRadius = "4px"),
                (o.style.fontSize = "18px"),
                (o.style.color = "#0D0C22"),
                (o.style.width = "auto"),
                (o.style.maxWidth = "260px"),
                (o.style.lineHeight = "1.5"),
                (o.style.fontFamily = '"Avenir Book", sans-serif'),
                setTimeout(function () {
                    s.dataset.message && "" != s.dataset.message && ((o.style.opacity = "1"), (o.style.visibility = "visible"), (o.style.transform = "scale(1)"), (o.style.transition = ".25s ease all"));
                }, 500),
                setTimeout(function () {
                    (o.style.transformOrigin = "right bottom"), (o.style.transform = "scale(0.7)"), (o.style.opacity = "0"), (o.style.transition = ".25s ease all"), (o.style.visibility = "hidden");
                }, 5e3),
                document.body.appendChild(l),
                l.appendChild(a),
                document.body.appendChild(e),
                document.body.appendChild(o);
            let r = 0;
            (e.onclick = function () {
                r || (a.src = "https://www.buymeacoffee.com/widget/page/" + s.dataset.id + "?description=" + encodeURIComponent(s.dataset.description) + "&color=" + encodeURIComponent(s.dataset.color)),
                    r++,
                    (o.style.transform = "scale(0.7)"),
                    (o.style.opacity = "0"),
                    (o.style.transition = ".25s ease all"),
                    (o.style.visibility = "hidden"),
                    (l.style.width = "100%"),
                    (l.style.height = "100%"),
                    (a.style.width = "420px"),
                    (a.style.height = `${window.innerHeight - 120}px`),
                    (a.style.transform = "scale(1)"),
                    (a.style.opacity = "1"),
                    t.matches ||
                    setTimeout(function () {
                        n.style.visibility = "visible";
                    }, 150),
                    (e.style.transform = "scale(1)"),
                    (e.style.transition = ".25s ease all"),
                    (e.innerHTML =
                        '<svg style="width: 16px;height:16px;" width="16" height="10" viewBox="0 0 16 10" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M14.1133 0L8 6.11331L1.88669 0L0 1.88663L8 9.88663L16 1.88663L14.1133 0Z" fill="white"/></svg>');
            }),
                (e.onmouseover = function () {
                    (e.style.transform = "scale(1.1)"), (e.style.transition = ".25s ease all");
                }),
                (e.onmouseleave = function () {
                    (o.style.transform = "scale(0.7)"), (o.style.opacity = "0"), (o.style.transition = ".25s ease all"), (o.style.visibility = "hidden"), (e.style.transform = "scale(1)"), (e.style.transition = ".25s ease all");
                }),
                (e.onmousedown = function () {
                    (e.style.transform = "scale(0.90)"), (e.style.transition = ".25s ease all");
                }),
                (l.onmouseover = function () {
                    (l.style.cursor = "pointer"), (e.style.transform = "scale(1)"), (e.style.transition = ".25s ease all");
                }),
                (l.onmouseleave = function () {
                    (e.style.transform = "scale(1)"), (e.style.transition = ".25s ease all");
                }),
                (l.onclick = function () {
                    (l.style.width = "0"),
                        (l.style.height = "0"),
                        (a.style.height = "0"),
                        (a.style.opacity = "0"),
                        (e.style.transform = "scale(1)"),
                        (e.style.transition = ".25s ease all"),
                        (a.style.transform = "scale(0)"),
                        t.matches || ((n.style.visibility = "hidden"), setTimeout(function () { }, 100)),
                        (e.innerHTML = '<img src="/images/logo-150.png" alt="Buy Me A Coffee" style="height: 36px; width: 36px; margin: 0; padding: 0;">');
                }),
                (l.onmousedown = function () {
                    (e.style.transform = "scale(0.90)"), (e.style.transition = ".25s ease all");
                });
            var y = document.cookie;
            y.split(";");
            var d = new Date();
            d.setTime(d.getTime() + 864e5);
            var c = "; expires=" + d.toGMTString();
            y.includes("visited") || "" == s.dataset.message
                ? ((o.style.transform = "scale(0.7)"), (o.style.opacity = "0"), (o.style.transition = ".25s ease all"), (o.style.visibility = "hidden"))
                : ((document.cookie = `visited=1${c};path=/`),
                    setTimeout(() => {
                        (o.style.transform = "scale(0.7)"), (o.style.opacity = "0"), (o.style.transition = ".25s ease all"), (o.style.visibility = "hidden");
                    }, 5e3));
        });
    },
]);
