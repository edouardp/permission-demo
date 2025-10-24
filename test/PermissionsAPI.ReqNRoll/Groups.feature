Feature: Groups

  Background:
    # Set up a default "read" permission that applies to all users
    # This creates the baseline permission that group rules can override
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "read",
      "description": "Default read permission",
      "isDefault": true
    }
    """

    # Verify the default permission was created successfully
    # This ensures all users will have read: true unless overridden by group/user rules
    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "read",
      "isDefault": true
    }
    """

  Scenario: Group DENY overrides default ALLOW
    # WHEN a group is granted DENY for a permission that exists in defaults
    # AND a user is created with membership in that group
    # THEN the system SHALL override the default ALLOW with the group-level DENY.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'GROUP_NAME' is set to 'restricted-{{GUID()}}'

    # Create a restricted group
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_NAME}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "id": [[GROUP_ID]]
    }
    """

    # Explicitly DENY "read" permission for this group
    # This should override the default "read": true
    Given the following request
    """
    POST /api/v1/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "read",
          "access": "DENY"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user in the restricted group
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "groups": ["{{GROUP_ID}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Verify that group DENY has overridden the default ALLOW
    # User should have "read": false instead of the default "read": true
    Given the following request
    """
    GET /api/v1/permissions/user/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "permissions": {
        "read": false
      }
    }
    """

    # Cleanup: Delete user
    Given the following request
    """
    DELETE /api/v1/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete group
    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
  Scenario: Setting non-existent permission on group returns error
    # WHEN attempting to set a permission that doesn't exist on a group
    # THEN the system SHALL return an error response
    
    Given the variable 'GROUP_NAME' is set to 'test-group-{{GUID()}}'

    # Create a group
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_NAME}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "id": [[GROUP_ID]]
    }
    """

    # Attempt to set a non-existent permission
    Given the following request
    """
    POST /api/v1/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "nonexistent-permission",
          "access": "ALLOW"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Permissions",
      "status": 400,
      "detail": "The following permissions do not exist: nonexistent-permission"
    }
    """

    # Cleanup: Delete group
    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
