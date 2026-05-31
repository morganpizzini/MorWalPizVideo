import { useState } from 'react';

export interface PepperboxTopBarProps {
    title?: string;
}

const COMING_SOON_MS = 3000;

export function PepperboxTopBar({ title }: PepperboxTopBarProps) {
    const [notice, setNotice] = useState<string | null>(null);

    const show = (label: string) => {
        setNotice(`${label} coming soon`);
        window.setTimeout(() => setNotice(null), COMING_SOON_MS);
    };

    return (
        <div className="pepperbox-shell__topbar pb-topbar" data-testid="pb-topbar">
            {title ? <div style={{ fontWeight: 700 }}>{title}</div> : null}
            <div className="pb-topbar__spacer" />
            {notice ? (
                <span
                    className="pb-topbar__notice"
                    role="status"
                    data-testid="pb-topbar-notice"
                >{notice}</span>
            ) : null}
            <button
                type="button"
                className="pb-topbar__btn"
                onClick={() => show('Log in')}
                data-testid="pb-topbar-login"
            >Log in</button>
            <button
                type="button"
                className="pb-topbar__btn pb-topbar__btn--primary"
                onClick={() => show('Sign up')}
                data-testid="pb-topbar-signup"
            >Sign up</button>
        </div>
    );
}

export default PepperboxTopBar;
