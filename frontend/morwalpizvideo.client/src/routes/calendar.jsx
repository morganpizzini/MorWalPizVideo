import { Link, useLoaderData } from "react-router"
import './calendar.scss'
import SEO from "@utils/seo";
import DateDisplay from "@utils/date-card";
import ReactGA from "react-ga4"

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
            <h1 className="text-center mb-4">CALENDARIO</h1>
            <hr />
            <div className="calendar-container p-4">
                {calendar.map((event, i) => {
                    const isOldEvent = event.oldEvent;
                    return (
                        <div
                            key={i}
                            className={`calendar-event-card card mb-3 ${isOldEvent ? 'old-event' : 'upcoming-event'}`}
                        >
                            <div className="card-body">
                                <div className="d-flex flex-column flex-md-row justify-content-between">
                                    {/* Left section: Date and Event Info */}
                                    <div className="d-flex align-items-start flex-grow-1">
                                        <div className="event-date-display me-3">
                                            <DateDisplay className="text-muted" dateString={event.date} />
                                        </div>
                                        <div className="event-details flex-grow-1">
                                            <h5 className={`event-title mb-2 ${isOldEvent ? 'text-muted' : ''}`}>
                                                {event.title}
                                            </h5>
                                            {event.description?.length > 0 && (
                                                <p className={`event-description mb-2 ${isOldEvent ? 'text-muted' : ''}`}>
                                                    <em>{event.description}</em>
                                                </p>
                                            )}
                                            {/* Categories Display */}
                                            {event.categories && event.categories.length > 0 && (
                                                <div className="event-categories mt-2">
                                                    {event.categories.map((category) => (
                                                        <span
                                                            key={category.id}
                                                            className={`badge category-badge ${isOldEvent ? 'bg-secondary' : 'bg-primary'} me-2 mb-1`}
                                                        >
                                                            {category.title}
                                                        </span>
                                                    ))}
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Right section: Match Link */}
                                    {event.matchId && event.matchUrl && (
                                        <div className="event-actions ms-md-3 mt-3 mt-md-0 d-flex align-items-start">
                                            {event.matchUrl === event.matchId ? (
                                                <Link
                                                    to={`https://youtu.be/${event.matchUrl}`}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    className="btn btn-link p-0 youtube-link"
                                                    title="Guarda su YouTube"
                                                >
                                                    <i className="fab fa-youtube youtube-icon"></i>
                                                </Link>
                                            ) : (
                                                <Link
                                                    className={`btn btn-sm ${isOldEvent ? 'btn-outline-secondary' : 'btn-outline-primary'} match-link`}
                                                    to={`/matches/${event.matchUrl}`}
                                                >
                                                    <i className="fas fa-info-circle me-1"></i>
                                                    Match
                                                </Link>
                                            )}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>
                    )
                })}
            </div>
        </>
    );
}
