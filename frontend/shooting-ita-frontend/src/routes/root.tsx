import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import { PepperboxSidebar, PepperboxTopBar, type SidebarNavItem } from '@morwalpiz/layout';
import '../styles/theme.scss';

const PRIMARY_NAV: SidebarNavItem[] = [
    { label: 'Home', to: '/' },
];

const DISCOVER_NAV: SidebarNavItem[] = [
    { label: 'Latest Videos', to: '/latest' },
    { label: 'Exclusives', to: '/exclusives' },
    { label: 'Popular Now', to: '/popular' },
];

export default function RootShell() {
    const navigate = useNavigate();
    const location = useLocation();

    return (
        <div className="pepperbox-shell pepperbox">
            <PepperboxSidebar
                brand="Shooting ITA"
                primaryNav={PRIMARY_NAV}
                discoverNav={DISCOVER_NAV}
                activePath={location.pathname}
                onNavigate={to => navigate(to)}
            />
            <PepperboxTopBar />
            <main className="pepperbox-shell__main">
                <Outlet />
            </main>
        </div>
    );
}
