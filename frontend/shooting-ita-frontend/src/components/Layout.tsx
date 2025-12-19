import React from 'react';
import { Outlet } from 'react-router-dom';
import { Container, Navbar, Nav, Offcanvas, Image } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faBars } from '@fortawesome/free-solid-svg-icons';
import Jumbotron from './Jumbotron'; // Assuming Jumbotron component exists

const Layout: React.FC = () => {
  const [showOffcanvas, setShowOffcanvas] = React.useState(false);

  const handleClose = () => setShowOffcanvas(false);
  const handleShow = () => setShowOffcanvas(true);

  return (
    <>
      {/* Navigation Bar */}
      <Navbar bg="light" expand={false} className="mb-3 d-lg-none fixed-top">
        <Container fluid>
          <Navbar.Brand href="/">Shooting ITA</Navbar.Brand>
          <Navbar.Toggle aria-controls="offcanvasNavbar" onClick={handleShow}>
            <FontAwesomeIcon icon={faBars} />
          </Navbar.Toggle>
          <Navbar.Offcanvas
            id="offcanvasNavbar"
            aria-labelledby="offcanvasNavbarLabel"
            placement="end"
            show={showOffcanvas}
            onHide={handleClose}
          >
            <Offcanvas.Header closeButton>
              <Offcanvas.Title id="offcanvasNavbarLabel">Menu</Offcanvas.Title>
            </Offcanvas.Header>
            <Offcanvas.Body>
              <Nav className="justify-content-end flex-grow-1 pe-3">
                <Nav.Link href="/" onClick={handleClose}>Home</Nav.Link>
                <Nav.Link href="/request-video" onClick={handleClose}>Request Video</Nav.Link>
                <Nav.Link href="/request-ad" onClick={handleClose}>Request Ad</Nav.Link>
                {/* Add other links as needed */}
              </Nav>
            </Offcanvas.Body>
          </Navbar.Offcanvas>
        </Container>
      </Navbar>
      {/* Jumbotron Header */}
      <Jumbotron className="d-none d-lg-flex" backgroundImageUrl="https://placehold.co/600x400" />
      {/* Main Content Area */}
      <Container className="mt-lg-0 pt-5 pt-lg-0 mt-4">
        <Outlet /> {/* Page content will be rendered here */}
      </Container>
    </>
  );
};

export default Layout;
