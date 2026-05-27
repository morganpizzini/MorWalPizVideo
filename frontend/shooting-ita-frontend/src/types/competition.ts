export enum CompetitionStatus {
  Draft = 0,
  Published = 1,
  RegistrationOpen = 2,
  RegistrationClosed = 3,
  InProgress = 4,
  Completed = 5,
  Cancelled = 6,
}

export const CompetitionStatusLabels: Record<CompetitionStatus, string> = {
  [CompetitionStatus.Draft]: 'Bozza',
  [CompetitionStatus.Published]: 'Pubblicato',
  [CompetitionStatus.RegistrationOpen]: 'Iscrizioni aperte',
  [CompetitionStatus.RegistrationClosed]: 'Iscrizioni chiuse',
  [CompetitionStatus.InProgress]: 'In corso',
  [CompetitionStatus.Completed]: 'Completato',
  [CompetitionStatus.Cancelled]: 'Annullato',
};

export interface Stage {
  stageNumber: number;
  name: string;
  description?: string;
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
  startDate: string;
  endDate?: string;
  organizerId?: string;
  status: CompetitionStatus;
  maxParticipants?: number;
  registrationDeadline?: string;
  rules?: string;
  stages: Stage[];
  imageUrl?: string;
  websiteUrl?: string;
  creationDateTime: string;
}
