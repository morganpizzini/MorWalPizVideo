export default function DateDisplay({ dateString, className }) {
    const date = new Date(dateString);
    // Format the date as desired
    const formattedDate = date.toLocaleDateString('CET', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });

    return <span className={className}>{formattedDate}</span>
};