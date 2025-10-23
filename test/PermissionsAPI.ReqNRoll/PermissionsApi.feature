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


  # --------------------------------------------------------------------------
  #
  Scenario: CRUD operations for permissions

    # Requirements:
    #
    # WHEN a permission is created with a name and description
    # THEN the system SHALL store the permission
    # AND the system SHALL allow retrieval of the permission by name
    # AND the system SHALL allow updating the permission description
    # AND the system SHALL allow deletion of the permission
    # AND the system SHALL return 404 when retrieving a deleted permission.
    
    Given the variable 'PERM_NAME' is set to 'execute-{{GUID()}}'
    
    # Verify permission doesn't exist yet
    Given the following request
    """
    GET /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
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
    POST /api/v1/permissions HTTP/1.1
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
    GET /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
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
    PUT /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
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
    GET /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
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
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Verify permission is deleted (should return 404)
    Given the following request
    """
    GET /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
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


  # --------------------------------------------------------------------------
  #
  Scenario: Check permissions for user with only default permissions

    # Requirements:
    #
    # WHEN the system calculates permissions for a user that does not exist
    # THEN the system SHALL return default permissions
    # WHERE default permissions include "read": true.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Query permissions for a user that hasn't been created yet
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

  # --------------------------------------------------------------------------
  #
  Scenario: User-level ALLOW permission without groups

    # Requirements:
    #
    # WHEN a user is created without group membership
    # AND the user is granted ALLOW for a specific permission
    # THEN the system SHALL calculate permissions as default permissions plus user-level ALLOW permissions.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create user without any groups
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

    # Grant user-specific ALLOW for "write" permission
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
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

    # Verify user has default "read" plus user-level "write"
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

  # --------------------------------------------------------------------------
  #
  Scenario: User-level DENY permission overrides default

    # Requirements:
    #
    # WHEN a user is created without group membership
    # AND the user is granted DENY for a permission that exists in defaults
    # THEN the system SHALL override the default ALLOW with the user-level DENY.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create user without any groups
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

    # Explicitly DENY "read" permission for this user
    # This should override the default "read": true
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
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

    # Verify user-level DENY has overridden the default ALLOW
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

  # --------------------------------------------------------------------------
  #
  Scenario: User-level permissions with multiple ALLOW and DENY

    # Requirements:
    #
    # WHEN a user is granted multiple user-level permissions
    # WHERE some permissions are ALLOW and some are DENY
    # THEN the system SHALL apply each user-level permission independently
    # AND the system SHALL combine them with default permissions.
    
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create user without any groups
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

    # ALLOW write permission
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
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

    # ALLOW delete permission
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
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

    # DENY execute permission
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permission": "execute",
      "access": "DENY"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Verify all user-level permissions are applied
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
        "delete": true,
        "execute": false
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


  # --------------------------------------------------------------------------
  #
  Scenario: Add user to group and check inherited permissions

    # Requirements:
    #
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
    POST /api/v1/groups/{{GROUP_ID}}/permissions HTTP/1.1
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


  # --------------------------------------------------------------------------
  #
  Scenario: Group DENY overrides default ALLOW

    # Requirements:
    #
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


  # --------------------------------------------------------------------------
  #
  Scenario: User-level ALLOW overrides group DENY

    # Requirements:
    #
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
    POST /api/v1/groups/{{GROUP_ID}}/permissions HTTP/1.1
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
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
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


  # --------------------------------------------------------------------------
  #
  Scenario: User in multiple groups combines permissions

    # Requirements:
    #
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
    POST /api/v1/groups/{{EDITORS_GROUP_ID}}/permissions HTTP/1.1
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
    POST /api/v1/groups/{{ADMINS_GROUP_ID}}/permissions HTTP/1.1
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
