Feature: Group Ordering

  Scenario: Groups are evaluated in alphabetical order with last one winning
    # GIVEN a user in multiple groups with conflicting permissions
    # WHEN permissions are calculated
    # THEN groups are processed alphabetically by name, with the last group's setting winning
    
    Given the variable 'TEST_PERMISSION' is set to 'execute:{{GUID()}}'
    Given the variable 'GROUP_ALPHA' is set to 'alpha-{{GUID()}}'
    Given the variable 'GROUP_BETA' is set to 'beta-{{GUID()}}'
    Given the variable 'GROUP_GAMMA' is set to 'gamma-{{GUID()}}'
    Given the variable 'USER_EMAIL' is set to 'user-{{GUID()}}@example.com'
    
    # Create permission
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{TEST_PERMISSION}}",
      "description": "Test permission for ordering",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{TEST_PERMISSION}}",
      "description": "Test permission for ordering"
    }
    """

    # Create groups in non-alphabetical order
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_GAMMA}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{GROUP_GAMMA}}"
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_ALPHA}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{GROUP_ALPHA}}"
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{GROUP_BETA}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{GROUP_BETA}}"
    }
    """

    # Set permissions on groups
    # Alpha: ALLOW (alphabetically first)
    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_ALPHA}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "allow": ["{{TEST_PERMISSION}}"],
      "deny": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Beta: DENY (alphabetically second)
    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_BETA}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "allow": [],
      "deny": ["{{TEST_PERMISSION}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Gamma: ALLOW (alphabetically third - should win)
    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_GAMMA}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "allow": ["{{TEST_PERMISSION}}"],
      "deny": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Create user with groups in non-alphabetical order (gamma, alpha, beta)
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "groups": ["{{GROUP_GAMMA}}", "{{GROUP_ALPHA}}", "{{GROUP_BETA}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Verify that gamma (alphabetically last) wins with ALLOW
    # Processing order should be: alpha (ALLOW) -> beta (DENY) -> gamma (ALLOW)
    # Final result: ALLOW (gamma wins)
    Given the following request
    """
    GET /api/v1/users/{{USER_EMAIL}}/permissions HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "email": "{{USER_EMAIL}}",
      "allow": ["{{TEST_PERMISSION}}"],
      "deny": []
    }
    """

    # Verify debug shows correct processing order
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
          "permission": "{{TEST_PERMISSION}}",
          "finalResult": "ALLOW",
          "chain": [
            {
              "level": "Default",
              "source": "system",
              "action": "NONE"
            },
            {
              "level": "Group",
              "source": "{{GROUP_ALPHA}}",
              "action": "ALLOW"
            },
            {
              "level": "Group",
              "source": "{{GROUP_BETA}}",
              "action": "DENY"
            },
            {
              "level": "Group",
              "source": "{{GROUP_GAMMA}}",
              "action": "ALLOW"
            }
          ]
        }
      ]
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

    # Cleanup: Delete groups
    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_ALPHA}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_BETA}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_GAMMA}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Cleanup: Delete permission
    Given the following request
    """
    DELETE /api/v1/permissions/{{TEST_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
