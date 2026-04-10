/**
 * Terms and Conditions Page
 * 
 * Legal terms for using MorWalPiz Shop service.
 * This is a stub that will be expanded with actual legal content.
 */

import { Helmet } from 'react-helmet-async';

export default function TermsAndConditions() {
  return (
    <>
      <Helmet>
        <title>Termini e Condizioni - MorWalPiz Shop</title>
        <meta
          name="description"
          content="Termini e condizioni d'uso di MorWalPiz Shop"
        />
      </Helmet>

      <div className="container my-5">
        <h1>Termini e Condizioni</h1>

        <p className="lead">
          Benvenuto su MorWalPiz Shop. Utilizzando questo servizio, accetti i
          seguenti termini e condizioni.
        </p>

        <section className="mt-4">
          <h2>1. Accettazione dei Termini</h2>
          <p>
            Utilizzando il servizio MorWalPiz Shop, accetti i seguenti termini e
            condizioni. Se non accetti questi termini, non potrai accedere al
            servizio.
          </p>
        </section>

        <section className="mt-4">
          <h2>2. Utilizzo dell'Email</h2>
          <p>La tua email verrà utilizzata esclusivamente per:</p>
          <ul>
            <li>Gestione dell'account e autenticazione</li>
            <li>Invio di newsletter (solo se acconsentito)</li>
            <li>Comunicazioni relative agli ordini</li>
          </ul>
          <p>
            <strong>La tua email NON verrà mai ceduta a terzi.</strong>
          </p>
        </section>

        <section className="mt-4">
          <h2>3. Newsletter</h2>
          <p>
            Se hai acconsentito all'invio di newsletter, potrai disiscriverti in
            qualsiasi momento utilizzando il link presente in ogni email.
          </p>
        </section>

        <section className="mt-4">
          <h2>4. Contenuti Digitali</h2>
          <p>
            I contenuti digitali acquistati sono di tua proprietà personale e non
            possono essere ridistribuiti o rivenduti senza autorizzazione
            esplicita.
          </p>
        </section>

        <section className="mt-4">
          <h2>5. Privacy e Protezione Dati</h2>
          <p>
            I tuoi dati personali sono protetti secondo le normative GDPR. Per
            maggiori informazioni, consulta la nostra{' '}
            <a href="/privacy">Privacy Policy</a>.
          </p>
        </section>

        <section className="mt-4">
          <h2>6. Modifiche ai Termini</h2>
          <p>
            Ci riserviamo il diritto di modificare questi termini in qualsiasi
            momento. Le modifiche saranno effettive dalla data di pubblicazione
            sul sito.
          </p>
        </section>

        <p className="text-muted mt-5">
          <small>
            Ultimo aggiornamento:{' '}
            {new Date().toLocaleDateString('it-IT', {
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </small>
        </p>
      </div>
    </>
  );
}