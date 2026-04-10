import { Link } from 'react-router';
import type { HeaderConfig, BrandConfig } from '../types.js';

export interface SiteHeaderProps {
  config: HeaderConfig;
  brand: BrandConfig;
}

export function SiteHeader({ config, brand }: SiteHeaderProps) {
  const getSocialIcon = (platform: string) => {
    const icons: Record<string, string> = {
      telegram: 'fab fa-telegram',
      youtube: 'fab fa-youtube',
      instagram: 'fab fa-instagram',
      facebook: 'fab fa-facebook',
      twitter: 'fab fa-twitter',
    };
    return icons[platform] || 'fas fa-link';
  };

  const getSocialButtonClass = (platform: string) => {
    return `btn btn-${platform}`;
  };

  return (
    <header className="site-header">
      <div className={`container text-center ${config.dimensions || ''}`}>
        {config.showBrand !== false && (
          <div className="title-container">
            <Link to={brand.homeRoute || '/'}>
              {brand.logo ? (
                <img 
                  src={brand.logo} 
                  alt={brand.name} 
                  className="brand-logo"
                />
              ) : (
                <h1 className="title">
                  <span className="big-letter">M</span>
                  <span className="small-letter">or</span>
                  <span className="big-letter">W</span>
                  <span className="small-letter">al</span>
                  <span className="big-letter">P</span>
                  <span className="small-letter">iz</span>
                </h1>
              )}
            </Link>
            {brand.tagline && <p className="tagline">{brand.tagline}</p>}
          </div>
        )}

        {!config.hideLinks && config.socialLinks && config.socialLinks.length > 0 && (
          <div className="social-buttons mt-3">
            {config.socialLinks.map((social, index) => (
              <a
                key={index}
                href={social.url}
                target="_blank"
                rel="noopener noreferrer"
                className={getSocialButtonClass(social.platform)}
              >
                <i className={getSocialIcon(social.platform)}></i> {social.label}
              </a>
            ))}
          </div>
        )}

        {config.navigation && config.navigation.length > 0 && (
          <nav className="main-navigation mt-3">
            <ul className="nav justify-content-center">
              {config.navigation.map((item, index) => (
                <li key={index} className="nav-item">
                  {item.external ? (
                    <a
                      href={item.path}
                      className="nav-link"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {item.icon && <i className={item.icon}></i>} {item.label}
                    </a>
                  ) : (
                    <Link to={item.path} className="nav-link">
                      {item.icon && <i className={item.icon}></i>} {item.label}
                    </Link>
                  )}
                </li>
              ))}
            </ul>
          </nav>
        )}
      </div>
    </header>
  );
}