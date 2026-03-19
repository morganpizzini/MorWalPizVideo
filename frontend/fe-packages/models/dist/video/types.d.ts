export declare enum ContentType {
    SingleVideo = 0,
    Collection = 1
}
export interface CategoryRef {
    id: string;
    title: string;
}
export interface VideoRef {
    youtubeId: string;
    categories: CategoryRef[];
}
export interface Video {
    youtubeId: string;
    title: string;
    description?: string;
    thumbnail?: string;
    duration?: string;
    views?: number;
    likes?: number;
    comments?: number;
    publishedAt?: string;
    categories: CategoryRef[];
}
export interface Match {
    id: string;
    matchId: string;
    title: string;
    description?: string;
    url: string;
    thumbnailVideoId: string;
    videoRefs: VideoRef[];
    categories: CategoryRef[];
    contentType: ContentType;
    isLink: boolean;
    videos?: Video[];
    creationDateTime?: string;
}
export interface VideoImportRequest {
    videoId: string;
    categories: string[];
}
export interface SwapRootThumbnailRequest {
    currentVideoId: string;
    newVideoId: string;
}
export interface RootCreationRequest {
    videoId: string;
    title: string;
    description: string;
    url: string;
    categories: string[];
}
export interface SubVideoCrationRequest {
    matchId: string;
    videoId: string;
    categories: string[];
}
export interface VideoTranslateRequest {
    videoIds: string[];
}
export interface ReviewDetails {
    titleItalian: string;
    titleEnglish: string;
}
export type VideoCategory = string;
//# sourceMappingURL=types.d.ts.map