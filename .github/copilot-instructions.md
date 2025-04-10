When I ask to create a new react component based on entity or an existing controller in "BackOfficeSPA\back-office-spa\src\routes\" check for similar component in the same directory. basically the rules are the following

Key Components to Implement if controller do crud ops
EntityList - List view with search and pagination
EntityDetail - Shows details with edit and delete options
CreateEntity - Form for creating entities
EditEntity - Form for editing entities

each CRUD operation or controller action should be a sub directory with
Component.tsx - React component defining the UI
action.ts - Handles form submissions and API calls
loader.ts - Fetches data before rendering components
index.ts - Exports the component, action, and loader

The you should create DTOs for create and update operations

Implementation Guidelines
Use React Router's data API with loaders and actions
Implement forms with React Bootstrap components
Use reusable GenericTable for list views
Add confirmation modals for destructive actions
Implement toast notifications for operation feedback
Add validation for required fields
Include error handling with dedicated components
The header of each page should made with PageHeader
In homepage there should be a Card component pointing to the new area created

Router Configuration Pattern
In router.ts, if CRUD ops configure nested routes following this pattern:
/entityName(s) - Main route showing entity list
/entityName/create - Route for creating new entities
/entityName/:id - Route for viewing entity details
/entityName/:id/edit - Route for editing an entity