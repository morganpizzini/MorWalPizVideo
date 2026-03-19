export declare enum LinkType {
    YouTubeVideo = 0,
    YouTubeChannel = 1,
    YouTubePlaylist = 2,
    Instagram = 3,
    Facebook = 4,
    CustomUrl = 5
}
export interface ShortLink {
    shortLinkId: string;
    target: string;
    linkType: LinkType;
    queryLinkIds: string[];
    message: string;
    clicksCount: number;
    videoId: string;
}
export type CreateShortLinkDTO = Omit<ShortLink, 'shortLinkId' | 'clicksCount' | 'videoId'> & {
    shortLinkId?: string;
    clicksCount?: string;
};
export type UpdateShortLinkDTO = Partial<Omit<ShortLink, 'shortLinkId' | 'clicksCount' | 'message' | 'videoId'>> & {
    shortLinkId: string;
};
//# sourceMappingURL=shortLink.d.ts.map