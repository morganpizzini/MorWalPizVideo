export const NavigationItemType = {
  Page: 0,
  Internal: 1,
  External: 2,
} as const;

export type NavigationItemType = typeof NavigationItemType[keyof typeof NavigationItemType];

export interface NavigationMenuItem {
  type: NavigationItemType;
  pageId?: string;
  targetUrl: string;
  displayText: string;
  column: number;
  displayOrder: number;
  openInNewTab?: boolean;
}

export interface ChannelNavigation {
  id?: string;
  channelId?: string;
  isActive: boolean;
  headerItems: NavigationMenuItem[];
  footerColumnCount: number;
  footerItems: NavigationMenuItem[];
}

export interface PublicNavigation {
  headerItems: NavigationMenuItem[];
  footerColumnCount: number;
  footerItems: NavigationMenuItem[];
}

export interface NavigationMenuItemDTO {
  type: NavigationItemType;
  pageId?: string;
  targetUrl: string;
  displayText: string;
  column: number;
}

export type SaveNavigationDTO = Omit<ChannelNavigation, 'id' | 'channelId'> & {
  headerItems: NavigationMenuItemDTO[];
  footerItems: NavigationMenuItemDTO[];
};