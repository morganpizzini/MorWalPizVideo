import { useState } from 'react';
import { useLoaderData } from 'react-router-dom';
import { voteFaqAnswer, type FaqPublicItem } from '@morwalpizvideo/services';

export default function FaqPage() {
    const items = useLoaderData() as FaqPublicItem[];
    const [message, setMessage] = useState('');
    const vote = async (faqId: string, channelName: string, value: 1 | -1) => {
        try {
            await voteFaqAnswer(faqId, channelName, value);
            setMessage('Il tuo voto è stato registrato.');
        } catch {
            setMessage('Accedi per votare questa risposta.');
        }
    };
    return <section className="faq-page" aria-labelledby="faq-title">
        <header className="faq-page__header"><p className="text-uppercase small mb-2">Knowledge base</p><h1 id="faq-title">Domande frequenti</h1><p>Risposte curate dai canali Shooting ITA.</p></header>
        {message && <p role="status" className="alert alert-info">{message}</p>}
        {items.length === 0 ? <p className="alert alert-secondary">Nessuna risposta pubblicata in questa categoria.</p> : <div className="faq-page__list">
            {items.map(item => <article className="faq-page__item" key={item.id}><p className="small text-uppercase mb-2">{item.categoryName}</p><h2>{item.question}</h2>{item.answers.map(answer => <div className="faq-page__answer" key={`${item.id}-${answer.channelName}`}><h3>{answer.channelName}</h3><p>{answer.content}</p><div className="d-flex gap-2 align-items-center"><span className="small">Utile: {answer.helpfulVotes} | Non utile: {answer.notHelpfulVotes}</span><button className="btn btn-sm btn-outline-success" type="button" onClick={() => vote(item.id, answer.channelName, 1)}>Utile</button><button className="btn btn-sm btn-outline-secondary" type="button" onClick={() => vote(item.id, answer.channelName, -1)}>Non utile</button></div></div>)}</article>)}
        </div>}
    </section>;
}