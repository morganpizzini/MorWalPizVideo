import { AccountEmailNotice, accountMailItalian } from '@morwalpiz/layout';
import { getPublicAccountMailErrorMessage, requestPublicEmailConfirmation } from '@morwalpizvideo/services';
import { usePublicAccount } from '../layout/account';

export default function ConfirmationNotice() {
  const { session, refresh } = usePublicAccount();
  if (!session.authenticated || !session.account || session.account.emailConfirmed) return null;
  const email = session.account.email;
  const t = (message: string) => accountMailItalian[message] ?? message;
  return <AccountEmailNotice t={t} initiallyRequested={session.confirmationEmailStatus === 'requested'}
    errorMessage={(error) => t(getPublicAccountMailErrorMessage(error))}
    onRequest={() => requestPublicEmailConfirmation({ email, site: 'public-site' })}
    onRefresh={async () => { if (!(await refresh())) throw new Error('Unable to verify the session.'); }} />;
}
