import { get } from '@morwalpizvideo/services';

interface FeatureStateResponse {
  videoBulkImportEnabled: boolean;
}

export const featureManager = {
  getFeatureState: async (): Promise<FeatureStateResponse> => {
    return get('/api/features') as Promise<FeatureStateResponse>;
  },
};