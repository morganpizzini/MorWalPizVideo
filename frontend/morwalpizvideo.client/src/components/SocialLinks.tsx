import { usePublicNavigation } from '../routes/layout/navigation';

export default function SocialLinks({ compact = false }: { compact?: boolean }) {
  const { navigation } = usePublicNavigation();
  const discord = navigation?.headerItems.find((item) => /^https:\/\/(discord\.gg\/|discord\.com\/invite\/)/i.test(item.targetUrl));
  const links = [
    { name: 'Telegram', icon: 'fa-telegram-plane', url: 'https://t.me/morwalpiz', className: 'telegram' },
    ...(discord ? [{ name: 'Discord', icon: 'fa-discord', url: discord.targetUrl, className: 'discord' }] : []),
    { name: 'Instagram', icon: 'fa-instagram', url: 'https://www.instagram.com/morwalpiz', className: 'instagram' },
    { name: 'YouTube', icon: 'fa-youtube', url: 'https://www.youtube.com/@morwalpiz?sub_confirmation=1', className: 'youtube' },
  ];
  return <div className={`social-links${compact ? ' social-links--compact' : ''}`}>{links.map((link) => <a key={link.name} href={link.url} className={`social-link social-link--${link.className}`} target="_blank" rel="noopener noreferrer" aria-label={link.name} title={link.name}><i className={`fab ${link.icon}`} aria-hidden="true" /></a>)}</div>;
}