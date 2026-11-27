import { useState } from 'react';
import { useLocation } from 'react-router';
import { AccountMailPage, accountMailItalian } from '@morwalpiz/layout';
import { confirmPublicEmail, requestPublicEmailConfirmation, requestPublicPasswordReset, resetPublicPassword, getPublicAccountMailErrorMessage } from '@morwalpizvideo/services';
import { getAccountMailLink, clearAccountMailLink } from '@morwalpizvideo/services/account-mail-link';
import './style.scss';

export default function AccountMail() {
  const location = useLocation();
  const [link] = useState(getAccountMailLink);
  const mode = location.pathname === '/account/forgot-password' ? 'request-reset' : location.pathname === '/account/reset-password'
    ? 'reset' : location.pathname === '/account/check-email' ? 'check-email' : 'confirm';
  const t = (message: string) => accountMailItalian[message] ?? message;
  return <AccountMailPage key={mode} mode={mode} t={t} link={link} initialEmail={typeof location.state?.email === 'string' ? location.state.email : ''}
    initiallyRequested={location.state?.confirmationEmailStatus === 'requested'}
    requestPath={mode === 'confirm' ? '/account/check-email' : '/account/forgot-password'}
    errorMessage={(error) => t(getPublicAccountMailErrorMessage(error))}
    onRequest={(email) => mode === 'request-reset'
      ? requestPublicPasswordReset({ email, site: 'public-site' })
      : requestPublicEmailConfirmation({ email, site: 'public-site' })}
    onConsume={async (token, password) => {
      if (mode === 'reset') await resetPublicPassword({ ...token, password });
      else await confirmPublicEmail(token);
      clearAccountMailLink();
    }} />;
}
