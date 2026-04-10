/**
 * Cookie Policy Page
 * 
 * Displays the cookie policy for MorWalPiz Shop
 */

import { Helmet } from 'react-helmet-async';

export default function CookiePolicy() {
  return (
    <>
      <Helmet>
        <title>Cookie Policy - MorWalPiz Shop</title>
        <meta
          name="description"
          content="Informativa sui cookie di MorWalPiz Shop"
        />
      </Helmet>

      <div className="container my-5">
        <div className="row">
          <div className="col-lg-10 mx-auto">
            <h1 className="mb-4">Cookie Policy</h1>
            <p className="text-muted">
              Ultimo aggiornamento: {new Date().toLocaleDateString('it-IT')}
            </p>

            <section className="mt-5">
              <h2>1. Introduzione</h2>
              <p>
                Questa Cookie Policy spiega cosa sono i cookie, come li utilizziamo sul sito 
                MorWalPiz Shop e quali sono le tue opzioni per gestirli. Ti consigliamo di 
                leggere questa policy insieme alla nostra{' '}
                <a href="/privacy">Privacy Policy</a> per comprendere pienamente come 
                gestiamo i tuoi dati personali.
              </p>
            </section>

            <section className="mt-5">
              <h2>2. Cosa sono i Cookie</h2>
              <p>
                I cookie sono piccoli file di testo che vengono memorizzati sul tuo dispositivo 
                (computer, tablet, smartphone) quando visiti un sito web. I cookie permettono al 
                sito di riconoscere il tuo dispositivo e memorizzare alcune informazioni sulle 
                tue preferenze o azioni passate.
              </p>
              <p>
                I cookie possono essere "di sessione" (che vengono eliminati quando chiudi il 
                browser) o "persistenti" (che rimangono sul tuo dispositivo per un periodo di 
                tempo più lungo o fino a quando non li elimini manualmente).
              </p>
            </section>

            <section className="mt-5">
              <h2>3. Tipologie di Cookie Utilizzati</h2>

              <h3 className="h5 mt-4">3.1 Cookie Strettamente Necessari</h3>
              <p>
                Questi cookie sono essenziali per il funzionamento del sito web e non possono 
                essere disabilitati nei nostri sistemi. Sono generalmente impostati solo in 
                risposta ad azioni da te effettuate che costituiscono una richiesta di servizi, 
                come l'impostazione delle tue preferenze sulla privacy, l'accesso o la 
                compilazione di moduli.
              </p>
              <div className="table-responsive">
                <table className="table table-bordered">
                  <thead>
                    <tr>
                      <th>Nome Cookie</th>
                      <th>Finalità</th>
                      <th>Durata</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>auth_session</td>
                      <td>Memorizza il token di sessione per mantenere l'utente autenticato</td>
                      <td>24 ore</td>
                    </tr>
                    <tr>
                      <td>cart_id</td>
                      <td>Identifica il carrello dell'utente</td>
                      <td>30 giorni</td>
                    </tr>
                    <tr>
                      <td>cookie_consent</td>
                      <td>Memorizza le preferenze sui cookie dell'utente</td>
                      <td>1 anno</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <h3 className="h5 mt-4">3.2 Cookie di Prestazione e Analitici</h3>
              <p>
                Questi cookie ci permettono di contare le visite e le fonti di traffico per 
                misurare e migliorare le prestazioni del nostro sito. Ci aiutano a sapere quali 
                pagine sono più e meno popolari e a vedere come i visitatori si muovono 
                all'interno del sito.
              </p>
              <div className="table-responsive">
                <table className="table table-bordered">
                  <thead>
                    <tr>
                      <th>Nome Cookie</th>
                      <th>Provider</th>
                      <th>Finalità</th>
                      <th>Durata</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>_ga</td>
                      <td>Google Analytics</td>
                      <td>Distingue gli utenti e raccoglie dati analitici</td>
                      <td>2 anni</td>
                    </tr>
                    <tr>
                      <td>_gid</td>
                      <td>Google Analytics</td>
                      <td>Distingue gli utenti</td>
                      <td>24 ore</td>
                    </tr>
                    <tr>
                      <td>_gat</td>
                      <td>Google Analytics</td>
                      <td>Limita la frequenza delle richieste</td>
                      <td>1 minuto</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <h3 className="h5 mt-4">3.3 Cookie di Sicurezza</h3>
              <p>
                Questi cookie sono utilizzati per proteggere il sito web da attività 
                fraudolente e garantire la sicurezza delle transazioni.
              </p>
              <div className="table-responsive">
                <table className="table table-bordered">
                  <thead>
                    <tr>
                      <th>Nome Cookie</th>
                      <th>Provider</th>
                      <th>Finalità</th>
                      <th>Durata</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>_GRECAPTCHA</td>
                      <td>Google reCAPTCHA</td>
                      <td>Protezione da bot e spam</td>
                      <td>6 mesi</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <h3 className="h5 mt-4">3.4 Cookie di Funzionalità</h3>
              <p>
                Questi cookie permettono al sito web di fornire funzionalità avanzate e 
                personalizzazione. Possono essere impostati da noi o da fornitori terzi i cui 
                servizi abbiamo aggiunto alle nostre pagine.
              </p>
              <div className="alert alert-info">
                <strong>Nota:</strong> Attualmente non utilizziamo cookie di questa categoria, 
                ma potremmo farlo in futuro per migliorare l'esperienza utente.
              </div>

              <h3 className="h5 mt-4">3.5 Cookie di Targeting/Pubblicità</h3>
              <div className="alert alert-success">
                <strong>Buone notizie!</strong> Non utilizziamo cookie di targeting o 
                pubblicità. Non tracciamo le tue attività su altri siti web per finalità 
                pubblicitarie.
              </div>
            </section>

            <section className="mt-5">
              <h2>4. Cookie di Prima Parte vs Cookie di Terze Parti</h2>
              
              <h3 className="h5 mt-4">4.1 Cookie di Prima Parte</h3>
              <p>
                Questi cookie sono impostati direttamente dal nostro sito web e sono utilizzati 
                solo da noi. Includono i cookie strettamente necessari e alcuni cookie di 
                funzionalità.
              </p>

              <h3 className="h5 mt-4">4.2 Cookie di Terze Parti</h3>
              <p>
                Questi cookie sono impostati da servizi di terze parti che utilizziamo sul 
                nostro sito, come:
              </p>
              <ul>
                <li>
                  <strong>Google reCAPTCHA:</strong> utilizzato per proteggere il sito da bot 
                  e spam
                </li>
                <li>
                  <strong>Google Analytics:</strong> utilizzato per analizzare l'utilizzo del 
                  sito (solo se hai acconsentito)
                </li>
              </ul>
              <p>
                Questi servizi hanno le proprie politiche sulla privacy che regolano l'uso 
                dei loro cookie:
              </p>
              <ul>
                <li>
                  <a
                    href="https://policies.google.com/privacy"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Google Privacy Policy
                  </a>
                </li>
                <li>
                  <a
                    href="https://policies.google.com/technologies/cookies"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Google Cookie Policy
                  </a>
                </li>
              </ul>
            </section>

            <section className="mt-5">
              <h2>5. Come Gestiamo il Tuo Consenso</h2>
              <p>
                In conformità con la normativa europea sui cookie (Direttiva ePrivacy), 
                richiediamo il tuo consenso prima di installare cookie non essenziali sul 
                tuo dispositivo.
              </p>
              <p>
                <strong>Cookie Essenziali:</strong> Non richiedono il tuo consenso perché sono 
                strettamente necessari per fornire i servizi che richiedi esplicitamente.
              </p>
              <p>
                <strong>Cookie Non Essenziali:</strong> Richiedono il tuo consenso esplicito. 
                Puoi scegliere di accettare o rifiutare queste categorie di cookie tramite il 
                banner dei cookie che appare quando visiti il sito per la prima volta.
              </p>
            </section>

            <section className="mt-5">
              <h2>6. Come Controllare e Gestire i Cookie</h2>

              <h3 className="h5 mt-4">6.1 Impostazioni del Browser</h3>
              <p>
                La maggior parte dei browser web ti permette di controllare i cookie attraverso 
                le impostazioni. Puoi configurare il tuo browser per rifiutare i cookie o per 
                avvisarti quando un sito web tenta di inserire un cookie sul tuo dispositivo.
              </p>
              <p>
                Ecco i link alle istruzioni per i browser più comuni:
              </p>
              <ul>
                <li>
                  <a
                    href="https://support.google.com/chrome/answer/95647"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Google Chrome
                  </a>
                </li>
                <li>
                  <a
                    href="https://support.mozilla.org/it/kb/Gestione%20dei%20cookie"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Mozilla Firefox
                  </a>
                </li>
                <li>
                  <a
                    href="https://support.apple.com/it-it/guide/safari/sfri11471/mac"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Safari
                  </a>
                </li>
                <li>
                  <a
                    href="https://support.microsoft.com/it-it/windows/eliminare-e-gestire-i-cookie-168dab11-0753-043d-7c16-ede5947fc64d"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Microsoft Edge
                  </a>
                </li>
              </ul>

              <h3 className="h5 mt-4">6.2 Disattivazione di Google Analytics</h3>
              <p>
                Se desideri impedire a Google Analytics di raccogliere dati sulla tua 
                navigazione, puoi installare il{' '}
                <a
                  href="https://tools.google.com/dlpage/gaoptout"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  componente aggiuntivo del browser per la disattivazione di Google Analytics
                </a>.
              </p>

              <h3 className="h5 mt-4">6.3 Gestione del Consenso</h3>
              <p>
                Puoi modificare le tue preferenze sui cookie in qualsiasi momento utilizzando 
                il pulsante "Impostazioni Cookie" presente nel footer del nostro sito.
              </p>

              <div className="alert alert-warning">
                <strong>Importante:</strong> Se blocchi o elimini i cookie, alcune funzionalità 
                del sito potrebbero non funzionare correttamente. Ad esempio, non sarai in grado 
                di rimanere connesso al tuo account o di completare un acquisto.
              </div>
            </section>

            <section className="mt-5">
              <h2>7. Cookie Flash e Altri Tecnologie Di Tracciamento</h2>
              <p>
                Attualmente non utilizziamo cookie Flash (Local Shared Objects), web beacon, 
                pixel tag o altre tecnologie di tracciamento simili oltre ai cookie HTTP 
                standard descritti in questa policy.
              </p>
            </section>

            <section className="mt-5">
              <h2>8. Cookie su Dispositivi Mobili</h2>
              <p>
                Questa Cookie Policy si applica anche quando accedi al nostro sito tramite 
                dispositivi mobili (smartphone, tablet). I browser mobili offrono opzioni 
                simili ai browser desktop per la gestione dei cookie.
              </p>
            </section>

            <section className="mt-5">
              <h2>9. Modifiche alla Cookie Policy</h2>
              <p>
                Potremmo aggiornare questa Cookie Policy periodicamente per riflettere 
                modifiche alle nostre pratiche o per altri motivi operativi, legali o 
                normativi. Ti consigliamo di rivedere regolarmente questa pagina per rimanere 
                informato su come utilizziamo i cookie.
              </p>
              <p>
                La data dell'ultimo aggiornamento è indicata in cima a questa pagina.
              </p>
            </section>

            <section className="mt-5">
              <h2>10. Ulteriori Informazioni</h2>
              <p>
                Per ulteriori informazioni su come gestiamo i tuoi dati personali, consulta 
                la nostra <a href="/privacy">Privacy Policy</a>.
              </p>
              <p>
                Per informazioni generali sui cookie, puoi visitare:
              </p>
              <ul>
                <li>
                  <a
                    href="https://www.allaboutcookies.org/"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    AllAboutCookies.org
                  </a>
                </li>
                <li>
                  <a
                    href="https://www.youronlinechoices.com/it/"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Your Online Choices
                  </a>
                </li>
              </ul>
            </section>

            <section className="mt-5 mb-5">
              <h2>11. Contatti</h2>
              <p>
                Se hai domande su questa Cookie Policy o sulle nostre pratiche in materia 
                di privacy, puoi contattarci:
              </p>
              <ul>
                <li>Email: privacy@morwalpizshop.it</li>
                <li>
                  Telegram:{' '}
                  <a
                    href="https://t.me/morwalpiz"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    @morwalpiz
                  </a>
                </li>
              </ul>
            </section>
          </div>
        </div>
      </div>
    </>
  );
}