import axios from 'axios';
import type {
  Competition,
  CompetitionStatistics,
  CompetitionStatus,
  StageEvaluation,
  UpsertStageEvaluationRequest,
} from '../types/competition';

// API base URL - should be configured via environment variables
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';
const COMPETITIONS_ENDPOINT = `${API_BASE_URL}/api/competitions`;

// Create axios instance with default config
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Competition Service
export const competitionService = {
  /**
   * Get all competitions
   */
  async getAll(): Promise<Competition[]> {
    const response = await api.get<Competition[]>('/api/competitions');
    return response.data;
  },

  /**
   * Get competition by ID
   */
  async getById(id: string): Promise<Competition> {
    const response = await api.get<Competition>(`/api/competitions/${id}`);
    return response.data;
  },

  /**
   * Get competitions by status
   */
  async getByStatus(status: CompetitionStatus): Promise<Competition[]> {
    const response = await api.get<Competition[]>(
      `/api/competitions/status/${status}`
    );
    return response.data;
  },

  /**
   * Get upcoming competitions
   */
  async getUpcoming(): Promise<Competition[]> {
    const response = await api.get<Competition[]>('/api/competitions/upcoming');
    return response.data;
  },

  /**
   * Get active competitions (registration open or in progress)
   */
  async getActive(): Promise<Competition[]> {
    const response = await api.get<Competition[]>('/api/competitions/active');
    return response.data;
  },

  /**
   * Search competitions by name or location
   */
  async search(query: string): Promise<Competition[]> {
    const response = await api.get<Competition[]>('/api/competitions/search', {
      params: { q: query },
    });
    return response.data;
  },

  /**
   * Get competition statistics
   */
  async getStatistics(): Promise<CompetitionStatistics> {
    const response = await api.get<CompetitionStatistics>(
      '/api/competitions/stats'
    );
    return response.data;
  },

  /**
   * Create a new competition (admin only)
   */
  async create(competition: Partial<Competition>): Promise<Competition> {
    const response = await api.post<Competition>(
      '/api/competitions',
      competition
    );
    return response.data;
  },

  /**
   * Update an existing competition (admin only)
   */
  async update(
    id: string,
    competition: Partial<Competition>
  ): Promise<Competition> {
    const response = await api.put<Competition>(
      `/api/competitions/${id}`,
      competition
    );
    return response.data;
  },

  /**
   * Delete a competition (admin only)
   */
  async delete(id: string): Promise<void> {
    await api.delete(`/api/competitions/${id}`);
  },

  /**
   * Submit or update a stage evaluation
   */
  async upsertStageEvaluation(
    competitionId: string,
    stageNumber: number,
    data: UpsertStageEvaluationRequest,
    token?: string
  ): Promise<StageEvaluation> {
    const headers: Record<string, string> = {};
    if (token) headers['Authorization'] = `Bearer ${token}`;
    const response = await api.post<StageEvaluation>(
      `/api/competitions/${competitionId}/stages/${stageNumber}/evaluations`,
      data,
      { headers }
    );
    return response.data;
  },

  /**
   * Get all evaluations for a stage
   */
  async getStageEvaluations(
    competitionId: string,
    stageNumber: number
  ): Promise<StageEvaluation[]> {
    const response = await api.get<StageEvaluation[]>(
      `/api/competitions/${competitionId}/stages/${stageNumber}/evaluations`
    );
    return response.data;
  },
};

// Error handling helper
export function handleApiError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (error.response) {
      // Server responded with error
      const message = error.response.data?.message || error.response.statusText;
      return `Errore: ${message}`;
    } else if (error.request) {
      // Request made but no response
      return 'Errore di connessione al server';
    }
  }
  return 'Si è verificato un errore imprevisto';
}

export default competitionService;