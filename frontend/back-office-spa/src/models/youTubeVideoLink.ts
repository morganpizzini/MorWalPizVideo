export interface YouTubeVideoLink {
  contentCreatorName: string;
  youTubeVideoId: string;
  imageName: string;
  shortLinkCode?: string;
  shortLinkTarget?: string;
}

export interface CreateYouTubeVideoLinkRequest {
  matchId: string;
  contentCreatorName: string;
  youTubeVideoId: string;
  fontStyle: string;
  fontSize: number;
  textColor: string;
  outlineColor: string;
  outlineThickness: number;
}

export interface YouTubeVideoLinkResponse {
  contentCreatorName: string;
  youTubeVideoId: string;
  imageName: string;
  shortLinkCode?: string;
  shortLinkTarget?: string;
}
