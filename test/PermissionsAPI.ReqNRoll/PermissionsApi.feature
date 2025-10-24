Feature: Permissions API Integration

  # This feature tests integration scenarios across multiple entities:
  # - Default permissions + Group permissions
  # - Group permissions + User permissions
  # - Users in multiple groups
  #
  # Permission calculation hierarchy (later levels override earlier ones):
  # Default → Group → User

  Background:
    # Create the default read permission that all users inherit
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "read",
      "description": "Default read permission for all users",
      "isDefault": true
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "read",
      "isDefault": true
    }
    """

    # Create additional permissions that tests will reference
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "write",
      "description": "Write permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "write",
      "isDefault": false
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "delete",
      "description": "Delete permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "delete",
      "isDefault": false
    }
    """

  Scenario: Check permissions for user with only default permissions
    # WHEN the system calculates permissions for a user with no group memberships or user-level permissions
    # THEN the system SHALL return default permissions
    # WHERE default permissions include "read": true.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create user without any groups or specific permissions
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "groups": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Query permissions for the user
    Given the following request
    """
    GET /api/v1/permissions/user/{{USER_EMAIL}} HTTP/1.1
    """

    # Should return default permissions only
    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "permissions": {
        "read": true
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

  Scenario: Add user to group and check inherited permissions
    # WHEN a group is created with ALLOW permission for a specific action
    # AND a user is created with membership in that group
    # THEN the system SHALL calculate user permissions as default permissions combined with group permissions.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'GROUP_NAME' is set to 'editors-{{GUID()}}'

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
    Content-Type: application/json

    {
      "id": [[GROUP_ID]],
      "name": "{{GROUP_NAME}}"
    }
    """

    # Grant "write" permission to the group
    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "write",
          "access": "ALLOW"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user and add them to the group
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

    # Verify user has both default "read" and group-inherited "write" permissions
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
        "read": true,
        "write": true
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

  Scenario: User-level ALLOW overrides group DENY
    # WHEN a group is granted DENY for a specific permission
    # AND a user is created with membership in that group
    # AND the user is granted ALLOW for the same permission
    # THEN the system SHALL override the group-level DENY with the user-level ALLOW.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'GROUP_NAME' is set to 'restricted-{{GUID()}}'

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

    # Group denies "delete" permission
    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "delete",
          "access": "DENY"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user in the group (will inherit the DENY)
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

    # Grant user-specific ALLOW for "delete" permission
    # This should override the group's DENY
    Given the following request
    """
    PUT /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "delete",
          "access": "ALLOW"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Verify user-level ALLOW has overridden group-level DENY
    # User should have "delete": true despite group having "delete": DENY
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
        "read": true,
        "delete": true
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

  Scenario: User in multiple groups combines permissions
    # WHEN multiple groups are created with different ALLOW permissions
    # AND a user is created with membership in all those groups
    # THEN the system SHALL calculate user permissions as the combination of default permissions and all group permissions.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'EDITORS_GROUP_NAME' is set to 'editors-{{GUID()}}'
    And the variable 'ADMINS_GROUP_NAME' is set to 'admins-{{GUID()}}'

    # Create "editors" group
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{EDITORS_GROUP_NAME}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "id": [[EDITORS_GROUP_ID]]
    }
    """

    # Create "admins" group
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{ADMINS_GROUP_NAME}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "id": [[ADMINS_GROUP_ID]]
    }
    """

    # Grant "write" permission to editors group
    Given the following request
    """
    PUT /api/v1/groups/{{EDITORS_GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "write",
          "access": "ALLOW"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Grant "delete" permission to admins group
    Given the following request
    """
    PUT /api/v1/groups/{{ADMINS_GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "delete",
          "access": "ALLOW"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user belonging to both groups
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "groups": ["{{EDITORS_GROUP_ID}}", "{{ADMINS_GROUP_ID}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Verify user has combined permissions from both groups plus defaults
    # Should have: "read" (default), "write" (from editors), "delete" (from admins)
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
        "read": true,
        "write": true,
        "delete": true
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

    # Cleanup: Delete editors group
    Given the following request
    """
    DELETE /api/v1/groups/{{EDITORS_GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete admins group
    Given the following request
    """
    DELETE /api/v1/groups/{{ADMINS_GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
