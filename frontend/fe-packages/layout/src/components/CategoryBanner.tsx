export interface CategoryBannerProps {
    title: string;
    artworkUrl?: string;
    className?: string;
}

export function CategoryBanner({ title, artworkUrl, className }: CategoryBannerProps) {
    return (
        <header
            className={`pb-category-banner ${className ?? ''}`}
            data-testid="pb-category-banner"
            style={artworkUrl ? { backgroundImage: `linear-gradient(180deg, rgba(0,0,0,0.2), rgba(0,0,0,0.7)), url(${artworkUrl})` } : undefined}
        >
            <h1 className="pb-category-banner__title">{title}</h1>
        </header>
    );
}

export default CategoryBanner;
