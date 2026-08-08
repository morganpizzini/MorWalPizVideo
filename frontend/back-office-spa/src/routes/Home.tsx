import { useEffect, useState } from 'react';
import { Alert, Badge, Button, Col, Row, Spinner, Table } from 'react-bootstrap';
import { Activity, ExternalLink, Link as LinkIcon, LogIn, PlaySquare, Users } from 'lucide-react';
import { useNavigate } from 'react-router';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { endpoints, get } from '@morwalpizvideo/services';

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

interface PublicationVideo { id: string; title: string; publishedAt: string; }
interface PublicationDay { date: string; count: number; videos: PublicationVideo[]; }

function formatDate(value: string | null): string {
  return value ? new Date(value).toLocaleString('it-IT') : 'Nessun accesso registrato';
}

function Kpi({ title, value, detail, icon: Icon }: { title: string; value: string | number; detail: string; icon: typeof Activity }) {
  return <div className="dashboard-kpi"><div className="d-flex justify-content-between align-items-start"><div><div className="text-muted small">{title}</div><div className="fs-3 fw-semibold mt-2">{value}</div></div><div className="dashboard-kpi-icon"><Icon size={20} /></div></div><div className="text-muted small mt-3">{detail}</div></div>;
}

export default function Home() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [publications, setPublications] = useState<PublicationDay[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    Promise.all([
      get(endpoints.DASHBOARD_SUMMARY) as Promise<DashboardSummary>,
      get(endpoints.DASHBOARD_VIDEO_PUBLICATIONS, { days: '21' }) as Promise<PublicationDay[]>,
    ]).then(([nextSummary, nextPublications]) => {
      if (!cancelled) { setSummary(nextSummary); setPublications(nextPublications); }
    }).catch(() => { if (!cancelled) setError('Impossibile caricare i dati della dashboard.'); });
    return () => { cancelled = true; };
  }, []);

  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!summary) return <div className="d-flex justify-content-center py-5"><Spinner /></div>;

  const chartData = publications.map(day => ({ ...day, label: new Date(day.date).toLocaleDateString('it-IT', { day: '2-digit', month: 'short' }) }));
  const recentVideos = publications.flatMap(day => day.videos).slice(-8).reverse();

  return <>
    <div className="d-flex flex-wrap justify-content-between align-items-end gap-3 mb-4"><div><h1 className="h3 mb-1">Dashboard</h1><p className="text-muted mb-0">Panoramica operativa degli ultimi dati disponibili.</p></div><Badge bg="light" text="dark">Aggiornata {formatDate(summary.generatedAt)}</Badge></div>
    <Row className="g-3 mb-4">
      <Col sm={6} xl={3}><Kpi title="Click short link" value={summary.totalShortLinkClicks.toLocaleString('it-IT')} detail={`${summary.totalShortLinks} link gestiti`} icon={LinkIcon} /></Col>
      <Col sm={6} xl={3}><Kpi title="Ultimo accesso BackOffice" value={summary.lastBackOfficeLoginAt ? new Date(summary.lastBackOfficeLoginAt).toLocaleDateString('it-IT') : '-'} detail={formatDate(summary.lastBackOfficeLoginAt)} icon={LogIn} /></Col>
      <Col sm={6} xl={3}><Kpi title="Video pubblicati" value={summary.publishedVideos} detail="Finestra mobile di 21 giorni" icon={PlaySquare} /></Col>
      <Col sm={6} xl={3}><Kpi title="Utenti attivi" value={summary.activeUsers} detail={`${summary.activeForms} form attivi`} icon={Users} /></Col>
    </Row>
    <Row className="g-3"><Col xl={8}><section className="dashboard-panel"><div className="d-flex justify-content-between align-items-start mb-3"><div><h2 className="h5 mb-1">Pubblicazione video</h2><p className="text-muted small mb-0">Video caricati nel BackOffice ordinati per <code>PublishedAt</code>.</p></div><Badge bg="primary">21 giorni</Badge></div><div style={{ width: '100%', height: 320 }}><ResponsiveContainer><BarChart data={chartData} onClick={event => { const day = chartData.find(item => item.label === event?.activeLabel); const video = day?.videos[0]; if (video) navigate(`/videos/${video.id}`); }}><CartesianGrid strokeDasharray="3 3" vertical={false} /><XAxis dataKey="label" tick={{ fontSize: 12 }} /><YAxis allowDecimals={false} /><Tooltip formatter={value => [value ?? 0, 'Video']} /><Bar dataKey="count" fill="#2f6f8f" radius={[4, 4, 0, 0]} cursor="pointer" /></BarChart></ResponsiveContainer></div></section></Col><Col xl={4}><section className="dashboard-panel"><div className="d-flex justify-content-between align-items-center mb-3"><h2 className="h5 mb-0">Stato operativo</h2><Activity size={18} /></div><div className="d-flex justify-content-between py-2 border-bottom"><span>Risposte form</span><strong>{summary.formResponses}</strong></div><div className="d-flex justify-content-between py-2 border-bottom"><span>Insights in attesa</span><strong>{summary.pendingInsights}</strong></div><div className="d-flex justify-content-between py-2"><span>Ultimi 21 giorni</span><strong>{summary.publishedVideos} video</strong></div></section></Col></Row>
    <section className="dashboard-panel mt-3"><div className="d-flex justify-content-between align-items-center mb-3"><h2 className="h5 mb-0">Pubblicazioni recenti</h2><Button variant="outline-primary" size="sm" onClick={() => navigate('/videos')}>Apri video <ExternalLink size={14} /></Button></div><Table responsive hover className="mb-0 align-middle"><thead><tr><th>Video</th><th>Data pubblicazione</th><th>Giorno</th></tr></thead><tbody>{recentVideos.map(video => <tr key={`${video.id}-${video.publishedAt}`} onClick={() => navigate(`/videos/${video.id}`)} style={{ cursor: 'pointer' }}><td>{video.title || video.id}</td><td>{formatDate(video.publishedAt)}</td><td>{new Date(video.publishedAt).toLocaleDateString('it-IT')}</td></tr>)}</tbody></Table></section>
  </>;
}