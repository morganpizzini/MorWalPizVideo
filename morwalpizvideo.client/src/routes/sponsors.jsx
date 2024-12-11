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
    const [email,setEmail] = useState('');
    const [password,setPassword] = useState('');
    const [token,setToken] = useState('');
    
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
            <fetcher.Form className='w-[30%] mx-auto flex flex-col items-center gap-5' method="post">
                <input type="hidden" name="token" value={token} />
                <label className="input input-bordered flex items-center gap-2 w-full">    
                    <input type="text" name="email" value={email} onChange={e => setEmail(e.target.value)} className="grow" placeholder="Email" />
                    {errors?.email ? <em>{errors.email}</em> : null}
                </label>
                <label className="input input-bordered flex items-center gap-2 w-full">
                    <input type="password" name="password" value={password} onChange={e => setPassword(e.target.value)} className="grow focus:outline-none focus:border-none" placeholder='Password' />
                    {errors?.password ? (
                        <em>{errors.password}</em>
                    ) : null}
                </label>
                This site is protected by reCAPTCHA and the Google
                <a href="https://policies.google.com/privacy">Privacy Policy</a> and
                <a href="https://policies.google.com/terms">Terms of Service</a> apply.
                <button type='submit' className='w-full text-white bg-[#403F3F] py-3 rounded-lg uppercase font-medium'>
                    {busy ? "Saving..." : "Save"}
                </button>
                <p>----</p>
                {result ? (
                    <p>{result.title} updated</p>
                ) : null}
                <p>----</p>
            </fetcher.Form>
            <div className="row text-center">
                {sponsors.map(sponsor => <div key={sponsor.title} className="col-12 col-sm-6 col-md-4 position-relative">
                    <img className="mw-100" src={`images/sponsors/${sponsor.imgSrc}`} />
                    <Link to={sponsor.url} target="_blank" rel="noopener noreferrer" className="stretched-link"></Link>
                </div>)}
            </div>
        </>
    );
}