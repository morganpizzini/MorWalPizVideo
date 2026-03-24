import { get } from '@morwalpizvideo/services';
import { FontListResponse } from '@morwalpizvideo/models';

/**
 * Service for utility functions like fetching fonts
 */
export class UtilityService {
  /**
   * Get all available fonts from the backend
   */
  static async getFonts(): Promise<FontListResponse> {
    return get(`/api/Utility/fonts`);
  }
}