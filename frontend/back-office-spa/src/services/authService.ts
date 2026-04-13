import { post, setAuthTokenProvider } from '@morwalpizvideo/services';

interface UserInfo {
  id: string;
  username: string;
  email: string;
  role: string;
}

interface LoginResponse {
  token: string;
  user: UserInfo;
}

class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';

  constructor() {
    // Register this service as the auth token provider for the shared services package
    setAuthTokenProvider(() => this.getToken());
  }

  // Store token in localStorage
  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  // Get token from localStorage
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  // Store user info in localStorage
  setUser(user: UserInfo): void {
    console.log('Storing user info:', user);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  // Get user info from localStorage
  getUser(): UserInfo | null {
    const userStr = localStorage.getItem(this.USER_KEY);

    return userStr && userStr !== 'undefined' ? JSON.parse(userStr) : null;
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  // Login method
  async login(username: string, password: string): Promise<LoginResponse> {
    const response = await post('/api/auth/login', { username, password });

    // Check if response contains errors (failed login)
    if (response.errors || !response.token || !response.user) {
      // Extract error details for better user feedback
      const errorData: any = {};
      
      if (response.message) errorData.message = response.message;
      if (response.retryAfter) errorData.retryAfter = response.retryAfter;
      if (response.remainingAttempts !== undefined) errorData.remainingAttempts = response.remainingAttempts;
      
      // If we have structured error data, throw it
      if (Object.keys(errorData).length > 0) {
        throw errorData;
      }
      
      // Otherwise throw a generic error
      throw new Error(response.errors?.[0] || 'Login failed. Please check your credentials.');
    }

    // Store token and user info only on successful login
    this.setToken(response.token);
    this.setUser(response.user);

    return response;
  }

  // Logout method
  async logout(): Promise<void> {
    try {
      // Call backend to clear cookie
      await post('/api/auth/logout', {});
    } catch (error) {
      // Continue with local cleanup even if API call fails
      console.error('Logout API call failed:', error);
    } finally {
      // Always clear local storage
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
    }
  }

  // Validate token with backend
  async validateToken(): Promise<boolean> {
    const token = this.getToken();
    if (!token) return false;

    try {
      await post('/api/auth/validate', { token });
      return true;
    } catch {
      return false;
    }
  }
}

export const authService = new AuthService();
export type { UserInfo, LoginResponse };