import { useEffect, useState } from 'react';

export interface SidebarNavItem {
    label: string;
    to: string;
    icon?: string;
}

export interface SidebarFooterLink {
    label: string;
    to: string;
}

export interface PepperboxSidebarProps {
    brand: string;
    primaryNav: SidebarNavItem[];
    discoverNav: SidebarNavItem[];
    helpHref?: string;
    footerLinks?: SidebarFooterLink[];
    activePath: string;
    onNavigate?: (to: string) => void;
}

function SidebarBody({
    brand,
    primaryNav,
    discoverNav,
    helpHref,
    footerLinks,
    activePath,
    onNavigate,
}: PepperboxSidebarProps) {
    const linkClass = (to: string) =>
        `pb-sidebar__link ${activePath === to || (to !== '/' && activePath.startsWith(to)) ? 'is-active' : ''}`;
    const onClick = (to: string) => (ev: React.MouseEvent) => {
        if (onNavigate) {
            ev.preventDefault();
            onNavigate(to);
        }
    };
    return (
        <nav className="pb-sidebar" aria-label="Primary navigation">
            <div className="pb-sidebar__brand">{brand}</div>
            <div>
                {primaryNav.map(item => (
                    <a key={item.to} href={item.to} className={linkClass(item.to)} onClick={onClick(item.to)}>
                        {item.icon ? <span aria-hidden="true">{item.icon}</span> : null}
                        <span>{item.label}</span>
                    </a>
                ))}
            </div>
            <div className="pb-sidebar__section-title">Discover</div>
            <div>
                {discoverNav.map(item => (
                    <a key={item.to} href={item.to} className={linkClass(item.to)} onClick={onClick(item.to)}>
                        {item.icon ? <span aria-hidden="true">{item.icon}</span> : null}
                        <span>{item.label}</span>
                    </a>
                ))}
            </div>
            {helpHref ? (
                <div style={{ marginTop: 'auto' }}>
                    <a href={helpHref} className="pb-sidebar__link">Help</a>
                </div>
            ) : null}
            {footerLinks?.length ? (
                <div style={{ paddingTop: 12, borderTop: '1px solid var(--pb-divider)', display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                    {footerLinks.map(link => (
                        <a key={link.to} href={link.to} className="pb-sidebar__link" style={{ padding: '4px 6px', fontSize: '0.75rem' }}>
                            {link.label}
                        </a>
                    ))}
                </div>
            ) : null}
        </nav>
    );
}

export function PepperboxSidebar(props: PepperboxSidebarProps) {
    const [drawerOpen, setDrawerOpen] = useState(false);

    useEffect(() => {
        // Close drawer on route change
        setDrawerOpen(false);
    }, [props.activePath]);

    return (
        <>
            <aside className="pepperbox-shell__sidebar" data-testid="pb-sidebar">
                <SidebarBody {...props} />
            </aside>
            <button
                type="button"
                className="pb-topbar__btn"
                style={{ display: 'none' }}
                aria-label="Open navigation"
                data-pb-hamburger
                data-testid="pb-hamburger"
                onClick={() => setDrawerOpen(true)}
            >☰</button>
            {drawerOpen ? (
                <div
                    role="dialog"
                    aria-modal="true"
                    data-testid="pb-sidebar-drawer"
                    style={{
                        position: 'fixed', inset: 0, zIndex: 1050,
                        background: 'rgba(0,0,0,0.6)', display: 'flex',
                    }}
                    onClick={() => setDrawerOpen(false)}
                >
                    <div
                        style={{ width: 'var(--pb-sidebar-width)', background: 'var(--pb-bg-elev)', height: '100%' }}
                        onClick={ev => ev.stopPropagation()}
                    >
                        <SidebarBody {...props} />
                    </div>
                </div>
            ) : null}
        </>
    );
}

export default PepperboxSidebar;
