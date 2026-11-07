import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { frontendEndpoints, publicApiService } from '@morwalpizvideo/services';
import type { PublicChannelSocialLink } from '@morwalpizvideo/models';

const SocialsContext = createContext<readonly PublicChannelSocialLink[]>([]);
const icons: Record<string, string> = {
  telegram: 'fa-telegram-plane', discord: 'fa-discord', instagram: 'fa-instagram',
  youtube: 'fa-youtube', facebook: 'fa-facebook', x: 'fa-x-twitter',
  reddit: 'fa-reddit', patreon: 'fa-patreon',
};
export function PublicSocialsProvider({ children }: { children: ReactNode }) {
  const [links, setLinks] = useState<readonly PublicChannelSocialLink[]>([]);
  useEffect(() => {
    publicApiService.get(frontendEndpoints.CHANNEL_SOCIALS)
      .then((data) => setLinks((data as { links?: PublicChannelSocialLink[] }).links ?? []))
      .catch(() => setLinks([]));
  }, []);
  return <SocialsContext.Provider value={links}>{children}</SocialsContext.Provider>;
}
export function usePublicSocials() { return useContext(SocialsContext); }
export function socialIcon(provider: string) { return icons[provider] ?? 'fa-link'; }
