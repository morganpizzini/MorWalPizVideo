// filepath: c:\Users\morga\OneDrive\Desktop\MorWalPizVideo\BackOfficeSPA\back-office-spa\src\layouts\Sidebar.tsx
import React, { useState, useEffect } from 'react';
import { Link, useLocation } from 'react-router';
import { ListGroup, Collapse, Offcanvas } from 'react-bootstrap';
import { useNavigate } from 'react-router';

interface SidebarProps {
  show: boolean;
  handleClose: () => void;
}

interface MenuItem {
  id: string;
  title: string;
  path: string;
  icon: string;
  children?: MenuItem[];
}

const menuItems: MenuItem[] = [
  {
    id: 'channels',
    title: 'Channels',
    path: '', // Removed path since this item has children
    icon: '📺',
    children: [
      {
        id: 'channels-list',
        title: 'List Channels',
        path: '/channels',
        icon: '📋',
      },
      {
        id: 'channels-create',
        title: 'Add Channel',
        path: '/channels/create',
        icon: '➕',
      },
    ],
  },
  {
    id: 'shortlinks',
    title: 'Short Links',
    path: '', // Removed path since this item has children
    icon: '🔗',
    children: [
      {
        id: 'shortlinks-list',
        title: 'List Short Links',
        path: '/shortlinks',
        icon: '📋',
      },
      {
        id: 'shortlinks-create',
        title: 'Add Short Link',
        path: '/shortlinks/create',
        icon: '➕',
      },
    ],
  },
  {
    id: 'querylinks',
    title: 'Query Links',
    path: '', // Removed path since this item has children
    icon: '🔍',
    children: [
      {
        id: 'querylinks-list',
        title: 'List Query Links',
        path: '/querylinks',
        icon: '📋',
      },
      {
        id: 'querylinks-create',
        title: 'Add Query Link',
        path: '/querylinks/create',
        icon: '➕',
      },
    ],
  },
];

const Sidebar: React.FC<SidebarProps> = ({ show, handleClose }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const [expandedItems, setExpandedItems] = useState<string[]>([]);

  // Auto-expand parent menu when child route is active
  useEffect(() => {
    const activeParent = menuItems.find(item =>
      item.children?.some(child => location.pathname.startsWith(child.path))
    );

    // Only auto-expand when no menu is currently expanded
    if (activeParent && expandedItems.length === 0) {
      setExpandedItems([activeParent.id]);
    }
  }, [location.pathname, expandedItems.length]); // Added expandedItems.length to the dependency array

  // Don't show sidebar on home page
  if (location.pathname === '/') {
    return <></>;
  }

  const toggleExpand = (itemId: string) => {
    setExpandedItems(
      prev =>
        prev.includes(itemId)
          ? [] // Close the current submenu if it's open
          : [itemId] // Only open the clicked submenu, closing any others
    );
  };

  const isActive = (path: string, item?: MenuItem) => {
    // For menu items with children (parent elements), don't mark as active
    if (item && item.children?.length) {
      return false; // Never mark parent items as active
    }
    // For individual menu items or child items, mark as active when the path matches
    return location.pathname === path || location.pathname.startsWith(`${path}/`);
  };

  const handleNavigate = (item: MenuItem, event: React.MouseEvent) => {
    // If the item has children, toggle expansion
    if (item.children && item.children.length > 0) {
      event.preventDefault();
      toggleExpand(item.id);
    } else {
      // Otherwise navigate and close sidebar
      navigate(item.path);
      handleClose();
    }
  };

  return (
    <Offcanvas
      show={show}
      onHide={handleClose}
      placement="start"
      backdrop={false}
      className="sidebar-nav"
    >
      <Offcanvas.Header closeButton>
        <Offcanvas.Title>BO MorWalPiz Navigation</Offcanvas.Title>
      </Offcanvas.Header>
      <Offcanvas.Body>
        <ListGroup variant="flush">
          <ListGroup.Item action as={Link} to="/" onClick={handleClose}>
            🏠 Home
          </ListGroup.Item>

          {menuItems.map(item => (
            <React.Fragment key={item.id}>
              <ListGroup.Item
                action
                as={Link}
                to={item.path}
                active={isActive(item.path, item)}
                className={item.children ? 'd-flex justify-content-between align-items-center' : ''}
                onClick={e => handleNavigate(item, e)}
              >
                <span>
                  {item.icon} {item.title}
                </span>
                {item.children && item.children.length > 0 && (
                  <span className="ms-2">{expandedItems.includes(item.id) ? '▼' : '▶'}</span>
                )}
              </ListGroup.Item>

              {item.children && item.children.length > 0 && (
                <Collapse in={expandedItems.includes(item.id)}>
                  <div>
                    <ListGroup variant="flush">
                      {item.children.map(child => (
                        <ListGroup.Item
                          key={child.id}
                          action
                          as={Link}
                          to={child.path}
                          active={isActive(child.path)}
                          onClick={handleClose}
                          className="ps-4"
                        >
                          {child.icon} {child.title}
                        </ListGroup.Item>
                      ))}
                    </ListGroup>
                  </div>
                </Collapse>
              )}
            </React.Fragment>
          ))}
        </ListGroup>
      </Offcanvas.Body>
    </Offcanvas>
  );
};

export default Sidebar;
