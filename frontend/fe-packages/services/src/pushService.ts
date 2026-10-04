import type {
  PushAudience,
  PushAudienceRequest,
  PushChannelSendRequest,
  PushDispatch,
  PushPlatformSendRequest,
  PushPublicKey,
  PushSubscribeRequest,
  PushSubscriptionState,
  PushTargets,
  PushNotificationTemplate,
  PushNotificationTemplateRequest,
} from "@morwalpizvideo/models";
import { adminApiService, publicApiService } from "./apiTransport";
import endpoints, { ComposeUrl } from "./endpoints";
import frontendEndpoints from "./endpoints-frontend";

// --- Public, anonymous surface -------------------------------------------------

export function getPushPublicKey(): Promise<PushPublicKey> {
  return publicApiService.get(
    frontendEndpoints.PUSH_PUBLIC_KEY,
  ) as Promise<PushPublicKey>;
}

export function savePushSubscription(
  request: PushSubscribeRequest,
): Promise<PushSubscriptionState> {
  return publicApiService.post(
    frontendEndpoints.PUSH_SUBSCRIPTIONS,
    request,
  ) as Promise<PushSubscriptionState>;
}

export function getPushSubscriptionSettings(
  endpoint: string,
  credential: string,
): Promise<PushSubscriptionState> {
  return publicApiService.post(frontendEndpoints.PUSH_SUBSCRIPTION_SETTINGS, {
    endpoint,
    credential,
  }) as Promise<PushSubscriptionState>;
}

export function revokePushSubscription(
  endpoint: string,
  credential: string,
): Promise<void> {
  return publicApiService.post(frontendEndpoints.PUSH_SUBSCRIPTION_REVOKE, {
    endpoint,
    credential,
  }) as Promise<void>;
}

// --- BackOffice surface --------------------------------------------------------

export function fetchPushAudiences(): Promise<PushAudience[]> {
  return adminApiService.get(endpoints.PUSH_AUDIENCES) as Promise<
    PushAudience[]
  >;
}

export function createPushAudience(
  request: PushAudienceRequest,
): Promise<PushAudience> {
  return adminApiService.post(
    endpoints.PUSH_AUDIENCES,
    request,
  ) as Promise<PushAudience>;
}

export function updatePushAudience(
  id: string,
  request: PushAudienceRequest,
): Promise<PushAudience> {
  return adminApiService.put(
    ComposeUrl(endpoints.PUSH_AUDIENCE_DETAIL, { id }),
    request,
  ) as Promise<PushAudience>;
}

export function deletePushAudience(id: string): Promise<void> {
  return adminApiService.Delete(
    ComposeUrl(endpoints.PUSH_AUDIENCE_DETAIL, { id }),
  ) as Promise<void>;
}

export function getPushTargets(): Promise<PushTargets> {
  return adminApiService.get(
    endpoints.PUSH_CAMPAIGN_TARGETS,
  ) as Promise<PushTargets>;
}

export function fetchPushPlatformDispatches(): Promise<PushDispatch[]> {
  return adminApiService.get(endpoints.PUSH_CAMPAIGNS_PLATFORM) as Promise<
    PushDispatch[]
  >;
}

export function fetchPushChannelDispatches(): Promise<PushDispatch[]> {
  return adminApiService.get(endpoints.PUSH_CAMPAIGNS_CHANNEL) as Promise<
    PushDispatch[]
  >;
}

export function sendPushPlatform(
  request: PushPlatformSendRequest,
): Promise<PushDispatch> {
  return adminApiService.post(
    endpoints.PUSH_CAMPAIGNS_PLATFORM,
    request,
  ) as Promise<PushDispatch>;
}

export function sendPushChannel(
  request: PushChannelSendRequest,
): Promise<PushDispatch> {
  return adminApiService.post(
    endpoints.PUSH_CAMPAIGNS_CHANNEL,
    request,
  ) as Promise<PushDispatch>;
}

export function fetchPushNotificationTemplates(): Promise<PushNotificationTemplate[]> {
  return adminApiService.get(endpoints.PUSH_TEMPLATES) as Promise<PushNotificationTemplate[]>;
}
export function createPushNotificationTemplate(request: PushNotificationTemplateRequest): Promise<PushNotificationTemplate> {
  return adminApiService.post(endpoints.PUSH_TEMPLATES, request) as Promise<PushNotificationTemplate>;
}
export function updatePushNotificationTemplate(id: string, request: PushNotificationTemplateRequest): Promise<PushNotificationTemplate> {
  return adminApiService.put(ComposeUrl(endpoints.PUSH_TEMPLATE_DETAIL, { id }), request) as Promise<PushNotificationTemplate>;
}
export function deletePushNotificationTemplate(id: string): Promise<void> {
  return adminApiService.Delete(ComposeUrl(endpoints.PUSH_TEMPLATE_DETAIL, { id })) as Promise<void>;
}

export function saveBackOfficePushSubscription(request: PushSubscribeRequest): Promise<PushSubscriptionState> {
  return adminApiService.post(endpoints.PUSH_BACKOFFICE_SUBSCRIBE, request) as Promise<PushSubscriptionState>;
}

export function getBackOfficePushSubscriptionSettings(endpoint: string, credential: string): Promise<PushSubscriptionState> {
  return adminApiService.post(endpoints.PUSH_BACKOFFICE_STATUS, { endpoint, credential }) as Promise<PushSubscriptionState>;
}

export function revokeBackOfficePushSubscription(endpoint: string, credential: string): Promise<void> {
  return adminApiService.post(endpoints.PUSH_BACKOFFICE_REVOKE, { endpoint, credential }) as Promise<void>;
}
