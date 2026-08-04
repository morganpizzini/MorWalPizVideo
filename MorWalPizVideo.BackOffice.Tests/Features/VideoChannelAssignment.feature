Feature: Video Channel Assignment
    As a BackOffice user
    I want to assign a YouTube video to a specific channel
    So that the video-to-channel ownership is consistent in the embedded YTChannel.Videos list

Background:
    Given the application is running in mock mode

Scenario: Assign an existing video to a known channel
    Given a video exists in the system
    And a channel exists in the system
    When I assign the video to the channel
    Then the response should be successful
    And the channel should contain the video

Scenario: Assigning the same video twice is idempotent
    Given a video exists in the system
    And a channel exists in the system
    When I assign the video to the channel
    And I assign the video to the channel again
    Then the response should be successful
    And the channel should contain the video exactly once

Scenario: Assigning to an unknown channel returns 400
    Given a video exists in the system
    When I assign the video to a channel with ID "nonexistent-channel-id"
    Then the response should be Bad Request

Scenario: Assigning with empty channel ID returns 400
    Given a video exists in the system
    When I assign the video to a channel with ID ""
    Then the response should be Bad Request

Scenario: Assigning an unknown video returns 404
    Given a channel exists in the system
    When I assign video with ID "nonexistent-video-id" to the channel
    Then the response should be Not Found

Scenario: Reassigning a video moves it between channels
    Given a video exists on a source channel
    And a different target channel exists
    When I assign the video to the target channel
    Then the response should be successful
    And the target channel should contain the video
    And the source channel should contain the video
