import { ActionFunction, redirect } from 'react-router';
import { authService } from '../../services/authService';

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
    return { 
      message: error instanceof Error ? error.message : 'Login failed. Please check your credentials.' 
    };
  }
};
