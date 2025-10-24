Feature: Input Validation

  Scenario: Invalid group names are rejected
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "admin users"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Group Name",
      "status": 400,
      "detail": "Group name must contain only alphanumeric characters and hyphens (A-Za-z0-9-). Cannot start or end with -."
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "-admin"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Group Name",
      "status": 400
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": ""
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Group Name",
      "status": 400
    }
    """

  Scenario: Invalid user emails are rejected
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "notanemail",
      "groups": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Email",
      "status": 400,
      "detail": "Email must be a valid format (e.g., user@company.com)."
    }
    """

    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "user @example.com",
      "groups": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Email",
      "status": 400
    }
    """

    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "",
      "groups": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/problem+json

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Invalid Email",
      "status": 400
    }
    """
