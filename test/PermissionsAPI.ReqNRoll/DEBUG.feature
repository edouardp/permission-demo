Feature: Permission Debug Endpoint

  Background:
    # Create default read permission
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
      "description": "Default read permission"
    }
    """

  Scenario: Debug complex permission chain with defaults, groups, and user overrides
    # Test the permission resolution chain: Default → Group → User
    
    Given the variable 'WRITE_PERMISSION' is set to 'write:{{GUID()}}'
    Given the variable 'DELETE_PERMISSION' is set to 'delete:{{GUID()}}'
    Given the variable 'ADMIN_GROUP' is set to 'admin-{{GUID()}}'
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create write permission (non-default)
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{WRITE_PERMISSION}}",
      "description": "Write permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{WRITE_PERMISSION}}",
      "description": "Write permission"
    }
    """

    # Create delete permission (non-default)
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{DELETE_PERMISSION}}",
      "description": "Delete permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{DELETE_PERMISSION}}",
      "description": "Delete permission"
    }
    """

    # Create admin group
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{ADMIN_GROUP}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{ADMIN_GROUP}}"
    }
    """

    # Set group permissions: ALLOW write, DENY delete
    Given the following request
    """
    PUT /api/v1/groups/{{ADMIN_GROUP}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "allow": ["{{WRITE_PERMISSION}}"],
      "deny": ["{{DELETE_PERMISSION}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user in admin group
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "groups": ["{{ADMIN_GROUP}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Override delete permission at user level to ALLOW
    Given the following request
    """
    PUT /api/v1/users/{{USER_EMAIL}}/permissions/{{DELETE_PERMISSION}} HTTP/1.1
    Content-Type: application/json

    {
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Test debug endpoint - should show complete chain
    Given the following request
    """
    GET /api/v1/users/{{USER_EMAIL}}/debug HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "permissions": [
        {
          "permission": "{{DELETE_PERMISSION}}",
          "finalResult": "ALLOW",
          "chain": [
            {
              "level": "Default",
              "source": "system",
              "action": "NONE"
            },
            {
              "level": "Group",
              "source": "{{ADMIN_GROUP}}",
              "action": "DENY"
            },
            {
              "level": "User",
              "source": "{{USER_EMAIL}}",
              "action": "ALLOW"
            }
          ]
        },
        {
          "permission": "read",
          "finalResult": "ALLOW",
          "chain": [
            {
              "level": "Default",
              "source": "system",
              "action": "ALLOW"
            }
          ]
        },
        {
          "permission": "{{WRITE_PERMISSION}}",
          "finalResult": "ALLOW",
          "chain": [
            {
              "level": "Default",
              "source": "system",
              "action": "NONE"
            },
            {
              "level": "Group",
              "source": "{{ADMIN_GROUP}}",
              "action": "ALLOW"
            }
          ]
        }
      ]
    }
    """

    # Cleanup
    Given the following request
    """
    DELETE /api/v1/users/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{ADMIN_GROUP}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{WRITE_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{DELETE_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
