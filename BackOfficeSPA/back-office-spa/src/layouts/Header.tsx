import React, { useState, useEffect } from 'react';
import { Navbar, Nav, Container, Button, Dropdown } from 'react-bootstrap';
import { Link, useNavigate, useLocation } from 'react-router';
import { authService } from '../services/authService';

const Header: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [currentUser, setCurrentUser] = useState<any>(null);

  useEffect(() => {
    // Check authentication status
    const checkAuth = () => {
      const authenticated = authService.isAuthenticated();
      const user = authService.getUser();
      setIsAuthenticated(authenticated);
      setCurrentUser(user);

      // Redirect to login if not authenticated and not already on login page
      if (!authenticated && location.pathname !== '/login') {
        navigate('/login');
      }
    };

    checkAuth();
  }, [location.pathname, navigate]);

  const handleLogout = () => {
    authService.logout();
    setIsAuthenticated(false);
    setCurrentUser(null);
    navigate('/login');
  };

  const handleLogin = () => {
    navigate('/login');
  };

  return (
    <Navbar bg="dark" variant="dark" expand="lg">
      <Container>
        <Navbar.Brand as={Link} to="/">
          BO MorWalPiz
        </Navbar.Brand>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto">

          </Nav>

          {/* Authentication Status in Header */}
          <Nav className="ms-auto">
            {isAuthenticated ? (
              <Dropdown align="end">
                <Dropdown.Toggle variant="outline-light" id="user-dropdown">
                  <span className="text-success me-2">●</span>
                  {currentUser?.username || currentUser?.email || 'User'}
                </Dropdown.Toggle>
                <Dropdown.Menu>
                  <Dropdown.Header>
                    <small className="text-muted">Logged in as:</small><br />
                    <strong>{currentUser?.username || currentUser?.email || 'User'}</strong>
                  </Dropdown.Header>
                  <Dropdown.Divider />
                  <Dropdown.Item onClick={handleLogout}>
                    🚪 Logout
                  </Dropdown.Item>
                </Dropdown.Menu>
              </Dropdown>
            ) : (
              <Button
                variant="outline-light"
                size="sm"
                onClick={handleLogin}
              >
                🔑 Login
              </Button>
            )}
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default Header;
