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

// Product Category exports
export type {
  ProductCategory,
  CreateProductCategoryDTO,
  UpdateProductCategoryDTO,
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
  VideoImportRequest,
  SwapRootThumbnailRequest,
  RootCreationRequest,
  SubVideoCrationRequest,
  VideoTranslateRequest,
  ReviewDetails,
  VideoCategory,
} from './video/types';
