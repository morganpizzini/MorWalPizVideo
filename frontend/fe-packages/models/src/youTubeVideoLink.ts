export interface YouTubeVideoLink {
  contentCreatorName: string;
  // Legacy fallback when short link fields are not available.
  youTubeVideoId?: string;
  imageName: string;
  shortLinkUrl?: string;
  shortLinkCode?: string;
  shortLinkTarget?: string;
  directVideoUrl?: string;
}

export interface YouTubeVideoLinkResponse {
  contentCreatorName: string;
  // Legacy fallback when short link fields are not available.
  youTubeVideoId?: string;
  imageName: string;
  shortLinkUrl?: string;
  shortLinkCode?: string;
  shortLinkTarget?: string;
  directVideoUrl?: string;
}
