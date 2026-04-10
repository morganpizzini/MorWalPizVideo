/**
 * Privacy Policy Page
 * 
 * Displays the privacy policy for MorWalPiz Shop
 */

import { Helmet } from 'react-helmet-async';

export default function Privacy() {
  return (
    <>
      <Helmet>
        <title>Privacy Policy - MorWalPiz Shop</title>
        <meta
          name="description"
          content="Informativa sulla privacy di MorWalPiz Shop"
        />
      </Helmet>

      <div className="container my-5">
        <div className="row">
          <div className="col-lg-10 mx-auto">
            <h1 className="mb-4">Privacy Policy</h1>
            <p className="text-muted">
              Ultimo aggiornamento: {new Date().toLocaleDateString('it-IT')}
            </p>

            <section className="mt-5">
              <h2>1. Introduzione</h2>
              <p>
                La presente Privacy Policy descrive come MorWalPiz Shop ("noi", "il nostro", "la nostra") 
                raccoglie, utilizza e protegge le informazioni personali degli utenti ("tu", "il tuo", "la tua") 
                che utilizzano il nostro sito web e i nostri servizi.
              </p>
              <p>
                Ci impegniamo a proteggere la tua privacy e a trattare i tuoi dati personali in conformità 
                con il Regolamento Generale sulla Protezione dei Dati (GDPR) dell'Unione Europea e le 
                normative italiane applicabili.
              </p>
            </section>

            <section className="mt-5">
              <h2>2. Titolare del Trattamento</h2>
              <p>
                Il titolare del trattamento dei dati personali è MorWalPiz Shop.
                Per qualsiasi comunicazione relativa al trattamento dei tuoi dati personali, 
                puoi contattarci tramite i canali indicati nella sezione "Contatti" in fondo a questa pagina.
              </p>
            </section>

            <section className="mt-5">
              <h2>3. Dati Raccolti</h2>
              <p>Raccogliamo e trattiamo le seguenti categorie di dati personali:</p>
              
              <h3 className="h5 mt-4">3.1 Dati forniti volontariamente</h3>
              <ul>
                <li>
                  <strong>Informazioni di registrazione:</strong> indirizzo email
                </li>
                <li>
                  <strong>Preferenze:</strong> consenso alla newsletter (opzionale)
                </li>
                <li>
                  <strong>Dati di acquisto:</strong> cronologia degli acquisti, prodotti nel carrello
                </li>
              </ul>

              <h3 className="h5 mt-4">3.2 Dati raccolti automaticamente</h3>
              <ul>
                <li>
                  <strong>Dati tecnici:</strong> indirizzo IP, tipo di browser, sistema operativo, 
                  informazioni sul dispositivo
                </li>
                <li>
                  <strong>Dati di navigazione:</strong> pagine visitate, tempo di permanenza, 
                  sorgente di traffico
                </li>
                <li>
                  <strong>Cookie e tecnologie simili:</strong> come descritto nella nostra 
                  Cookie Policy
                </li>
              </ul>
            </section>

            <section className="mt-5">
              <h2>4. Base Giuridica e Finalità del Trattamento</h2>
              
              <h3 className="h5 mt-4">4.1 Esecuzione del contratto</h3>
              <p>
                Trattiamo i tuoi dati per l'esecuzione del contratto di vendita, inclusi:
              </p>
              <ul>
                <li>Gestione dell'account utente</li>
                <li>Elaborazione degli ordini</li>
                <li>Consegna dei prodotti digitali acquistati</li>
                <li>Assistenza clienti</li>
              </ul>

              <h3 className="h5 mt-4">4.2 Consenso</h3>
              <p>
                Con il tuo consenso esplicito, utilizziamo i tuoi dati per:
              </p>
              <ul>
                <li>Invio di newsletter e comunicazioni promozionali (solo se hai acconsentito)</li>
                <li>Utilizzo di cookie non essenziali (come descritto nella Cookie Policy)</li>
              </ul>

              <h3 className="h5 mt-4">4.3 Obblighi legali</h3>
              <p>
                Trattiamo i tuoi dati per adempiere agli obblighi di legge, inclusi:
              </p>
              <ul>
                <li>Conservazione delle fatture e documenti fiscali</li>
                <li>Risposta a richieste delle autorità competenti</li>
              </ul>

              <h3 className="h5 mt-4">4.4 Interesse legittimo</h3>
              <p>
                Trattiamo i tuoi dati per il nostro legittimo interesse a:
              </p>
              <ul>
                <li>Prevenire frodi e abusi</li>
                <li>Migliorare la sicurezza del sito</li>
                <li>Analizzare l'utilizzo del sito per migliorare i nostri servizi</li>
              </ul>
            </section>

            <section className="mt-5">
              <h2>5. Condivisione dei Dati</h2>
              <p>
                Non vendiamo, né cediamo i tuoi dati personali a terze parti per finalità di marketing. 
                Potremmo condividere i tuoi dati con:
              </p>
              <ul>
                <li>
                  <strong>Fornitori di servizi:</strong> società che ci assistono nella gestione 
                  del sito e dei servizi (es. hosting, email, pagamenti)
                </li>
                <li>
                  <strong>Partner tecnologici:</strong> Google (reCAPTCHA, Analytics) in conformità 
                  con le loro politiche sulla privacy
                </li>
                <li>
                  <strong>Autorità competenti:</strong> quando richiesto dalla legge o per 
                  proteggere i nostri diritti legali
                </li>
              </ul>
              <p>
                Tutti i fornitori terzi sono selezionati con cura e sono obbligati contrattualmente 
                a proteggere i tuoi dati e a utilizzarli solo per le finalità specificate.
              </p>
            </section>

            <section className="mt-5">
              <h2>6. Trasferimento dei Dati all'Estero</h2>
              <p>
                I tuoi dati potrebbero essere trasferiti a server situati al di fuori dello 
                Spazio Economico Europeo (SEE). In tal caso, garantiamo che vengano applicate 
                misure di salvaguardia adeguate, come le Clausole Contrattuali Standard approvate 
                dalla Commissione Europea.
              </p>
            </section>

            <section className="mt-5">
              <h2>7. Conservazione dei Dati</h2>
              <p>
                Conserviamo i tuoi dati personali per il tempo strettamente necessario 
                a raggiungere le finalità per cui sono stati raccolti:
              </p>
              <ul>
                <li>
                  <strong>Dati dell'account:</strong> fino alla cancellazione dell'account 
                  o alla richiesta di cancellazione
                </li>
                <li>
                  <strong>Dati di acquisto:</strong> 10 anni per obblighi fiscali e contabili
                </li>
                <li>
                  <strong>Consensi marketing:</strong> fino alla revoca del consenso
                </li>
                <li>
                  <strong>Dati di navigazione:</strong> massimo 24 mesi
                </li>
              </ul>
            </section>

            <section className="mt-5">
              <h2>8. I Tuoi Diritti</h2>
              <p>
                In conformità con il GDPR, hai il diritto di:
              </p>
              <ul>
                <li>
                  <strong>Accesso:</strong> ottenere conferma del trattamento e una copia 
                  dei tuoi dati
                </li>
                <li>
                  <strong>Rettifica:</strong> correggere dati inesatti o incompleti
                </li>
                <li>
                  <strong>Cancellazione:</strong> richiedere la cancellazione dei tuoi dati 
                  ("diritto all'oblio")
                </li>
                <li>
                  <strong>Limitazione:</strong> limitare il trattamento in determinate circostanze
                </li>
                <li>
                  <strong>Portabilità:</strong> ricevere i tuoi dati in formato strutturato 
                  e trasferirli ad altro titolare
                </li>
                <li>
                  <strong>Opposizione:</strong> opporti al trattamento basato su interesse legittimo
                </li>
                <li>
                  <strong>Revoca consenso:</strong> revocare il consenso in qualsiasi momento 
                  (senza pregiudicare la liceità del trattamento precedente)
                </li>
                <li>
                  <strong>Reclamo:</strong> presentare reclamo all'Autorità Garante per la 
                  Protezione dei Dati Personali
                </li>
              </ul>
              <p>
                Per esercitare questi diritti, contattaci utilizzando i canali indicati nella 
                sezione "Contatti".
              </p>
            </section>

            <section className="mt-5">
              <h2>9. Sicurezza dei Dati</h2>
              <p>
                Adottiamo misure tecniche e organizzative appropriate per proteggere i tuoi 
                dati personali da accessi non autorizzati, perdita, distruzione o alterazione, 
                inclusi:
              </p>
              <ul>
                <li>Crittografia dei dati in transito (HTTPS/TLS)</li>
                <li>Autenticazione sicura con reCAPTCHA v3</li>
                <li>Backup regolari dei dati</li>
                <li>Limitazione degli accessi ai dati al personale autorizzato</li>
                <li>Monitoraggio e audit dei sistemi</li>
              </ul>
            </section>

            <section className="mt-5">
              <h2>10. Cookie e Tecnologie di Tracciamento</h2>
              <p>
                Utilizziamo cookie e tecnologie simili per migliorare la tua esperienza sul 
                nostro sito. Per informazioni dettagliate, consulta la nostra{' '}
                <a href="/cookie-policy">Cookie Policy</a>.
              </p>
            </section>

            <section className="mt-5">
              <h2>11. Minori</h2>
              <p>
                I nostri servizi non sono destinati a minori di 18 anni. Non raccogliamo 
                consapevolmente dati personali di minori. Se veniamo a conoscenza di aver 
                raccolto dati di un minore, procederemo immediatamente alla loro cancellazione.
              </p>
            </section>

            <section className="mt-5">
              <h2>12. Modifiche alla Privacy Policy</h2>
              <p>
                Ci riserviamo il diritto di modificare questa Privacy Policy in qualsiasi momento. 
                Le modifiche sostanziali saranno notificate tramite il sito web o via email. 
                La data dell'ultimo aggiornamento è indicata in cima a questa pagina.
              </p>
            </section>

            <section className="mt-5">
              <h2>13. Contatti</h2>
              <p>
                Per qualsiasi domanda o richiesta relativa al trattamento dei tuoi dati personali, 
                puoi contattarci:
              </p>
              <ul>
                <li>Email: privacy@morwalpizshop.it</li>
                <li>Telegram: <a href="https://t.me/morwalpiz" target="_blank" rel="noopener noreferrer">@morwalpiz</a></li>
              </ul>
            </section>

            <section className="mt-5 mb-5">
              <div className="alert alert-info">
                <h3 className="h5">Autorità Garante</h3>
                <p className="mb-0">
                  Hai il diritto di presentare reclamo all'Autorità Garante per la Protezione 
                  dei Dati Personali:<br />
                  <strong>Garante per la protezione dei dati personali</strong><br />
                  Piazza Venezia, 11 - 00187 Roma<br />
                  Tel: +39 06.696771<br />
                  Email: garante@gpdp.it<br />
                  Web: <a href="https://www.garanteprivacy.it" target="_blank" rel="noopener noreferrer">www.garanteprivacy.it</a>
                </p>
              </div>
            </section>
          </div>
        </div>
      </div>
    </>
  );
}