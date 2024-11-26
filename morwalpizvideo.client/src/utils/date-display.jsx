export default function DateDisplay({ dateString, className }) {
    // Parse the date string
    console.log(dateString);

    const date = new Date(dateString);
    console.log(date);
    // Format the date as desired
    const formattedDate = date.toLocaleDateString('CET', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });

    return <span className={className}>{formattedDate}</span>
};