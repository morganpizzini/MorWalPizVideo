import ReactGA from "react-ga4";
import './links.scss'
export default function Matches() {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: 'link-video' });

    return (
        <>
            <div id="page-container" className="p-4 bg-white">
                <div className="linktree-container">
                    <div className="linktree-links">
                        <a href="/sl/last" className="linktree-link" target="_blank" rel="noopener noreferrer">📺 Ultimo video YT 📺</a>
                        <a href="https://buymeacoffee.com/morwalpiz" className="linktree-link" target="_blank" rel="noopener noreferrer">☕ Offri un caffè ☕</a>
                        <a href="https://www.youtube.com/@morwalpiz/shorts" className="linktree-link" target="_blank" rel="noopener noreferrer">🏃‍♂️‍➡️ Contenuti shorts 🏃‍♂️‍➡️</a>
                        <a href="https://www.instagram.com/morwalpiz" className="linktree-link" target="_blank" rel="noopener noreferrer">😄 Instagram 😄</a>
                    </div>
                </div>
            </div>
        </>
    );
}