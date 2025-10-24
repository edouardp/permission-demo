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
      "id": [[GAMMA_ID]],
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
      "id": [[ALPHA_ID]],
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
      "id": [[BETA_ID]],
      "name": "{{GROUP_BETA}}"
    }
    """

    # Set conflicting permissions: alpha=ALLOW, beta=DENY, gamma=ALLOW
    Given the following request
    """
    PUT /api/v1/groups/{{ALPHA_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "{{TEST_PERMISSION}}",
          "access": "ALLOW"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    Given the following request
    """
    PUT /api/v1/groups/{{BETA_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "{{TEST_PERMISSION}}",
          "access": "DENY"
        }
      ]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    Given the following request
    """
    PUT /api/v1/groups/{{GAMMA_ID}}/permissions HTTP/1.1
    Content-Type: application/json

    {
      "permissions": [
        {
          "permission": "{{TEST_PERMISSION}}",
          "access": "ALLOW"
        }
      ]
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
      "groups": ["{{GAMMA_ID}}", "{{ALPHA_ID}}", "{{BETA_ID}}"]
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Verify permission is ALLOW (gamma wins as it's last alphabetically)
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

    # Verify debug shows alphabetical processing: alpha (ALLOW) -> beta (DENY) -> gamma (ALLOW)
    Given the following request
    """
    GET /api/v1/user/{{USER_EMAIL}}/debug HTTP/1.1
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
    DELETE /api/v1/groups/{{ALPHA_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{BETA_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{GAMMA_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{TEST_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
