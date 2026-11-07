import { socialIcon, usePublicSocials } from '../routes/layout/socials';

export default function SocialLinks({ compact = false }: { compact?: boolean }) {
  const links = usePublicSocials();
  return <div className={`social-links${compact ? ' social-links--compact' : ''}`}>{links.map((link) => <a key={link.provider} href={link.url} className={`social-link social-link--${link.provider}`} target="_blank" rel="noopener noreferrer" aria-label={link.provider} title={link.provider}><i className={`fab ${socialIcon(link.provider)}`} aria-hidden="true" /></a>)}</div>;
}