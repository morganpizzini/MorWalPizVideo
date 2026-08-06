import { redirect } from 'react-router';
import { authService } from '../services/authService';

export const CAN_ACCESS_BACKOFFICE = 'canaccessbackoffice';

export async function requireBackOfficeAccess() {
  const session = await authService.validateSession();

  if (!session) {
    return redirect('/login');
  }

  if (!session.effectivePermissions.includes(CAN_ACCESS_BACKOFFICE)) {
    return redirect('/');
  }

  return null;
}