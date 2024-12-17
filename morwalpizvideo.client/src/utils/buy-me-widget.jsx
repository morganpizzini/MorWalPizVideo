import { useEffect } from "react";

export default function BuyMeWidget() {
    const containerId = "supportByBMCWidget";
    useEffect(() => {
        const element = document.getElementById(containerId);
        if (element.hasChildNodes()) {
            return;
        }
        const script = document.createElement("script");
        const div = document.getElementById(containerId);
        script.setAttribute(
            "src",
            "/js/buy-me-coffee-widget.js"
        );
        script.setAttribute("data-name", "BMC-Widget");
        script.setAttribute("data-cfasync", "false");
        script.setAttribute("data-id", "MorWalPiz");
        script.setAttribute("data-description", "Aiutami con un caricatore di colpi!");
        script.setAttribute(
            "data-message",
            ""
            //"Grazie per essere qui! Regalami un caricatore di colpi per i prossimi video!"
        );
        script.setAttribute("data-color", "#6E6E6E");
        script.setAttribute("data-position", "Right");
        script.setAttribute("data-x_margin", "18");
        script.setAttribute("data-y_margin", "18");
        script.async = true
        script.onload = function () {
            var evt = document.createEvent("Event");
            evt.initEvent("DOMContentLoaded", false, false);
            window.dispatchEvent(evt);
        };

        div.appendChild(script);
    }, []);

    return <div id={containerId}></div>;
};