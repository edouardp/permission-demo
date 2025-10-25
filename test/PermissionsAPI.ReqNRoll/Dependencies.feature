Feature: Dependencies

  Scenario: Permission with no dependencies shows empty lists
    Given the variable 'PERM_NAME' is set to 'unused-{{GUID()}}'
    
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Unused permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Unused permission",
      "isDefault": false
    }
    """

    Given the following request
    """
    GET /api/v1/permissions/{{PERM_NAME}}/dependencies HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "permission": "{{PERM_NAME}}",
      "groups": [],
      "users": []
    }
    """

  Scenario: Permission used by groups shows group dependencies
    Given the variable 'PERM_NAME' is set to 'shared-{{GUID()}}'
    And the variable 'GROUP_A' is set to 'team-a-{{GUID()}}'
    And the variable 'GROUP_B' is set to 'team-b-{{GUID()}}'
    
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Shared permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Shared permission",
      "isDefault": false
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_A}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "id": [[GROUP_A_ID]],
      "name": "{{GROUP_A}}"
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_B}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "id": [[GROUP_B_ID]],
      "name": "{{GROUP_B}}"
    }
    """

    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_A_ID}}/permissions/{{PERM_NAME}} HTTP/1.1
    Content-Type: application/json

    {
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_B_ID}}/permissions/{{PERM_NAME}} HTTP/1.1
    Content-Type: application/json

    {
      "access": "DENY"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    Given the following request
    """
    GET /api/v1/permissions/{{PERM_NAME}}/dependencies HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "permission": "{{PERM_NAME}}",
      "groups": ["{{GROUP_A}}", "{{GROUP_B}}"],
      "users": []
    }
    """

  Scenario: Group with no users shows empty dependencies
    Given the variable 'GROUP_NAME' is set to 'empty-{{GUID()}}'
    
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

    Given the following request
    """
    GET /api/v1/groups/{{GROUP_ID}}/dependencies HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "groupId": "{{GROUP_ID}}",
      "groupName": "{{GROUP_NAME}}",
      "users": []
    }
    """

  Scenario: Dependencies prevent deletion with 409 Conflict
    Given the variable 'PERM_NAME' is set to 'protected-{{GUID()}}'
    And the variable 'GROUP_NAME' is set to 'users-{{GUID()}}'
    
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Protected permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PERM_NAME}}",
      "description": "Protected permission",
      "isDefault": false
    }
    """

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

    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_ID}}/permissions/{{PERM_NAME}} HTTP/1.1
    Content-Type: application/json

    {
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 409 Conflict
    Content-Type: application/problem+json

    {
      "title": "Referential integrity violation",
      "status": 409
    }
    """
