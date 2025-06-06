import { API_CONFIG } from '@config/api';
import { FontListResponse } from '@models/font';

/**
 * Service for utility functions like fetching fonts
 */
export class UtilityService {
  /**
   * Get all available fonts from the backend
   */
  static async getFonts(): Promise<FontListResponse> {
    const response = await fetch(`/api/Utility/fonts`);
    
    if (!response.ok) {
      throw new Error(`Failed to fetch fonts: ${response.statusText}`);
    }
    
    return response.json();
  }
}
