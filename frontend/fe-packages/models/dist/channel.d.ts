export type Channel = Readonly<{
    channelId: string;
    channelName: string;
    yTChannelId: string;
}>;
export type CreateChannelDTO = Omit<Channel, 'channelId'> & {
    channelId?: string;
};
export type UpdateChannelDTO = Partial<Omit<Channel, 'channelId' | 'yTChannelId'>> & {
    channelId: string;
};
//# sourceMappingURL=channel.d.ts.map