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
export type { Channel, CreateChannelDTO, UpdateChannelDTO } from './channel';

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
  ProductCategory as VideoProductCategory, 
  CreateProductCategoryDTO, 
  UpdateProductCategoryDTO 
} from './productCategory';

// Query Link exports
export type { QueryLink, CreateQueryLinkDTO, UpdateQueryLinkDTO } from './queryLink';

// Short Link exports
export type { ShortLink, CreateShortLinkDTO, UpdateShortLinkDTO } from './shortLink';
export { LinkType } from './shortLink';

// Sponsor exports
export type { Sponsor, CreateSponsorDTO, UpdateSponsorDTO } from './sponsor';

// YouTube Video Link exports
export type {
  YouTubeVideoLink,
  CreateYouTubeVideoLinkRequest,
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
  SwapRootThumbnailRequest,
  RootCreationRequest,
  SubVideoCrationRequest,
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
} from './insights';
