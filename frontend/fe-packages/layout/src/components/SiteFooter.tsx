import { Link } from 'react-router';
import type { FooterConfig, BrandConfig } from '../types.js';

export interface SiteFooterProps {
  config: FooterConfig;
  brand: BrandConfig;
}

export function SiteFooter({ config, brand }: SiteFooterProps) {
  return (
    <footer className="site-footer mt-auto">
      <div className="container py-3">
        <div className="row">
          <div className="col-4 col-md-3">
            <Link to={brand.homeRoute || '/'} className="d-flex align-items-center mb-3 link-body-emphasis text-decoration-none">
              {brand.logo ? (
                <img 
                  title={brand.name} 
                  alt={brand.name} 
                  src={brand.logo} 
                  style={{ height: '75px', width: '75px' }} 
                />
              ) : (
                <span className="h5 mb-0">{brand.name}</span>
              )}
            </Link>
            <p className="mb-0">{brand.name}</p>
            {config.copyright && <p>{config.copyright}</p>}
          </div>

          {config.sections.map((section, index) => (
            <div 
              key={index} 
              className={`col-4 ${index === 0 ? 'offset-md-3' : ''} col-md-3`}
            >
              <h5>{section.title}</h5>
              <ul className="nav flex-column">
                {section.links.map((link, linkIndex) => (
                  <li key={linkIndex} className="nav-item mb-2">
                    {link.external ? (
                      <a
                        href={link.path}
                        className="nav-link p-0 link-light"
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        {link.label}
                      </a>
                    ) : (
                      <Link to={link.path} className="nav-link p-0 link-light">
                        {link.label}
                      </Link>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {config.legalLinks && config.legalLinks.length > 0 && (
          <div className="row mt-3">
            <div className="col-12">
              <ul className="nav justify-content-center border-top pt-3">
                {config.legalLinks.map((link, index) => (
                  <li key={index} className="nav-item">
                    <Link to={link.path} className="nav-link px-2 text-muted">
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        )}
      </div>
    </footer>
  );
}