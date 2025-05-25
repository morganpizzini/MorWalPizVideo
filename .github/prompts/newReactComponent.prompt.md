The application is a react application written in typescript and use vite as build server.
The routing library is react-router "^7.4.0" which uses 'loaders' and 'actions'

component with the same functionalities can be found in this directory #file:../../BackOfficeSPA/back-office-spa/src/routes/

The component directory should be created in the same directory as the other components in #file:../../BackOfficeSPA/back-office-spa/src/routes/. Named as the entity name in plural form.

## There should be 4 pages to Implement if controller contains crud ops
- index page for with List view, search and pagination
- detail page that Shows details with delete button and a redirect to edit page
- edit page with Form

- each page should be organized in a separate directory with the following structure:
  - Component.tsx - React component defining the UI
  - action.ts - Handles form submissions and API calls
  - loader.ts - Fetches data before rendering components
  - index.ts - Exports the component, action, and loader

- create DTOs for create and update operations

if you need a reference for component's structure, you can check the following directory #file:../../BackOfficeSPA/back-office-spa/src/routes/categories which contains example for create/detail/edit/index pages

## Implementation Guidelines:
- all operation that requires a unique identifier, like get action or delete action should work with entity id property
- Use React Router's data API with loaders and actions
- Implement forms with React Bootstrap components
- Use reusable GenericTable for list views
- Add confirmation modals for destructive actions
- Implement toast notifications for operation feedback
- Add validation for required fields
- Include error handling with dedicated components
- The header of each page should made with PageHeader
- if needed add mock entities in #file:../../BackOfficeSPA/back-office-spa/db.default.json
- In #file:../../BackOfficeSPA/back-office-spa/src/routes/Home.tsx add new Card component (from #file:../../BackOfficeSPA/back-office-spa/src/components/Card') pointing to the new area created

## API endpoint template

${API_CONFIG.BASE_URL}/\[entityname(s)\] where the comes from import { API_CONFIG } from '@config/api';

## Router Configuration Pattern

In #file:../../BackOfficeSPA/back-office-spa/src/router.ts, if CRUD ops configure nested routes following this pattern:
- /\[entityName(s)\] - Main route showing entity list
- /\[entityName\]/create - Route for creating new entities
- /\[entityName\]/:id - Route for viewing entity details
- /\[entityName\]/:id/edit - Route for editing an entity