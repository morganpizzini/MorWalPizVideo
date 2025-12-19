import axios from 'axios';

// IMPORTANT: Replace with your actual backend API base URL
const API_BASE_URL = 'http://localhost:5000/api'; // Example URL

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// --- Interfaces for Request Data (match backend DTOs) ---

interface RequestVideoData {
  name: string;
  email: string;
  recordingDate: string;
  recordingTime: string;
  shooterDescription: string;
  recaptchaToken: string; // Include the token
}

interface RequestAdData {
  companyName: string;
  contactPerson: string;
  email: string;
  companyDescription: string;
  recaptchaToken: string; // Include the token
}

// --- API Service Functions ---

/**
 * Submits a video request to the backend.
 * @param data - The video request form data including the Recaptcha token.
 */
const requestVideo = async (data: RequestVideoData): Promise<any> => {
  try {
    // IMPORTANT: Replace '/video-requests' with your actual backend endpoint
    const response = await apiClient.post('/video-requests', data);
    return response.data;
  } catch (error) {
    console.error('Error submitting video request:', error);
    // Re-throw the error so the component can handle it
    throw error;
  }
};

/**
 * Submits an ad insertion request to the backend.
 * @param data - The ad request form data including the Recaptcha token.
 */
const requestAd = async (data: RequestAdData): Promise<any> => {
  try {
    // IMPORTANT: Replace '/ad-requests' with your actual backend endpoint
    const response = await apiClient.post('/ad-requests', data);
    return response.data;
  } catch (error) {
    console.error('Error submitting ad request:', error);
    // Re-throw the error so the component can handle it
    throw error;
  }
};

// --- Export the service functions ---

const apiService = {
  requestVideo,
  requestAd,
  // Add other API functions here as needed
};

export default apiService;

