import ReactGA from "react-ga4"
export default function Matches() {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: 'sponsor-video' })
    return (
        <>
            <div id="page-container" className="p-4 bg-white">
                <h1 className="page-title">Video di presentazione</h1>
                <hr />
                <p><b>IT</b>: Attiva l'audio nel video, se non ti &#232; possibile l'ascolto trovi il documento riassuntivo sotto </p>
                <p><b>EN</b>: Turn on video's audio, if you are unable to listen you can find the summary below</p>
                <iframe width="100%" height="450px" className="rounded" src="https://www.youtube.com/embed/N4ndkYb7uFI?autoplay=1&mute=1" title="YouTube video player" frameBorder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen></iframe>    
                <div className="page-text text-center">
                    <a target="_blank" rel="noopener noreferrer" className="btn btn-primary me-2 mt-2" href="https://morwalpizblob.blob.core.windows.net/page-images/sponsor-video/Morgan-Pizzini-CV.pdf" download>
                        Scarica il riassunto (IT)
                    </a>
                    <a target="_blank" rel="noopener noreferrer" className="btn btn-primary mt-2" href="https://morwalpizblob.blob.core.windows.net/page-images/sponsor-video/Morgan-Pizzini-CV-EN.pdf" download>
                        Download recap (EN)
                    </a>
                </div>
            </div>
        </>
    );
}