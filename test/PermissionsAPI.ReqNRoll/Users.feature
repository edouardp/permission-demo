Feature: Users

  Background:
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

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "execute",
      "description": "Execute permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "execute",
      "isDefault": false
    }
    """

  Scenario: User-level ALLOW permission without groups
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

  Scenario: User-level DENY permission overrides default
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

  Scenario: User-level permissions with multiple ALLOW and DENY
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

    # Set multiple permissions in one request
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "write",
          "access": "ALLOW"
        },
        {
          "permission": "delete",
          "access": "ALLOW"
        },
        {
          "permission": "execute",
          "access": "DENY"
        }
      ]
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

  Scenario: Reading permissions for non-existent user returns error
    # WHEN attempting to read permissions for a user that doesn't exist
    # THEN the system SHALL return an error response
    
    Given the variable 'NON_EXISTENT_EMAIL' is set to 'nonexistent-{{GUID()}}@example.com'

    # Attempt to read permissions for user that was never created
    Given the following request
    """
    GET /api/v1/permissions/user/{{NON_EXISTENT_EMAIL}} HTTP/1.1
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
  Scenario: Setting non-existent permission on user returns error
    # WHEN attempting to set a permission that doesn't exist on a user
    # THEN the system SHALL return an error response
    
    Given the variable 'USER_EMAIL' is set to 'test-user-{{GUID()}}@example.com'

    # Create a user
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

    # Attempt to set a non-existent permission
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
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

    # Cleanup: Delete user
    Given the following request
    """
    DELETE /api/v1/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
  Scenario: Setting multiple non-existent permissions returns comprehensive error
    # WHEN attempting to set multiple permissions where some don't exist
    # THEN the system SHALL return all invalid permissions in the error
    
    Given the variable 'USER_EMAIL' is set to 'test-user-{{GUID()}}@example.com'

    # Create a user
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

    # Attempt to set multiple permissions, some valid, some invalid
    Given the following request
    """
    POST /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "write",
          "access": "ALLOW"
        },
        {
          "permission": "invalid-perm-1",
          "access": "ALLOW"
        },
        {
          "permission": "delete",
          "access": "DENY"
        },
        {
          "permission": "invalid-perm-2",
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
      "detail": "The following permissions do not exist: invalid-perm-1, invalid-perm-2"
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
