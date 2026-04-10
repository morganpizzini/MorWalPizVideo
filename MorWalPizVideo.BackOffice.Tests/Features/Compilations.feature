Feature: Compilations Management
    As a BackOffice user
    I want to manage video compilations
    So that I can organize and group related videos together

Background:
    Given the application is running in mock mode

Scenario: Fetch all compilations
    When I request all compilations
    Then the response should be successful
    And the response should contain a list of compilations

Scenario: Get compilation by ID
    Given a compilation exists in the system
    When I request the compilation by its ID
    Then the response should be successful
    And the response should contain the compilation details

Scenario: Get non-existent compilation
    When I request a compilation with ID "nonexistent-id"
    Then the response should be Not Found
    And the response should contain message "not found"

Scenario: Create a new compilation without videos
    When I create a compilation with title "Test Compilation" and description "Test Description" and url "test-url"
    Then the response should be Created
    And the response should contain the compilation ID

Scenario: Create a compilation with invalid data
    When I create a compilation with empty title
    Then the response should be Bad Request

Scenario: Update an existing compilation
    Given a compilation exists in the system
    When I update the compilation with new title "Updated Title"
    Then the response should be No Content

Scenario: Update a non-existent compilation
    When I update a compilation with ID "nonexistent-id"
    Then the response should be Not Found

Scenario: Delete a compilation
    Given a compilation exists in the system
    When I delete the compilation
    Then the response should be No Content

Scenario: Delete a non-existent compilation
    When I delete a compilation with ID "nonexistent-id"
    Then the response should be Not Found