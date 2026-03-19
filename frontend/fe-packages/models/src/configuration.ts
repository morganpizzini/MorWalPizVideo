/**
 * Represents a configuration entry in the system
 */
export interface MorWalPizConfiguration {
  /** Unique identifier for the configuration */
  id: string;

  /** Key of the configuration */
  key: string;

  /** Value of the configuration */
  value: string;

  /** Type of the configuration value */
  type: string;

  /** Description of the configuration */
  description: string;
}

/**
 * Type for creating a new configuration
 */
export type CreateConfigurationDTO = Omit<MorWalPizConfiguration, 'id'>;

/**
 * Type for updating an existing configuration
 */
export type UpdateConfigurationDTO = Partial<Omit<MorWalPizConfiguration, 'id'>>;
