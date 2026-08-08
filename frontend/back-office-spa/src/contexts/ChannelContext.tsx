import { createContext, useContext, useEffect, useState, type PropsWithChildren } from 'react';
import { useRevalidator } from 'react-router';
import {
  getSelectedChannelId,
  selectFirstAccessibleChannel,
  setSelectedChannelId,
} from '@morwalpizvideo/services';
import type { Channel } from '../models/channel';

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
  const [selectedChannelId, setSelectedChannelState] = useState(() =>
    selectFirstAccessibleChannel(channels) ?? getSelectedChannelId());

  useEffect(() => {
    const nextSelectedChannelId = selectFirstAccessibleChannel(channels);
    setSelectedChannelState(nextSelectedChannelId);
  }, [channels]);

  const selectChannel = (channelId: string) => {
    if (!channels.some(channel => channel.channelId === channelId)) {
      return;
    }

    setSelectedChannelId(channelId);
    setSelectedChannelState(channelId);
    revalidator.revalidate();
  };

  return (
    <channelContext.Provider value={{ channels, selectedChannelId, selectChannel }}>
      {children}
    </channelContext.Provider>
  );
}

export function useChannelContext(): ChannelContextValue {
  return useContext(channelContext);
}