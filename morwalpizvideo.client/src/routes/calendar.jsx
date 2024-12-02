import { useLoaderData } from "react-router-dom"
import './calendar.scss'
import SEO from "@utils/seo";
import { getCalendar } from "@services/calendar";
import DateDisplay from "@utils/date-card";
import ReactGA from "react-ga4"
export async function loader() {
    const calendar = await getCalendar();
    return { calendar };
}

export default function Calendar() {
    const { calendar } = useLoaderData();
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: 'calendar' })
    return (
        <>
            <SEO
                title="calendar"
                description="i miei prossimi impegni"
                imageUrl={`/images/logo-300.jpg`}
                type='article' />
            <h1 className="text-center">CALENDARIO</h1>
            <hr />
            <div className="p-4 bg-secondary">
                {calendar.map((event, i) => (<div key={i} className="card">
                    <div className="card-body d-flex align-items-center">
                        <DateDisplay className="text-muted" dateString={event.date} />
                        <div className="px-2">
                            <span className="fw-bold">{event.title}</span>
                            {event.description?.length > 0 && 
                                <p className="mb-0"><em className="fs-08">{event.title}</em></p>}
                        </div>
                    </div>
                </div>))}
            </div>
        </>
    );
}