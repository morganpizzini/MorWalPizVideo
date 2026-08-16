import { createContext, useContext, useEffect, type PropsWithChildren } from 'react';
import { useRevalidator } from 'react-router';
import {
  getSelectedChannelId,
  setSelectedChannelId as persistSelectedChannelId,
} from '@morwalpizvideo/services';
import type { Channel } from '../models/channel';
import { useAppStore } from '../state/appStore';

interface ChannelContextValue {
  channels: readonly Channel[];
  selectedChannelId: string | null;
  selectChannel: (channelId: string) => void;
}

const channelContext = createContext<ChannelContextValue>({
  channels: [],
  selectedChannelId: null,
  selectChannel: () => undefined,
});

interface ChannelProviderProps extends PropsWithChildren {
  channels: readonly Channel[];
}

export function ChannelProvider({ channels, children }: ChannelProviderProps) {
  const revalidator = useRevalidator();
  const selectedChannelId = useAppStore(state => state.selectedChannelId);
  const setSelectedChannelId = useAppStore(state => state.setSelectedChannelId);
  const setAccessibleChannels = useAppStore(state => state.setAccessibleChannels);
  const storeChannels = useAppStore(state => state.accessibleChannels);
  const availableChannels = storeChannels.length ? storeChannels : channels;

  useEffect(() => {
    const hasCurrentSelection = selectedChannelId !== null &&
      channels.some(channel => channel.channelId === selectedChannelId);
    const persistedChannelId = getSelectedChannelId();
    const hasPersistedSelection = persistedChannelId !== null &&
      channels.some(channel => channel.channelId === persistedChannelId);
    const nextSelectedChannelId = hasCurrentSelection
      ? selectedChannelId
      : hasPersistedSelection
        ? persistedChannelId
        : channels[0]?.channelId ?? null;

    setAccessibleChannels(channels);
    persistSelectedChannelId(nextSelectedChannelId);
    setSelectedChannelId(nextSelectedChannelId);
  }, [channels, setAccessibleChannels, setSelectedChannelId]);

  const selectChannel = (channelId: string) => {
    if (!availableChannels.some(channel => channel.channelId === channelId)) {
      return;
    }

    persistSelectedChannelId(channelId);
    setSelectedChannelId(channelId);
    revalidator.revalidate();
  };

  return (
    <channelContext.Provider value={{ channels: availableChannels, selectedChannelId, selectChannel }}>
      {children}
    </channelContext.Provider>
  );
}

export function useChannelContext(): ChannelContextValue {
  return useContext(channelContext);
}