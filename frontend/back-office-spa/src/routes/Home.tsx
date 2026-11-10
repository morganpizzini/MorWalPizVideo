import { useEffect, useState } from 'react';
import { Alert, Badge, Button, Col, Row, Spinner, Table } from 'react-bootstrap';
import {
  Activity,
  ArrowUpRight,
  Link as LinkIcon,
  LogIn,
  PlaySquare,
  RefreshCw,
  Users,
} from 'lucide-react';
import { Link, useNavigate } from 'react-router';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { endpoints, get } from '@morwalpizvideo/services';
import { getRoutePermissions, hasPermission } from '../authorization/permissions';
import { useAppStore } from '../state/appStore';
import DashboardQuickActions from '../components/DashboardQuickActions';

interface DashboardSummary {
  totalShortLinks: number;
  totalShortLinkClicks: number;
  lastBackOfficeLoginAt: string | null;
  activeUsers: number;
  publishedVideos: number;
  activeForms: number;
  formResponses: number;
  pendingInsights: number;
  generatedAt: string;
}

interface PublicationVideo {
  id: string;
  title: string;
  publishedAt: string;
}
interface PublicationDay {
  date: string;
  count: number;
  videos: PublicationVideo[];
}

type DashboardDataState =
  | { channelId: null; status: 'unavailable'; summary: null; publications: [] }
  | { channelId: string; status: 'loading'; summary: null; publications: [] }
  | {
      channelId: string;
      status: 'error';
      summary: null;
      publications: [];
      message: string;
    }
  | {
      channelId: string;
      status: 'ready';
      summary: DashboardSummary;
      publications: PublicationDay[];
    };

function formatDate(value: string | null): string {
  return value ? new Date(value).toLocaleString('it-IT') : 'Nessun accesso registrato';
}

function Kpi({
  title,
  value,
  detail,
  icon: Icon,
}: {
  title: string;
  value: string | number;
  detail: string;
  icon: typeof Activity;
}) {
  return (
    <div className="dashboard-kpi">
      <div className="d-flex justify-content-between align-items-start">
        <div>
          <div className="text-muted small">{title}</div>
          <div className="fs-3 fw-semibold mt-2">{value}</div>
        </div>
        <div className="dashboard-kpi-icon">
          <Icon size={20} />
        </div>
      </div>
      <div className="text-muted small mt-3">{detail}</div>
    </div>
  );
}

export default function Home() {
  const navigate = useNavigate();
  const selectedChannelId = useAppStore(state => state.selectedChannelId);
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canViewVideos = hasPermission(effectivePermissions, getRoutePermissions('/videos', false));
  const [retryCount, setRetryCount] = useState(0);
  const [dashboardData, setDashboardData] = useState<DashboardDataState>({
    channelId: null,
    status: 'unavailable',
    summary: null,
    publications: [],
  });

  useEffect(() => {
    if (!selectedChannelId) {
      setDashboardData({
        channelId: null,
        status: 'unavailable',
        summary: null,
        publications: [],
      });
      return;
    }

    let cancelled = false;
    setDashboardData({
      channelId: selectedChannelId,
      status: 'loading',
      summary: null,
      publications: [],
    });
    Promise.all([
      get(endpoints.DASHBOARD_SUMMARY) as Promise<DashboardSummary>,
      get(endpoints.DASHBOARD_VIDEO_PUBLICATIONS, { days: '21' }) as Promise<PublicationDay[]>,
    ])
      .then(([nextSummary, nextPublications]) => {
        if (!cancelled) {
          setDashboardData({
            channelId: selectedChannelId,
            status: 'ready',
            summary: nextSummary,
            publications: nextPublications,
          });
        }
      })
      .catch(() => {
        if (!cancelled) {
          setDashboardData({
            channelId: selectedChannelId,
            status: 'error',
            summary: null,
            publications: [],
            message: 'Impossibile caricare i dati della dashboard.',
          });
        }
      });
    return () => {
      cancelled = true;
    };
  }, [retryCount, selectedChannelId]);

  const currentDashboardData =
    dashboardData.channelId === selectedChannelId
      ? dashboardData
      : selectedChannelId
        ? {
            channelId: selectedChannelId,
            status: 'loading' as const,
            summary: null,
            publications: [],
          }
        : {
            channelId: null,
            status: 'unavailable' as const,
            summary: null,
            publications: [],
          };
  const chartData =
    currentDashboardData.status === 'ready'
      ? currentDashboardData.publications.map(day => ({
          ...day,
          label: new Date(day.date).toLocaleDateString('it-IT', {
            day: '2-digit',
            month: 'short',
          }),
        }))
      : [];

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-end gap-3 mb-4">
        <div>
          <h1 className="h3 mb-1">Dashboard</h1>
          <p className="text-muted mb-0">Panoramica operativa degli ultimi dati disponibili.</p>
        </div>
        {currentDashboardData.status === 'ready' ? (
          <Badge bg="light" text="dark">
            Aggiornata {formatDate(currentDashboardData.summary.generatedAt)}
          </Badge>
        ) : null}
      </div>
      <DashboardQuickActions />

      {currentDashboardData.status === 'loading' ? (
        <div className="dashboard-state" role="status" aria-live="polite">
          <Spinner animation="border" size="sm" />
          <span>Caricamento dei dati operativi…</span>
        </div>
      ) : null}
      {currentDashboardData.status === 'unavailable' ? (
        <Alert variant="secondary" className="dashboard-state" role="status">
          Seleziona un canale accessibile per visualizzare i dati operativi.
        </Alert>
      ) : null}
      {currentDashboardData.status === 'error' ? (
        <Alert variant="danger" className="dashboard-state" role="alert">
          <span>{currentDashboardData.message}</span>
          <Button
            variant="outline-danger"
            size="sm"
            onClick={() => setRetryCount(count => count + 1)}
          >
            <RefreshCw size={15} aria-hidden="true" /> Riprova
          </Button>
        </Alert>
      ) : null}

      {currentDashboardData.status === 'ready' ? (
        <>
          <Row className="g-3 mb-4">
            <Col sm={6} xl={3}>
              <Kpi
                title="Click short link"
                value={currentDashboardData.summary.totalShortLinkClicks.toLocaleString('it-IT')}
                detail={`Ultimi 7 giorni · ${currentDashboardData.summary.totalShortLinks} link gestiti`}
                icon={LinkIcon}
              />
            </Col>
            <Col sm={6} xl={3}>
              <Kpi
                title="Ultimo accesso BackOffice"
                value={
                  currentDashboardData.summary.lastBackOfficeLoginAt
                    ? new Date(
                        currentDashboardData.summary.lastBackOfficeLoginAt
                      ).toLocaleDateString('it-IT')
                    : '-'
                }
                detail={formatDate(currentDashboardData.summary.lastBackOfficeLoginAt)}
                icon={LogIn}
              />
            </Col>
            <Col sm={6} xl={3}>
              <Kpi
                title="Video pubblicati"
                value={currentDashboardData.summary.publishedVideos}
                detail="Finestra mobile di 21 giorni"
                icon={PlaySquare}
              />
            </Col>
            <Col sm={6} xl={3}>
              <Kpi
                title="Utenti attivi"
                value={currentDashboardData.summary.activeUsers}
                detail={`${currentDashboardData.summary.activeForms} form attivi`}
                icon={Users}
              />
            </Col>
          </Row>
          <Row className="g-3">
            <Col xl={8}>
              <section className="dashboard-panel">
                <div className="d-flex justify-content-between align-items-start mb-3">
                  <div>
                    <h2 className="h5 mb-1">Pubblicazione video</h2>
                    <p className="text-muted small mb-0">
                      Video caricati nel BackOffice ordinati per <code>PublishedAt</code>.
                    </p>
                  </div>
                  <Badge bg="primary">21 giorni</Badge>
                </div>
                <div className="dashboard-chart">
                  <ResponsiveContainer>
                    <BarChart
                      data={chartData}
                      onClick={event => {
                        if (!canViewVideos) return;
                        const day = chartData.find(item => item.label === event?.activeLabel);
                        const video = day?.videos[0];
                        if (video) navigate(`/videos/${video.id}`);
                      }}
                    >
                      <CartesianGrid strokeDasharray="3 3" vertical={false} />
                      <XAxis dataKey="label" tick={{ fontSize: 12 }} interval="preserveStartEnd" />
                      <YAxis allowDecimals={false} />
                      <Tooltip formatter={value => [value ?? 0, 'Video']} />
                      <Bar
                        dataKey="count"
                        fill="#2f6f8f"
                        radius={[4, 4, 0, 0]}
                        cursor={canViewVideos ? 'pointer' : 'default'}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </section>
            </Col>
            <Col xl={4}>
              <section className="dashboard-panel">
                <div className="d-flex justify-content-between align-items-center mb-3">
                  <h2 className="h5 mb-0">Stato operativo</h2>
                  <Activity size={18} aria-hidden="true" />
                </div>
                <div className="d-flex justify-content-between py-2 border-bottom">
                  <span>Risposte form</span>
                  <strong>{currentDashboardData.summary.formResponses}</strong>
                </div>
                <div className="d-flex justify-content-between py-2 border-bottom">
                  <span>Insights in attesa</span>
                  <strong>{currentDashboardData.summary.pendingInsights}</strong>
                </div>
                <div className="d-flex justify-content-between py-2">
                  <span>Ultimi 21 giorni</span>
                  <strong>{currentDashboardData.summary.publishedVideos} video</strong>
                </div>
              </section>
            </Col>
          </Row>
          <section className="dashboard-panel mt-3">
            <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
              <h2 className="h5 mb-0">Pubblicazioni recenti</h2>
              {canViewVideos ? (
                <Link to="/videos" className="btn btn-outline-primary btn-sm">
                  Apri video <ArrowUpRight size={14} aria-hidden="true" />
                </Link>
              ) : null}
            </div>
            {currentDashboardData.publications.some(day => day.videos.length > 0) ? (
              <Table responsive hover className="mb-0 align-middle">
                <thead>
                  <tr>
                    <th>Video</th>
                    <th>Giorno</th>
                  </tr>
                </thead>
                <tbody>
                  {currentDashboardData.publications
                    .flatMap(day => day.videos)
                    .slice(-8)
                    .reverse()
                    .map(video => (
                      <tr key={`${video.id}-${video.publishedAt}`}>
                        <td>
                          {canViewVideos ? (
                            <Link to={`/videos/${video.id}`} className="dashboard-video-link">
                              {video.title || video.id}
                            </Link>
                          ) : (
                            video.title || video.id
                          )}
                        </td>
                        <td>{new Date(video.publishedAt).toLocaleDateString('it-IT')}</td>
                      </tr>
                    ))}
                </tbody>
              </Table>
            ) : (
              <p className="text-muted mb-0">Nessun video pubblicato negli ultimi 21 giorni.</p>
            )}
          </section>
        </>
      ) : null}
    </>
  );
}
