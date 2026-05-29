Feature: Products Management
    As a BackOffice user
    I want to manage products
    So that I can maintain the affiliate catalog exposed to the public site

Background:
    Given the application is running in mock mode

Scenario: Fetch all products
    When I request all products
    Then the response should be successful
    And the response should contain a list of products

Scenario: Get product by ID
    Given a product exists in the system
    When I request the product by its ID
    Then the response should be successful
    And the response should contain the product details

Scenario: Get non-existent product
    When I request a product with ID "nonexistent-product-id"
    Then the response should be Not Found

Scenario: Create a new product without categories
    When I create a product with title "Test Product" and description "Test Description" and url "https://example.com/product"
    Then the response should be No Content

Scenario: Create a product with invalid url
    When I create a product with title "Bad" and description "Bad" and url "not-a-valid-url"
    Then the response should be Bad Request

Scenario: Update an existing product
    Given a product exists in the system
    When I update the product with new title "Updated Product Title"
    Then the response should be No Content
    And the product should have the new title

Scenario: Update a non-existent product
    When I update a product with ID "nonexistent-product-id"
    Then the response should be Bad Request

Scenario: Delete a product
    Given a product exists in the system
    When I delete the product
    Then the response should be No Content

Scenario: Delete a non-existent product
    When I delete a product with ID "nonexistent-product-id"
    Then the response should be Bad Request
