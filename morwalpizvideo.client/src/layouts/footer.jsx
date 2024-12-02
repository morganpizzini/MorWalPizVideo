import { Link } from "react-router-dom";
import "./footer.scss"
export default function TitleComponent() {

    return (
        <footer className="mt-auto">
            <div className="container py-3">
                <div className="row">
                    <div className="col-4 col-md-3">
                        <a href="/" className="d-flex align-items-center mb-3 link-body-emphasis text-decoration-none">
                            <img title="logo" alt="logo" src="/images/logo-150.png" style={{ "height": "75px", "width": "75px" }} />
                        </a>
                        <p className="mb-0">MorWalPiz</p>
                        <p>&copy; {(new Date().getFullYear())}</p>
                    </div>
                    <div className="col-4 offset-md-3 col-md-3">
                        <h5>Pagine</h5>
                        <ul className="nav flex-column">
                            <li className="nav-item mb-2"><Link to="/pages/chi-sono" className="nav-link p-0 link-light">Chi sono</Link></li>
                            <li className="nav-item mb-2"><Link to="/calendar" className="nav-link p-0 link-light">Calendario</Link></li>
                        </ul>
                    </div>
                    <div className="col-4 col-md-3">
                        <h5>Sezioni</h5>
                        <ul className="nav flex-column">
                            <li className="nav-item mb-2"><Link to="/" className="nav-link p-0 link-light">Home</Link></li>
                            <li className="nav-item mb-2"><Link to="/attrezzatura" className="nav-link p-0 link-light">Attrezzatura</Link></li>
                            <li className="nav-item mb-2"><Link to="/sponsors" className="nav-link p-0 link-light">Sponsors</Link></li>
                            <li className="nav-item mb-2"><Link to="/cookie-policy" className="nav-link p-0 link-light">Cookie policy</Link></li>
                        </ul>
                    </div>
                </div>
            </div>
        </footer>
    );
}