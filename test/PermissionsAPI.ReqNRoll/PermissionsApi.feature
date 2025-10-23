Feature: Permissions API
  
  # This feature tests a three-level permission system:
  # 1. Default permissions - applied to all users (e.g., "read": true)
  # 2. Group permissions - ALLOW or DENY permissions for groups
  # 3. User permissions - ALLOW or DENY permissions for individual users
  #
  # Permission calculation hierarchy (later levels override earlier ones):
  # Default → Group → User
  #
  # Users are identified by email and can belong to 0 to many groups.

  Scenario: CRUD operations for permissions
    # Tests creating, reading, updating, and deleting permission definitions.
    # Permissions are metadata that define what actions exist in the system.
    
    Given the variable 'PERM_NAME' is set to 'execute-{{GUID()}}'
    
    # Verify permission doesn't exist yet
    Given the following request
    """
    GET /api/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 404 NotFound
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
      "title": "Not Found",
      "status": 404
    }
    """
    
    # Create a new permission
    Given the following request
    """
    POST /api/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Allows execution of scripts"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Allows execution of scripts"
    }
    """

    # Read the permission
    Given the following request
    """
    GET /api/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Allows execution of scripts"
    }
    """

    # Update the permission description
    Given the following request
    """
    PUT /api/permissions/{{PERM_NAME}} HTTP/1.1
    Content-Type: application/json

    {
      "description": "Allows execution of scripts and commands"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Read the updated permission
    Given the following request
    """
    GET /api/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Allows execution of scripts and commands"
    }
    """

    # Delete the permission
    Given the following request
    """
    DELETE /api/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Verify permission is deleted (should return 404)
    Given the following request
    """
    GET /api/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 404 NotFound
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
      "title": "Not Found",
      "status": 404
    }
    """

  Scenario: Check permissions for user with only default permissions
    # Tests that a user who doesn't exist in the system still gets default permissions.
    # This verifies the first level of the permission hierarchy.
    # Expected: User should have "read": true (the default permission)
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Query permissions for a user that hasn't been created yet
    Given the following request
    """
    GET /api/permissions/user/{{USER_EMAIL}} HTTP/1.1
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

  Scenario: Add user to group and check inherited permissions
    # Tests that users inherit permissions from groups they belong to.
    # This verifies the second level of the permission hierarchy.
    # Expected: User should have both default "read" and group-granted "write" permissions.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'GROUP_NAME' is set to 'editors-{{GUID()}}'

    # Create a group
    Given the following request
    """
    POST /api/groups HTTP/1.1
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
    POST /api/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "write",
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user and add them to the group
    Given the following request
    """
    POST /api/users HTTP/1.1
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
    GET /api/permissions/user/{{USER_EMAIL}} HTTP/1.1
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
    DELETE /api/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete group
    Given the following request
    """
    DELETE /api/groups/{{GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: Group DENY overrides default ALLOW
    # Tests that group-level DENY permissions override default ALLOW permissions.
    # This verifies that the group level (level 2) takes precedence over defaults (level 1).
    # Expected: User's default "read": true should be overridden to "read": false by group DENY.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'GROUP_NAME' is set to 'restricted-{{GUID()}}'

    # Create a restricted group
    Given the following request
    """
    POST /api/groups HTTP/1.1
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
    POST /api/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "read",
      "access": "DENY"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user in the restricted group
    Given the following request
    """
    POST /api/users HTTP/1.1
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
    GET /api/permissions/user/{{USER_EMAIL}} HTTP/1.1
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
    DELETE /api/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete group
    Given the following request
    """
    DELETE /api/groups/{{GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: User-level ALLOW overrides group DENY
    # Tests that user-level permissions override group-level permissions.
    # This verifies the third level of the permission hierarchy takes precedence.
    # Expected: User-specific ALLOW should override group DENY.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'GROUP_NAME' is set to 'restricted-{{GUID()}}'

    # Create a group
    Given the following request
    """
    POST /api/groups HTTP/1.1
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
    POST /api/groups/{{GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "delete",
      "access": "DENY"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user in the group (will inherit the DENY)
    Given the following request
    """
    POST /api/users HTTP/1.1
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
    POST /api/users/{{USER_EMAIL}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "delete",
      "access": "ALLOW"
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
    GET /api/permissions/user/{{USER_EMAIL}} HTTP/1.1
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
    DELETE /api/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete group
    Given the following request
    """
    DELETE /api/groups/{{GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: User in multiple groups combines permissions
    # Tests that users in multiple groups receive the combined permissions from all groups.
    # This verifies that group permissions are additive when a user belongs to multiple groups.
    # Expected: User should have permissions from both groups plus defaults.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    And the variable 'EDITORS_GROUP_NAME' is set to 'editors-{{GUID()}}'
    And the variable 'ADMINS_GROUP_NAME' is set to 'admins-{{GUID()}}'

    # Create "editors" group
    Given the following request
    """
    POST /api/groups HTTP/1.1
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
    POST /api/groups HTTP/1.1
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
    POST /api/groups/{{EDITORS_GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "write",
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Grant "delete" permission to admins group
    Given the following request
    """
    POST /api/groups/{{ADMINS_GROUP_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "delete",
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user belonging to both groups
    Given the following request
    """
    POST /api/users HTTP/1.1
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
    GET /api/permissions/user/{{USER_EMAIL}} HTTP/1.1
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
    DELETE /api/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete editors group
    Given the following request
    """
    DELETE /api/groups/{{EDITORS_GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete admins group
    Given the following request
    """
    DELETE /api/groups/{{ADMINS_GROUP_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
