Feature: ShortLinks Management
    As a BackOffice user
    I want to manage short links
    So that I can create and track shortened URLs for content

Background:
    Given the application is running in mock mode

Scenario: Fetch all short links
    When I request all short links
    Then the response should be successful
    And the response should contain a list of short links

Scenario: Get short link by code
    Given a short link with code "test1" exists
    When I request the short link with code "test1"
    Then the response should be successful
    And the response should contain the short link details

Scenario: Get non-existent short link
    When I request the short link with code "nonexistent"
    Then the response should be Not Found
    And the response should contain message "No shortlink found for this video"

Scenario: Create a new standalone short link
    When I create a short link with target "https://example.com" and link type "Other"
    Then the response should be successful
    And the response should contain a short link URL

Scenario: Delete a short link
    Given a standalone short link exists
    When I delete the short link
    Then the response should be No Content

Scenario: Fetch includes short links from matches
    When I request all short links
    Then the response should be successful
    And the response should contain short links from matches

Scenario: Fetch includes short links from channels
    When I request all short links
    Then the response should be successful
    And the response should contain short links from channels

Scenario: Get embedded short link from match by code
    Given a match with embedded short link exists
    When I request the short link by its code
    Then the response should be successful
    And the response should contain the short link details

Scenario: Get embedded short link from channel by code
    Given a channel with embedded short link exists
    When I request the short link by its code
    Then the response should be successful
    And the response should contain the short link details

Scenario: Update embedded short link in match
    Given a match with embedded short link exists
    When I update the embedded short link
    Then the response should be successful
    And the embedded short link should be updated in the match

Scenario: Delete embedded short link from match
    Given a match with embedded short link exists
    When I delete the embedded short link by code
    Then the response should be No Content
    And the short link should be removed from the match

Scenario: Delete embedded short link from channel
    Given a channel with embedded short link exists
    When I delete the embedded short link by code
    Then the response should be No Content
    And the short link should be removed from the channel

Scenario: Create short link for match sets correct LinkType
    Given a match exists for short link creation
    When I create a short link for the match
    Then the response should be successful
    And the short link should have LinkType YouTubeVideo

Scenario: Create short link for channel sets correct LinkType
    Given a channel exists for short link creation
    When I create a short link for the channel
    Then the response should be successful
    And the short link should have LinkType YouTubeChannel
