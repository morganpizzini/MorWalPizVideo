import { Link, useLoaderData } from "react-router-dom"
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
            <div className="p-4 bg-light">
                {calendar.map((event, i) => {
                    var cardClass = event.oldEvent ? "bg-secondary" : "";
                    return (<div key={i} className="card">
                        <div className={`card-body d-flex justify-content-between align-items-center ${cardClass}`}>
                            <div className="d-flex align-items-center">
                                <DateDisplay className="text-muted" dateString={event.date} />
                                <div className="px-2">
                                    <span className="fw-bold">{event.title}</span>
                                    {event.description?.length > 0 &&
                                        <p className="mb-0 lh-1"><em className="fs-08">{event.title}</em></p>}
                                </div>
                            </div>
                            <div>
                                {event.matchId &&
                                    <>
                                    {event.matchUrl === event.matchId &&
                                        <Link to={`https://youtu.be/${event.matchUrl}`} target="_blank" rel="noopener noreferrer" className="me-1 pt-1">
                                            <i className="fa-1_8x text-danger fab fa-youtube"></i>
                                        </Link>
                                    }
                                    {event.matchUrl !== event.matchId &&
                                        <Link className={event.oldEvent ? "text-white" : ""} to={`/matches/${event.matchUrl}`}>
                                            Match
                                        </Link>
                                    }
                                    </>
                                }
                            </div>
                        </div>
                    </div>)
                })}
            </div>
        </>
    );
}