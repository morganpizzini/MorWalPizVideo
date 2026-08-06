Feature: Videos Management
    As a BackOffice user
    I want to manage YouTube videos and collections
    So that I can curate the content presented on the public site

Background:
    Given the application is running in mock mode

Scenario: Fetch all videos
    When I request all videos
    Then the response should be successful
    And the response should contain a list of videos

Scenario: Get video by ID
    Given a video exists in the system
    When I request the video by its ID
    Then the response should be successful
    And the response should contain the video details

Scenario: Get non-existent video
    When I request a video with ID "nonexistent-video-id"
    Then the response should be Not Found

Scenario: Update an existing video
    Given a video exists in the system
    When I update the video with new title "Updated Video Title"
    Then the response should be No Content

Scenario: Update a non-existent video
    When I update a video with ID "nonexistent-video-id"
    Then the response should be Not Found
