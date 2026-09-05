import { FormEvent, useState } from 'react';
import { Link, useLoaderData } from 'react-router-dom';
import type { ChannelNews } from '@morwalpizvideo/models';
import { subscribeNewsletter } from '@morwalpizvideo/services';
import { useGoogleReCaptcha } from 'react19-google-recaptcha-v3';

export default function ChannelNewsDetail() {
    const item = useLoaderData() as ChannelNews;
    const hero = item.images[0];
    const { executeRecaptcha } = useGoogleReCaptcha();
    const [isOpen, setIsOpen] = useState(false);
    const [email, setEmail] = useState('');
    const [language, setLanguage] = useState('IT');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setIsSubmitting(true);
        setMessage('');
        setError('');
        try {
            const recaptchaToken = executeRecaptcha ? await executeRecaptcha('newsletterSubscribe') : '';
            await subscribeNewsletter({ channelId: item.channelId, email, language, recaptchaToken });
            setMessage('Controlla la posta per completare la richiesta.');
        } catch {
            setError('Impossibile inviare la richiesta. Riprova.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <article className="channel-news-detail">
            <Link to="/" className="btn btn-outline-light mb-3">Back to home</Link>
            <header className="channel-news-detail__header">
                <img src={item.channelLogoUrl || '/images/logo-150.png'} alt={item.channelName} />
                <div>
                    <small>{item.channelName}</small>
                    <h1>{item.title}</h1>
                    {item.subtitle && <p>{item.subtitle}</p>}
                </div>
            </header>
            <section className="channel-news-detail__newsletter" aria-labelledby="newsletter-title">
                <div>
                    <h2 id="newsletter-title">Ricevi gli aggiornamenti del canale</h2>
                    <p>Iscriviti alla newsletter di {item.channelName}.</p>
                </div>
                <button className="btn btn-primary" type="button" aria-expanded={isOpen} aria-controls="channel-news-newsletter-form" onClick={() => setIsOpen(value => !value)}>
                    {isOpen ? 'Chiudi iscrizione' : 'Iscriviti alla newsletter'}
                </button>
                {isOpen && <form id="channel-news-newsletter-form" className="channel-news-detail__newsletter-form" onSubmit={submit}>
                    <label className="form-label" htmlFor="channel-news-newsletter-email">Email</label>
                    <input id="channel-news-newsletter-email" className="form-control" type="email" required value={email} onChange={event => setEmail(event.target.value)} />
                    <label className="form-label mt-3" htmlFor="channel-news-newsletter-language">Lingua</label>
                    <select id="channel-news-newsletter-language" className="form-select" value={language} onChange={event => setLanguage(event.target.value)}>
                        <option value="IT">Italiano</option>
                        <option value="ENG">English</option>
                    </select>
                    <button className="btn btn-primary mt-3" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Invio...' : 'Conferma iscrizione'}</button>
                    <p className="small mt-3 mb-0">Leggi la <a href="/cookie-policy">policy privacy</a> prima di inviare la richiesta.</p>
                    {message && <p className="text-success mt-3 mb-0" role="status">{message}</p>}
                    {error && <p className="text-danger mt-3 mb-0" role="alert">{error}</p>}
                </form>}
            </section>
            {hero && <img className="channel-news-detail__hero" src={hero.publicUrl} alt={hero.altText || item.title} />}
            <div className="channel-news-detail__body" dangerouslySetInnerHTML={{ __html: item.descriptionHtml }} />
        </article>
    );
}