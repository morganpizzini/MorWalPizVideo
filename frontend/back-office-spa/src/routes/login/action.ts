import { ActionFunction, redirect } from 'react-router';
import { authService } from '../../services/authService';

interface LoginError {
  message?: string;
  retryAfter?: number;
  remainingAttempts?: number;
}

export const action: ActionFunction = async ({ request }) => {
  const formData = await request.formData();
  const username = formData.get('username') as string;
  const password = formData.get('password') as string;

  if (!username || !password) {
    return { message: 'Username and password are required' };
  }

  try {
    await authService.login(username, password);
    // Redirect to dashboard after successful login
    return redirect('/');
  } catch (error) {
    console.error('Login error:', error);
    
    // Try to extract structured error data from the response
    if (error && typeof error === 'object') {
      const errorObj = error as any;
      
      // Check if error has response data with structured fields
      if (errorObj.response?.data) {
        const data = errorObj.response.data;
        return {
          message: data.message || data.title || 'Login failed. Please check your credentials.',
          retryAfter: data.retryAfter,
          remainingAttempts: data.remainingAttempts
        } as LoginError;
      }
      
      // Check if error object directly has these fields
      if (errorObj.message || errorObj.retryAfter || errorObj.remainingAttempts) {
        return {
          message: errorObj.message || 'Login failed. Please check your credentials.',
          retryAfter: errorObj.retryAfter,
          remainingAttempts: errorObj.remainingAttempts
        } as LoginError;
      }
    }
    
    // Fallback for simple error message
    return { 
      message: error instanceof Error ? error.message : 'Login failed. Please check your credentials.' 
    } as LoginError;
  }
};
