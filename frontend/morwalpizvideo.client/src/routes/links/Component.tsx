import { trackPageView } from '../../analytics/consent';
import './style.scss';
export default function Matches() {
  if (typeof window !== 'undefined') {
    trackPageView('link-video', '/links');
  }

  return (
    <main className="links-page">
      <div id="page-container" className="page-content">
        <div className="linktree-container">
          <h1 className="visually-hidden">Link utili</h1>
          <div className="linktree-links">
            <a href="/sl/last" className="linktree-link" target="_blank" rel="noopener noreferrer">
              📺 Ultimo video YT 📺
            </a>
            <a
              href="https://buymeacoffee.com/morwalpiz"
              className="linktree-link"
              target="_blank"
              rel="noopener noreferrer"
            >
              ☕ Offri un caffè ☕
            </a>
            <a
              href="https://www.youtube.com/@morwalpiz/shorts"
              className="linktree-link"
              target="_blank"
              rel="noopener noreferrer"
            >
              🏃‍♂️‍➡️ Contenuti shorts 🏃‍♂️‍➡️
            </a>
            <a
              href="https://www.instagram.com/morwalpiz"
              className="linktree-link"
              target="_blank"
              rel="noopener noreferrer"
            >
              😄 Instagram 😄
            </a>
          </div>
        </div>
      </div>
    </main>
  );
}
