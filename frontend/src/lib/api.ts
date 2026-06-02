/**
 * API client for communicating with the backend service.
 * Handles request/response formatting and error handling.
 */

import type {
  SingleItemEnvelope,
  UserPreference,
  UpdateUserPreferenceRequest,
} from './types';

const BACKEND_SERVICE_URL = import.meta.env.VITE_API_BASE_URL || '';

/**
 * Custom error class for API failures with status code info.
 */
export class ApiRequestError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number,
    public readonly responseBody?: string
  ) {
    super(message);
    this.name = 'ApiRequestError';
  }
}

/**
 * Executes a typed HTTP request to the backend API.
 * @param resourcePath - API endpoint path starting with /
 * @param requestConfig - Optional fetch configuration
 * @returns Promise resolving to typed response data
 */
export async function executeApiRequest<TResponse>(
  resourcePath: string,
  requestConfig?: RequestInit
): Promise<TResponse> {
  const fullRequestUrl = `${BACKEND_SERVICE_URL}${resourcePath}`;

  const mergedHeaders: HeadersInit = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    ...requestConfig?.headers,
  };

  const httpResponse = await fetch(fullRequestUrl, {
    ...requestConfig,
    headers: mergedHeaders,
  });

  if (!httpResponse.ok) {
    const errorText = await httpResponse.text();
    throw new ApiRequestError(
      `Backend request failed: ${httpResponse.status} ${httpResponse.statusText}`,
      httpResponse.status,
      errorText
    );
  }

  const parsedResponseData = await httpResponse.json();
  return parsedResponseData as TResponse;
}

/**
 * Retrieves user preferences for the specified user.
 * @param userId - The user's unique identifier
 */
export function getUserPreferences(
  userId: number
): Promise<SingleItemEnvelope<UserPreference>> {
  return executeApiRequest<SingleItemEnvelope<UserPreference>>(
    `/v1/userpreferences/${userId}`
  );
}

/**
 * Updates user preferences for the specified user (upsert).
 * @param userId - The user's unique identifier
 * @param data - The preference values to save
 */
export function updateUserPreferences(
  userId: number,
  data: UpdateUserPreferenceRequest
): Promise<SingleItemEnvelope<UserPreference>> {
  return executeApiRequest<SingleItemEnvelope<UserPreference>>(
    `/v1/userpreferences/${userId}`,
    {
      method: 'PUT',
      body: JSON.stringify(data),
    }
  );
}
