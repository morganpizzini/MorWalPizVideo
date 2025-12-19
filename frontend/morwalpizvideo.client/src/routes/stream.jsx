import { Helmet } from 'react-helmet-async';
import { useState } from 'react';
import { useLoaderData } from "react-router";
import configKeys from "@utils/configKeys"
function Stream() {
    const [chatVisible, setChatVisible] = useState(true);
    const { configuration } = useLoaderData();
    console.log(configuration)

    const toggleChat = () => {
        setChatVisible(prevState => !prevState);
    };

    return (
        <>
            <Helmet>
                <title>Stream - MorWalPiz</title>
                <meta name="description" content="Live streaming video" />
            </Helmet>

            <div className="navbar navbar-dark bg-dark mb-3">
                <div className="container-fluid">
                    <span className="navbar-brand">Live Streaming {!configuration[configKeys.STREAM_ENABLE] && <>- OFFLINE</>}</span>
                    <button
                        className="btn btn-outline-light ms-auto"
                        onClick={toggleChat}
                    >
                        {chatVisible ? 'Hide Chat' : 'Show Chat'}
                    </button>
                </div>
            </div>
            {!configuration[configKeys.STREAM_ENABLE] && <>
                <img className="w-100 mb-3" src={configuration[configKeys.STREAM_IMAGE_PLACEHOLDER]} />
            </>}
            {configuration[configKeys.STREAM_ENABLE] && <>
                <div className="row">
                    <div className={chatVisible ? "col-md-9 pe-0" : "col-12 pr-0"}>
                        <div className="ratio ratio-16x9">
                            <iframe
                                title="Main Player"
                                src={configuration[configKeys.STREAM_VIDEO_PATH]}
                                allow="autoplay; fullscreen"
                            ></iframe>
                        </div>
                    </div>
                    <div className={`col-md-3 ps-0 ${chatVisible ? '' : 'd-none'}`}>
                        <div className="ratio ratio-16x9 h-100">
                            <iframe
                                title="Chat"
                                src={configuration[configKeys.STREAM_CHAT_PATH]}
                                allow="autoplay"
                            ></iframe>
                        </div>
                    </div>
                </div>
            </>}
        </>
    );
}

export default Stream;
