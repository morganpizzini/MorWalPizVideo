import { post, setAuthTokenProvider } from './apiService';

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
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  // Get user info from localStorage
  getUser(): UserInfo | null {
    const userStr = localStorage.getItem(this.USER_KEY);
    return userStr ? JSON.parse(userStr) : null;
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  // Login method
  async login(username: string, password: string): Promise<LoginResponse> {
    const response = await post('/api/auth/login', { username, password });

    console.log(response);

    // Store token and user info
    this.setToken(response.token);
    this.setUser(response.user);

    return response;
  }

  // Logout method
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  // Validate token with backend
  async validateToken(): Promise<boolean> {
    const token = this.getToken();
    if (!token) return false;

    try {
      const response = await post('/api/auth/validate', { token });
      console.log("validate token:", response);
      return true;
    } catch {
      return false;
    }
  }
}

export const authService = new AuthService();
export type { UserInfo, LoginResponse };