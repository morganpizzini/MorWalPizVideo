// Main barrel export file for @morwalpizvideo/models

// Calendar Event exports
export type {
  CalendarEvent,
  CreateCalendarEventRequest,
  UpdateCalendarEventRequest,
} from './CalendarEvent';

// Categories exports
export type { Category, CreateCategoryDTO, UpdateCategoryDTO } from './categories';

// Channel exports
export type { Channel, ChannelSocial, ChannelVideo, CreateChannelDTO, UpdateChannelDTO } from './channel';
export type { ChannelNews, ChannelNewsAdmin, ChannelNewsImage, ChannelNewsStatus } from './channelNews';

// Configuration exports
export type {
  MorWalPizConfiguration,
  CreateConfigurationDTO,
  UpdateConfigurationDTO,
} from './configuration';

// Custom Form exports
export {
  QuestionType,
  AnswerType,
} from './CustomForm';

export type {
  QuestionOption,
  CustomFormQuestion,
  OpenQuestion,
  MultipleChoiceQuestion,
  SingleChoiceQuestion,
  AnyQuestion,
  CustomFormAnswer,
  OpenAnswer,
  MultipleChoiceAnswer,
  SingleChoiceAnswer,
  AnyAnswer,
  CustomFormResponse,
  CustomForm,
  CreateCustomFormRequest,
  UpdateCustomFormRequest,
  SubmitFormResponseRequest,
} from './CustomForm';

// Font exports
export type { FontCategoryResponse, FontListResponse } from './font';

// Product exports
export type { Product, CreateProductDTO, UpdateProductDTO } from './product';

// Product Category exports (for video content products)
export type { 
  ProductCategory,
  ProductCategory as VideoProductCategory,
  CreateProductCategoryDTO, 
  UpdateProductCategoryDTO 
} from './productCategory';

// Query Link exports
export type { QueryLink, CreateQueryLinkDTO, UpdateQueryLinkDTO } from './queryLink';

// Short Link exports
export type { ShortLink, CreateShortLinkDTO, UpdateShortLinkDTO } from './shortLink';
export { LinkType } from './shortLink';

// QuickLinks exports
export { QuickLinkKind } from './quickLinks';
export type { QuickLink, QuickLinks, CreateQuickLinksDTO, UpdateQuickLinksDTO } from './quickLinks';

// Sponsor exports
export type { Sponsor, CreateSponsorDTO, UpdateSponsorDTO } from './sponsor';

// YouTube Video Link exports
export type {
  YouTubeVideoLink,
  YouTubeVideoLinkResponse,
} from './youTubeVideoLink';

// Video exports
export {
  ContentType,
} from './video/types';

export type {
  CategoryRef,
  VideoRef,
  Video,
  Match,
  Compilation,
  VideoImportRequest,
  VideoTranslateRequest,
  ReviewDetails,
  VideoCategory,
} from './video/types';

// Shop - Digital Product exports
export type {
  DigitalProduct,
  ProductCategory as DigitalProductCategory,
  CreateDigitalProductRequest,
  UpdateDigitalProductRequest,
} from './digitalProduct';

// Shop - Customer exports
export type {
  Customer,
  EmailLoginRequest,
  EmailVerificationRequest,
  LoginResponse,
} from './customer';

// Shop - Cart exports
export type {
  Cart,
  CartItem,
  AddToCartRequest,
  UpdateCartItemRequest,
  CheckoutRequest,
  CheckoutResponse,
} from './cart';

// Shop - Legal exports
export type {
  LegalContent,
  LegalContentType,
  CreateLegalContentRequest,
  UpdateLegalContentRequest,
} from './legal';

// Insights exports
export {
  InsightNewsStatus,
  ContentPlanType,
  InsightSourceKind,
  InsightCommentSourceType,
  InsightTopicCreationMode,
  InsightCommentAnalysisRunStatus,
} from './insights';

export type {
  InsightTopic,
  InsightNewsItem,
  InsightContentPlan,
  CreateInsightTopicRequest,
  UpdateInsightTopicRequest,
  ReviewNewsItemRequest,
  GenerateContentPlanRequest,
  UpdateContentPlanRequest,
  AnalyzeInsightCommentsRequest,
  AnalyzeInsightCommentsResponse,
} from './insights';
