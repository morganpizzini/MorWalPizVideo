import { VideoImportRequest, SwapRootThumbnailRequest, RootCreationRequest, SubVideoCrationRequest, ReviewDetails } from './types';
export declare const VideoService: {
    translateShort: (videoIds: string[]) => Promise<void>;
    importVideo: (request: VideoImportRequest) => Promise<void>;
    convertToRoot: (request: RootCreationRequest) => Promise<void>;
    swapThumbnailUrl: (request: SwapRootThumbnailRequest) => Promise<void>;
    createRoot: (request: RootCreationRequest) => Promise<void>;
    createSubVideo: (request: SubVideoCrationRequest) => Promise<void>;
    getReviewDetails: (reviewText: string) => Promise<ReviewDetails>;
};
//# sourceMappingURL=service.d.ts.map