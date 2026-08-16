import { Link } from 'react-router';
import type { NavigationMenuItem } from '@morwalpizvideo/models';

export default function PublicNavigationLink({ item, className }: { item: NavigationMenuItem; className?: string }) {
    if (item.openInNewTab) {
        return <a className={className} href={item.targetUrl} target="_blank" rel="noopener noreferrer">{item.displayText}</a>;
    }
    return <Link className={className} to={item.targetUrl}>{item.displayText}</Link>;
}