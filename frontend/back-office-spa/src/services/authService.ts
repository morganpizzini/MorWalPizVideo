import { post, resetCsrfToken, setUnauthorizedHandler } from '@morwalpizvideo/services';

interface UserInfo {
  id: string;
  username: string;
  email: string;
  role: string;
}

interface LoginResponse {
  user: UserInfo;
}

class AuthService {
  private readonly USER_KEY = 'auth_user';

  constructor() {
    // Redirect to login on 401 responses (unless on auth endpoints)
    setUnauthorizedHandler(() => {
      localStorage.removeItem(this.USER_KEY);
      resetCsrfToken();
      window.location.href = '/login';
    });
  }

  // Store user info in localStorage (display only; the session itself lives in the HttpOnly auth cookie)
  setUser(user: UserInfo): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  // Get user info from localStorage
  getUser(): UserInfo | null {
    const userStr = localStorage.getItem(this.USER_KEY);

    return userStr && userStr !== 'undefined' ? JSON.parse(userStr) : null;
  }

  // Quick UI check based on locally cached user info; the auth cookie is the real authority
  isAuthenticated(): boolean {
    return !!this.getUser();
  }

  // Login method
  async login(username: string, password: string): Promise<LoginResponse> {
    const response = await post('/api/auth/login', { username, password });

    // Check if response contains errors (failed login)
    if (response.errors || !response.user) {
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

    // Store user info only on successful login; the auth cookie is set by the server
    resetCsrfToken();
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
      localStorage.removeItem(this.USER_KEY);
      resetCsrfToken();
    }
  }

  // Validate the session cookie with the backend
  async validateToken(): Promise<boolean> {
    try {
      const response = await post('/api/auth/validate', {});
      return response !== null && response !== undefined && !response.errors;
    } catch {
      return false;
    }
  }
}

export const authService = new AuthService();
export type { UserInfo, LoginResponse };