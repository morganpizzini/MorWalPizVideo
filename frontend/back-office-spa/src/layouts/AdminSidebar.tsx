import { useEffect, useMemo, useState } from 'react';
import { Nav, Offcanvas } from 'react-bootstrap';
import { BarChart3, ChevronDown, Youtube } from 'lucide-react';
import { NavLink, useLocation } from 'react-router';
import {
  adminMenuGroups,
  canAccessMenuItem,
  dashboardMenuItem,
  getAdminMenuGroupForPath,
} from './adminMenu';
import { useChannelContext } from '../contexts/ChannelContext';
import { useAppStore } from '../state/appStore';

interface AdminSidebarProps {
  show: boolean;
  onHide: () => void;
}

export default function AdminSidebar({ show, onHide }: AdminSidebarProps) {
  const permissions = useAppStore(state => state.effectivePermissions);
  const { channels, selectedChannelId, selectChannel, channelSwitchLocked } = useChannelContext();
  const { pathname } = useLocation();
  const visibleGroups = useMemo(
    () =>
      adminMenuGroups
        .map(group => ({
          ...group,
          items: group.items.filter(item => canAccessMenuItem(item, permissions)),
        }))
        .filter(group => group.items.length > 0),
    [permissions]
  );
  const activeGroupId = getAdminMenuGroupForPath(pathname, visibleGroups);
  const defaultGroupId =
    activeGroupId ??
    (pathname === '/' && visibleGroups.some(group => group.id === 'publishing')
      ? 'publishing'
      : null);
  const [expandedGroupId, setExpandedGroupId] = useState<string | null>(defaultGroupId);

  useEffect(() => {
    if (activeGroupId) {
      setExpandedGroupId(activeGroupId);
    } else if (pathname === '/') {
      setExpandedGroupId(defaultGroupId);
    }
  }, [activeGroupId, defaultGroupId, pathname]);

  useEffect(() => {
    if (expandedGroupId && !visibleGroups.some(group => group.id === expandedGroupId)) {
      setExpandedGroupId(null);
    }
  }, [expandedGroupId, visibleGroups]);

  const selected = channels.find(channel => channel.channelId === selectedChannelId);
  const externalLinks = [
    selected?.youTubeStudioUrl
      ? { label: 'YouTube Studio', url: selected.youTubeStudioUrl, Icon: Youtube }
      : null,
    selected?.googleAnalyticsUrl
      ? { label: 'Google Analytics', url: selected.googleAnalyticsUrl, Icon: BarChart3 }
      : null,
  ].filter((link): link is { label: string; url: string; Icon: typeof Youtube } => link !== null);

  const content = (
    <Nav as="nav" aria-label="Admin navigation" className="flex-column gap-1 px-3 pb-3">
      <div className="channel-selector mb-3">
        <label className="sidebar-section-label d-block" htmlFor="channel-selector">
          Channel
        </label>
        <select
          id="channel-selector"
          className="form-select form-select-sm"
          aria-label="Select channel"
          value={selectedChannelId ?? ''}
          onChange={event => selectChannel(event.target.value)}
          disabled={channels.length === 0 || channelSwitchLocked}
        >
          {channels.length === 0 ? <option value="">No accessible channels</option> : null}
          {channels.map(channel => (
            <option key={channel.channelId} value={channel.channelId}>
              {channel.channelName}
            </option>
          ))}
        </select>
      </div>
      {canAccessMenuItem(dashboardMenuItem, permissions) ? (
        <div className="mb-2">
          <Nav.Link as={NavLink} to={dashboardMenuItem.path} end onClick={onHide}>
            <dashboardMenuItem.icon size={17} aria-hidden="true" />
            <span>{dashboardMenuItem.label}</span>
          </Nav.Link>
        </div>
      ) : null}
      {visibleGroups.map(group => {
        const isExpanded = expandedGroupId === group.id;
        const containsCurrentPage = activeGroupId === group.id;
        const groupContentId = `admin-navigation-${group.id}`;
        return (
          <section
            key={group.id}
            className={`admin-sidebar-group${containsCurrentPage ? ' is-current' : ''}`}
          >
            <button
              type="button"
              className="sidebar-group-toggle"
              aria-expanded={isExpanded}
              aria-controls={groupContentId}
              aria-current={containsCurrentPage ? 'location' : undefined}
              onClick={() =>
                setExpandedGroupId(current => (current === group.id ? null : group.id))
              }
            >
              <span>{group.label}</span>
              <ChevronDown size={16} aria-hidden="true" className="sidebar-group-chevron" />
            </button>
            <div id={groupContentId} hidden={!isExpanded}>
              {group.items.map(item => {
                const Icon = item.icon;
                return (
                  <Nav.Link
                    as={NavLink}
                    to={item.path}
                    end={item.path === '/'}
                    key={item.path}
                    onClick={onHide}
                  >
                    <Icon size={17} aria-hidden="true" />
                    <span>{item.label}</span>
                  </Nav.Link>
                );
              })}
            </div>
          </section>
        );
      })}
      {externalLinks.length > 0 ? (
        <div className="mt-auto border-top pt-3 px-3 pb-3">
          <div className="sidebar-section-label">External reports</div>
          {externalLinks.map(({ label, url, Icon }) => (
            <a
              className="nav-link d-flex align-items-center gap-2"
              href={url}
              target="_blank"
              rel="noopener noreferrer"
              key={label}
              onClick={onHide}
            >
              <Icon size={17} aria-hidden="true" />
              <span>{label}</span>
            </a>
          ))}
        </div>
      ) : null}
    </Nav>
  );

  return (
    <Offcanvas
      show={show}
      onHide={onHide}
      responsive="lg"
      className="admin-sidebar admin-sidebar-offcanvas"
    >
      <Offcanvas.Header closeButton>
        <Offcanvas.Title>MorWalPiz Admin</Offcanvas.Title>
      </Offcanvas.Header>
      <Offcanvas.Body className="p-0">{content}</Offcanvas.Body>
    </Offcanvas>
  );
}
