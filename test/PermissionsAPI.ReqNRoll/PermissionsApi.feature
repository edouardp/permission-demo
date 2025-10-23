Feature: Permissions API

  Scenario: Get permissions endpoint returns ready message
    Given the following request
    """
    GET /api/permissions HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "message": "Permissions API ready"
    }
    """
