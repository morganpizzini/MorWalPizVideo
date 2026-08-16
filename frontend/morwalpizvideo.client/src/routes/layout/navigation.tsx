import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import type { PublicNavigation } from '@morwalpizvideo/models';
import { fetchPublicNavigation } from '../../services/navigation';

export interface PublicNavigationState {
    navigation: PublicNavigation | null;
    loading: boolean;
    error: boolean;
}

const defaultState: PublicNavigationState = { navigation: null, loading: true, error: false };
const PublicNavigationContext = createContext<PublicNavigationState>(defaultState);

export function PublicNavigationProvider({ children }: { children: ReactNode }) {
    const [state, setState] = useState<PublicNavigationState>(defaultState);

    useEffect(() => {
        let cancelled = false;
        fetchPublicNavigation()
            .then(navigation => { if (!cancelled) setState({ navigation, loading: false, error: false }); })
            .catch(() => { if (!cancelled) setState({ navigation: null, loading: false, error: true }); });
        return () => { cancelled = true; };
    }, []);

    return <PublicNavigationContext.Provider value={state}>{children}</PublicNavigationContext.Provider>;
}

export function usePublicNavigation(): PublicNavigationState {
    return useContext(PublicNavigationContext);
}