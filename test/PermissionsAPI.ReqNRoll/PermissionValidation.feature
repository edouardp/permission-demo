Feature: Permission Name Validation

  Scenario: Valid permission names are accepted
    # WHEN creating permissions with valid names (alphanumeric, hyphens, and colons)
    # THEN the system SHALL accept them
    
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "read",
      "description": "Read permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "read",
      "description": "Read permission"
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "tenant:read",
      "description": "Tenant read permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "tenant:read",
      "description": "Tenant read permission"
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "service:api:execute",
      "description": "Service API execute permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "service:api:execute",
      "description": "Service API execute permission"
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "read-write",
      "description": "Read-write permission with hyphen",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "read-write",
      "description": "Read-write permission with hyphen"
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "Read123",
      "description": "Mixed case with numbers",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "Read123",
      "description": "Mixed case with numbers"
    }
    """

  Scenario: Invalid permission names are rejected
    # WHEN creating permissions with invalid names
    # THEN the system SHALL reject them with validation error
    
    # Test spaces
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "read write",
      "description": "Invalid space permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Permission Name",
      "status": 400,
      "detail": "Permission name must contain only alphanumeric characters, hyphens, and colons (A-Za-z0-9:-). Cannot start or end with : or -. Cannot contain consecutive colons. Cannot have - adjacent to :."
    }
    """

    # Test special characters
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "read@write",
      "description": "Invalid special character permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Permission Name",
      "status": 400,
      "detail": "Permission name must contain only alphanumeric characters, hyphens, and colons (A-Za-z0-9:-). Cannot start or end with : or -. Cannot contain consecutive colons. Cannot have - adjacent to :."
    }
    """

    # Test empty name
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "",
      "description": "Empty name permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Permission Name",
      "status": 400,
      "detail": "Permission name must contain only alphanumeric characters, hyphens, and colons (A-Za-z0-9:-). Cannot start or end with : or -. Cannot contain consecutive colons. Cannot have - adjacent to :."
    }
    """
