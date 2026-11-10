import { Alert, Spinner } from 'react-bootstrap';
import {
  CalendarDays,
  FilePlus2,
  FileText,
  Link as LinkIcon,
  ListVideo,
  Settings,
  WandSparkles,
} from 'lucide-react';
import { Link } from 'react-router';
import { useDashboardQuickActions } from '../hooks/useDashboardQuickActions';

const icons = {
  videos: ListVideo,
  'article-create': FilePlus2,
  articles: FileText,
  calendar: CalendarDays,
  editorial: WandSparkles,
  link: LinkIcon,
};

export default function DashboardQuickActions() {
  const { available, state, reload } = useDashboardQuickActions();
  return (
    <section className="dashboard-actions mb-4" aria-labelledby="dashboard-actions-title">
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h2 id="dashboard-actions-title" className="h5 mb-0">
          Attività rapide
        </h2>
        <Link to="/dashboard-actions" className="btn btn-outline-secondary btn-sm">
          <Settings size={16} aria-hidden="true" /> Personalizza
        </Link>
      </div>
      {!available ? (
        <p role="status">Seleziona un canale accessibile per le tue attività rapide.</p>
      ) : state.status === 'loading' ? (
        <div role="status">
          <Spinner animation="border" size="sm" /> Caricamento attività rapide…
        </div>
      ) : state.status === 'error' ? (
        <Alert variant="danger">
          {state.message}{' '}
          <button type="button" className="btn btn-outline-danger btn-sm" onClick={reload}>
            Riprova attività rapide
          </button>
        </Alert>
      ) : (
        <>
          <div className="d-flex flex-wrap gap-2">
            {state.data.visibleActions.map(action => {
              const Icon = icons[action.icon as keyof typeof icons] ?? LinkIcon;
              const content = (
                <>
                  <Icon size={17} aria-hidden="true" />
                  <span>{action.label}</span>
                </>
              );
              return action.kind === 'custom' ? (
                <a key={action.id} href={action.url} className="btn btn-outline-primary">
                  {content}
                </a>
              ) : (
                <Link key={action.id} to={action.url} className="btn btn-outline-primary">
                  {content}
                </Link>
              );
            })}
          </div>
          {state.data.visibleActions.length === 0 ? (
            <p className="text-muted mb-0" role="status">
              {state.data.actions.length === 0
                ? 'Nessuna attività selezionata. Personalizza i tuoi collegamenti.'
                : 'Le attività selezionate non sono attualmente disponibili con i tuoi permessi.'}
            </p>
          ) : null}
        </>
      )}
    </section>
  );
}
