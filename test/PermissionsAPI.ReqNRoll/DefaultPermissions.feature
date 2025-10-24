Feature: Default Permissions

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

  Scenario: Set permission as default and verify it applies to all users
    # WHEN a permission is created with isDefault set to true
    # THEN the system SHALL apply that permission to all users by default.
    
    Given the variable 'PERM_NAME' is set to 'list-{{GUID()}}'
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create a new permission as default
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "List items permission",
      "isDefault": true
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "isDefault": true
    }
    """

    # Create user to test default permissions
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

    # Verify user gets both read (from Background) and new default permission
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
        "{{PERM_NAME}}": true
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

    # Cleanup
    Given the following request
    """
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: Toggle permission default status
    # WHEN a permission's default status is changed
    # THEN the system SHALL update whether it applies to all users.
    
    Given the variable 'PERM_NAME' is set to 'write-{{GUID()}}'
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'

    # Create permission as non-default
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Write permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "name": "{{PERM_NAME}}",
      "isDefault": false
    }
    """

    # Create user to test permissions
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

    # Verify user does NOT have this permission
    Given the following request
    """
    GET /api/v1/permissions/user/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK

    {
      "email": "{{USER_EMAIL}}",
      "permissions": {
        "read": true
      }
    }
    """

    # Set permission as default
    Given the following request
    """
    PUT /api/v1/permissions/{{PERM_NAME}}/default HTTP/1.1
    Content-Type: application/json

    true
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Verify user now has this permission
    Given the following request
    """
    GET /api/v1/permissions/user/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK

    {
      "email": "{{USER_EMAIL}}",
      "permissions": {
        "read": true,
        "{{PERM_NAME}}": true
      }
    }
    """

    # Unset as default
    Given the following request
    """
    PUT /api/v1/permissions/{{PERM_NAME}}/default HTTP/1.1
    Content-Type: application/json

    false
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Verify user no longer has this permission
    Given the following request
    """
    GET /api/v1/permissions/user/{{USER_EMAIL}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK

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

    # Cleanup: Delete permission
    Given the following request
    """
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
