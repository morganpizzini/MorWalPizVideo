import { useEffect } from "react";

interface AccountLegalRedirectProps {
  path: string;
}

export default function AccountLegalRedirect({
  path,
}: AccountLegalRedirectProps) {
  useEffect(() => {
    window.location.replace(`https://morwalpiz.com${path}`);
  }, [path]);

  return (
    <main className="account-page">
      <p role="status">Opening the MorWalPiz legal page…</p>
    </main>
  );
}
