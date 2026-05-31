import type { ReactNode } from 'react';

export interface EmptyStateProps {
    title: string;
    message?: ReactNode;
    className?: string;
}

export function EmptyState({ title, message, className }: EmptyStateProps) {
    return (
        <div className={`pb-empty ${className ?? ''}`} role="status" data-testid="pb-empty">
            <div className="pb-empty__title">{title}</div>
            {message ? <div className="pb-empty__message">{message}</div> : null}
        </div>
    );
}

export default EmptyState;
