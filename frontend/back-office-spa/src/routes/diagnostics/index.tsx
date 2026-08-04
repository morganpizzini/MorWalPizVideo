import { useEffect, useState } from 'react';
import { Alert, Table } from 'react-bootstrap';
import { get } from '@morwalpizvideo/services';

interface HealthCheck {
  status: string;
  description?: string;
  durationMilliseconds: number;
}

interface BackendProblem {
  timestampUtc: string;
  category: string;
  message: string;
  properties: Record<string, string | null>;
}

interface DiagnosticsResponse {
  status: string;
  checks: Record<string, HealthCheck>;
  recentProblems: BackendProblem[];
}

interface DiagnosticsErrorResponse {
  errors?: unknown;
}

function getErrorMessage(response: DiagnosticsErrorResponse): string {
  const errors = Array.isArray(response.errors) ? response.errors : [response.errors];
  const message = errors.find((error): error is string => typeof error === 'string');

  if (message && /403|forbidden|accesso negato/i.test(message)) {
    return 'Accesso negato ai diagnostics del backend.';
  }

  return message || 'Impossibile caricare i diagnostics del backend.';
}

export default function Diagnostics() {
  const [data, setData] = useState<DiagnosticsResponse>();
  const [error, setError] = useState<string>();

  useEffect(() => {
    get('/api/Diagnostics')
      .then((response) => {
        if (response && typeof response === 'object' && 'errors' in response) {
          setError(getErrorMessage(response as DiagnosticsErrorResponse));
          return;
        }

        setData(response as DiagnosticsResponse);
      })
      .catch(() => setError('Impossibile caricare i diagnostics del backend.'));
  }, []);

  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!data) return <p>Caricamento diagnostics...</p>;

  return (
    <div>
      <h1>Diagnostics</h1>
      <p>Stato corrente: <strong>{data.status}</strong></p>
      <Table responsive striped>
        <thead><tr><th>Check</th><th>Stato</th><th>Durata</th><th>Descrizione</th></tr></thead>
        <tbody>
          {Object.entries(data.checks).map(([name, check]) => (
            <tr key={name}><td>{name}</td><td>{check.status}</td><td>{check.durationMilliseconds.toFixed(0)} ms</td><td>{check.description ?? ''}</td></tr>
          ))}
        </tbody>
      </Table>
      <h2>Problemi recenti</h2>
      {data.recentProblems.length === 0 ? <p>Nessun problema live registrato.</p> : (
        <Table responsive striped>
          <thead><tr><th>Ora</th><th>Categoria</th><th>Messaggio</th></tr></thead>
          <tbody>{data.recentProblems.map((problem, index) => (
            <tr key={`${problem.timestampUtc}-${index}`}><td>{problem.timestampUtc}</td><td>{problem.category}</td><td>{problem.message}</td></tr>
          ))}</tbody>
        </Table>
      )}
    </div>
  );
}