Feature: Permissions

  Scenario: CRUD operations for permissions
    # WHEN a permission is created with a name and description
    # THEN the system SHALL store the permission
    # AND the system SHALL allow retrieval of the permission by name
    # AND the system SHALL allow updating the permission description
    # AND the system SHALL allow deletion of the permission
    # AND the system SHALL return 404 when retrieving a deleted permission.
    
    Given the variable 'EXECUTE_PERMISSION' is set to 'execute:{{GUID()}}'
    
    # Verify permission doesn't exist yet
    Given the following request
    """
    GET /api/v1/permissions/{{EXECUTE_PERMISSION}} HTTP/1.1
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
      "name": "{{EXECUTE_PERMISSION}}",
      "description": "Allows execution of scripts",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{EXECUTE_PERMISSION}}",
      "description": "Allows execution of scripts"
    }
    """

    # Read the permission
    Given the following request
    """
    GET /api/v1/permissions/{{EXECUTE_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "name": "{{EXECUTE_PERMISSION}}",
      "description": "Allows execution of scripts"
    }
    """

    # Update the permission description
    Given the following request
    """
    PUT /api/v1/permissions/{{EXECUTE_PERMISSION}} HTTP/1.1
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
    GET /api/v1/permissions/{{EXECUTE_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "name": "{{EXECUTE_PERMISSION}}",
      "description": "Allows execution of scripts and commands"
    }
    """

    # Delete the permission
    Given the following request
    """
    DELETE /api/v1/permissions/{{EXECUTE_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Verify permission is deleted (should return 404)
    Given the following request
    """
    GET /api/v1/permissions/{{EXECUTE_PERMISSION}} HTTP/1.1
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
