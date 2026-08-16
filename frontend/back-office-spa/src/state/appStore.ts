import { create } from 'zustand';
import type { Channel } from '../models/channel';
import type { UserInfo } from '../services/authService';

export interface FeatureFlags {
  videoBulkImportEnabled: boolean;
}

export type SessionStatus = 'unknown' | 'authenticated' | 'anonymous';

interface AppState {
  user: UserInfo | null;
  effectivePermissions: readonly string[];
  sessionStatus: SessionStatus;
  featureFlags: FeatureFlags;
  accessibleChannels: readonly Channel[];
  selectedChannelId: string | null;
  hydrated: boolean;
  hydrate: (state: AppHydration) => void;
  setAccessibleChannels: (channels: readonly Channel[]) => void;
  setSelectedChannelId: (channelId: string | null) => void;
  reset: () => void;
}

export interface AppHydration {
  user: UserInfo | null;
  effectivePermissions: readonly string[];
  featureFlags: FeatureFlags;
  accessibleChannels: readonly Channel[];
  selectedChannelId: string | null;
  sessionStatus: Exclude<SessionStatus, 'unknown'>;
}

const initialState = {
  user: null,
  effectivePermissions: [] as readonly string[],
  sessionStatus: 'unknown' as SessionStatus,
  featureFlags: { videoBulkImportEnabled: false },
  accessibleChannels: [] as readonly Channel[],
  selectedChannelId: null,
  hydrated: false,
};

export const useAppStore = create<AppState>((set) => ({
  ...initialState,
  hydrate: (state) => set({ ...state, hydrated: true }),
  setAccessibleChannels: (accessibleChannels) => set({ accessibleChannels }),
  setSelectedChannelId: (selectedChannelId) => set({ selectedChannelId }),
  reset: () => set(initialState),
}));

export const appStore = useAppStore;