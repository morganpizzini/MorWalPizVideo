import { redirect, type ActionFunction, type LoaderFunction } from 'react-router';
import { authService, type AuthValidationResponse } from '../services/authService';
import { hasPermission, permissions } from '../authorization/permissions';

export const CAN_ACCESS_BACKOFFICE = permissions.backoffice.access;

export async function requirePermissions(
  requiredPermissions: readonly string[],
  validatedSession?: AuthValidationResponse | null
) {
  const session = validatedSession ?? await authService.validateSession();

  if (!session) {
    return redirect('/login');
  }

  if (!hasPermission(session.effectivePermissions, requiredPermissions)) {
    return redirect('/forbidden');
  }

  return null;
}

export function requireBackOfficeAccess() {
  return requirePermissions([CAN_ACCESS_BACKOFFICE]);
}

export function withPermission(
  requiredPermissions: readonly string[],
  loader?: LoaderFunction
): LoaderFunction {
  return async args => {
    const denial = await requirePermissions(requiredPermissions);
    return denial ?? (loader ? loader(args) : null);
  };
}

export function withActionPermission(
  requiredPermissions: readonly string[],
  action: ActionFunction
): ActionFunction {
  return async args => {
    const denial = await requirePermissions(requiredPermissions);
    return denial ?? action(args);
  };
}