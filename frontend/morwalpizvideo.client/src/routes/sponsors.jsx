import { useState, useEffect, useCallback } from "react"
import { Link, useLoaderData } from "react-router";
import {
    useGoogleReCaptcha
} from 'react-google-recaptcha-v3';
import { useFetcher } from "react-router";

export default function Sponsors() {
    const { sponsors } = useLoaderData();
    let fetcher = useFetcher();
    let busy = fetcher.state !== "idle";
    let errors = fetcher.data?.errors;
    let result = fetcher.data != undefined && (fetcher.data.errors == undefined || fetcher.data.errors.length == 0) ? fetcher.data : null;

    const { executeRecaptcha } = useGoogleReCaptcha();
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [description, setDescription] = useState('');
    const [token, setToken] = useState('');

    const handleReCaptchaVerify = useCallback(async () => {
        if (!executeRecaptcha) {
            return;
        }
        const token = await executeRecaptcha('sponsorForm');
        setToken(token);
    }, [executeRecaptcha]);

    useEffect(() => {
        handleReCaptchaVerify();
    }, [handleReCaptchaVerify]);
    return (
        <>
            <h1 className="text-center mb-3">SPONSORS</h1>
            <div className="row text-center mb-5">
                {sponsors.map(sponsor => <div key={sponsor.title} className="col-12 col-sm-6 col-md-4 position-relative">
                    <img className="mw-100" src={sponsor.imgSrc} />
                    <Link to={sponsor.url} target="_blank" rel="noopener noreferrer" className="stretched-link"></Link>
                </div>)}
            </div>
            <div className="row">
                <div className="col-12 offset-md-8 col-md-4 bg-light p-3">
                    <h4 className="mb-0">Vuoi aiutarmi anche tu in questa avventura?</h4>
                    <p className="fs-08 mb-1">Lascia alcune informazioni base e verrai ricontattato, grazie!</p>
                    <fetcher.Form className='w-[30%] mx-auto flex flex-col items-center gap-5' method="post">
                        <input type="hidden" name="token" value={token} />
                        <div className="form-group">
                            <label className="form-label fw-bold">Nome</label>
                            <input type="text" name="name" value={name} onChange={e => setName(e.target.value)} className="form-control" placeholder="Nome" />
                            {errors?.email ? <em className="text-danger">{errors.name}</em> : null}
                        </div>
                        <div className="form-group">
                            <label className="form-label fw-bold">Email</label>
                            <input type="mail" name="email" value={email} onChange={e => setEmail(e.target.value)} className="form-control" placeholder="Email" />
                            {errors?.email ? <em className="text-danger">{errors.email}</em> : null}
                        </div>
                        <div className="form-group">
                            <label className="form-label fw-bold">Descrizione</label>
                            <textarea className="form-control" rows="5" name="description" value={description} onChange={e => setDescription(e.target.value)} placeholder='Breve descrizione'></textarea>
                            {errors?.description ? (
                                <em className="text-danger">{errors.description}</em>
                            ) : null}
                        </div>
                        <div className="form-group fs-08 mt-1">
                            Sito protetto da reCAPTCHA con Google
                            {/*This site is protected by reCAPTCHA and the Google*/}
                            &nbsp;<a href="https://policies.google.com/privacy">Privacy Policy</a> and <a href="https://policies.google.com/terms">Terms of Service</a>.
                        </div>
                        <div className="d-flex justify-content-between">
                            {result ? (
                                <p className="text-success m-0 mt-1">{result.title}</p>
                            ) : <p></p>}
                            <div className="text-end">
                                <button type='submit' className='btn btn-secondary'>
                                    {busy ? "Saving..." : "Save"}
                                </button>
                            </div>
                        </div>
                    </fetcher.Form>
                </div>
            </div>
        </>
    );
}