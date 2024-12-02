import './date-card.scss'

export default function DateDisplay({ dateString, className }) {
    const date = new Date(dateString);
    // Get the abbreviated month
    const month = new Intl.DateTimeFormat('en-US', { month: 'short' }).format(date);
    const day = date.getDate();
    const year = date.getFullYear();
    return (<div className={`date-container ${className}`}>
        <span className="month">{month}</span>
        <span className="day">{day}</span>
        <span className="year">{year}</span>
    </div>)
};