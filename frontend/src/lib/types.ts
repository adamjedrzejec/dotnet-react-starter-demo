/**
 * TypeScript interfaces matching backend envelope pattern DTOs.
 * Organization API responses follow a consistent structure.
 */

/** Response metadata with correlation and timing info */
export interface ResponseMetadata {
  timestamp: string;
  transactionId: string;
  totalCount?: number;
}

/** HATEOAS navigation links */
export interface NavigationLinks {
  self: string;
  next?: string;
  prev?: string;
}

/** Generic envelope for single-item API responses */
export interface SingleItemEnvelope<TPayload> {
  item: TPayload;
  metadata: ResponseMetadata;
  links: NavigationLinks;
}

/** Generic envelope for collection API responses */
export interface CollectionEnvelope<TPayload> {
  items: TPayload[];
  metadata: ResponseMetadata;
  links: NavigationLinks;
}

/** Health check response payload */
export interface SystemHealthInfo {
  status: string;
  version: string;
  checkedAtDate: string;
  environment: string;
}

/** Error response from API failures */
export interface ApiErrorResponse {
  code: string;
  message: string;
  details?: Array<{
    field?: string;
    message: string;
  }>;
}

/** User display and notification preferences */
export interface UserPreference {
  userPreferenceId: number;
  userId: number;
  themePreference: string;
  emailNotificationIndicator: boolean;
  pushNotificationIndicator: boolean;
  createdDate: string;
  updatedDate: string;
}

/** Request body for updating user preferences */
export interface UpdateUserPreferenceRequest {
  themePreference: string;
  emailNotificationIndicator: boolean;
  pushNotificationIndicator: boolean;
}
