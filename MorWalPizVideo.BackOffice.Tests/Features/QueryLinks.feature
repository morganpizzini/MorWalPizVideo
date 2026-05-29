Feature: QueryLinks Management
    As a BackOffice user
    I want to manage query links
    So that I can attach reusable query-string snippets to shortlinks

Background:
    Given the application is running in mock mode

Scenario: Fetch all query links
    When I request all query links
    Then the response should be successful
    And the response should contain a list of query links

Scenario: Get query link by ID
    Given a query link exists in the system
    When I request the query link by its ID
    Then the response should be successful
    And the response should contain the query link details

Scenario: Get non-existent query link
    When I request a query link with ID "nonexistent-query-id"
    Then the response should be Not Found

Scenario: Create a new query link
    When I create a query link with title "Test QL" and value "list=PLTEST123"
    Then the response should be No Content
    And the query link should appear in the list

Scenario: Create a query link with empty title
    When I create a query link with title "" and value "list=ANY"
    Then the response should be Bad Request

Scenario: Update an existing query link
    Given a query link exists in the system
    When I update the query link with new title "Updated QL Title"
    Then the response should be No Content
    And the query link should have the new title

Scenario: Update a non-existent query link
    When I update a query link with ID "nonexistent-query-id"
    Then the response should be Bad Request

Scenario: Delete a query link
    Given a query link exists in the system
    When I delete the query link
    Then the response should be No Content

Scenario: Delete a non-existent query link
    When I delete a query link with ID "nonexistent-query-id"
    Then the response should be Bad Request
