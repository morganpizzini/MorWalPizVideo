import { Link } from "react-router-dom";
import "./title-header.scss"
export default function TitleComponent() {
    
    return (
        <div className="container text-center">
            <div className="title-container">
                <Link to={``}>
                    <h1 className="title">
                        <span className="big-letter">M</span><span className="small-letter">or</span><span className="big-letter">W</span><span className="small-letter">al</span><span className="big-letter">P</span><span className="small-letter">iz</span>
                    </h1>
                </Link>
            </div>
            <div className="social-buttons mt-3">
                <a href="https://www.instagram.com/morwalpiz" target="_blank" rel="noopener noreferrer" className="btn btn-instagram">
                    <i className="fab fa-instagram"></i> Seguimi
                </a>
                <a href="https://www.youtube.com/@morwalpiz?sub_confirmation=1" target="_blank" rel="noopener noreferrer" className="btn btn-youtube">
                    <i className="fab fa-youtube"></i> Iscriviti
                </a>
            </div>
        </div>
    );
}