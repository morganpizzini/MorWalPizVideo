// Competition domain types matching C# backend models

export enum CompetitionStatus {
  Draft = 0,
  Published = 1,
  RegistrationOpen = 2,
  RegistrationClosed = 3,
  InProgress = 4,
  Completed = 5,
  Cancelled = 6,
}

export interface Stage {
  stageNumber: number;
  name: string;
  targetCount: number;
  roundCount: number;
  minScore: number;
  maxScore: number;
  timeLimitSeconds?: number;
  briefing?: string;
  order: number;
}

export interface Competition {
  id: string;
  name: string;
  description?: string;
  location?: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
  organizerId?: string;
  status: CompetitionStatus;
  maxParticipants?: number;
  registrationDeadline?: string; // ISO date string
  rules?: string;
  stages: Stage[];
  imageUrl?: string;
  websiteUrl?: string;
  creationDateTime: string; // ISO date string
}

export interface CompetitionStatistics {
  totalCompetitions: number;
  byStatus: Array<{ status: string; count: number }>;
  upcomingCompetitions: number;
  ongoingCompetitions: number;
  completedCompetitions: number;
  averageStagesPerCompetition: number;
  competitionsWithRegistrationOpen: number;
}

// Helper functions
export function getStatusLabel(status: CompetitionStatus): string {
  switch (status) {
    case CompetitionStatus.Draft:
      return 'Bozza';
    case CompetitionStatus.Published:
      return 'Pubblicata';
    case CompetitionStatus.RegistrationOpen:
      return 'Iscrizioni Aperte';
    case CompetitionStatus.RegistrationClosed:
      return 'Iscrizioni Chiuse';
    case CompetitionStatus.InProgress:
      return 'In Corso';
    case CompetitionStatus.Completed:
      return 'Completata';
    case CompetitionStatus.Cancelled:
      return 'Annullata';
    default:
      return 'Sconosciuto';
  }
}

export function getStatusBadgeClass(status: CompetitionStatus): string {
  switch (status) {
    case CompetitionStatus.Draft:
      return 'bg-secondary';
    case CompetitionStatus.Published:
      return 'bg-info';
    case CompetitionStatus.RegistrationOpen:
      return 'bg-success';
    case CompetitionStatus.RegistrationClosed:
      return 'bg-warning';
    case CompetitionStatus.InProgress:
      return 'bg-primary';
    case CompetitionStatus.Completed:
      return 'bg-dark';
    case CompetitionStatus.Cancelled:
      return 'bg-danger';
    default:
      return 'bg-secondary';
  }
}

export function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString('it-IT', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

export function formatDateTime(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleString('it-IT', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}