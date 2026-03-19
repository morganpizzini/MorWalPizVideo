export interface MorWalPizConfiguration {
    id: string;
    key: string;
    value: string;
    type: string;
    description: string;
}
export type CreateConfigurationDTO = Omit<MorWalPizConfiguration, 'id'>;
export type UpdateConfigurationDTO = Partial<Omit<MorWalPizConfiguration, 'id'>>;
//# sourceMappingURL=configuration.d.ts.map