Feature: Dependencies
  As an API consumer
  I want to check what dependencies prevent deletion of entities
  So that I can understand what needs to be cleaned up before deletion

  Scenario: Permission with no dependencies shows empty lists
    # WHEN a permission is created but not assigned to any groups or users
    # THEN the dependencies endpoint SHALL return empty lists for both groups and users
    
    Given the variable 'PERM_NAME' is set to 'unused-{{GUID()}}'
    
    # Create a permission that won't be used anywhere
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

    # Check dependencies - should be empty
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

    # Cleanup - delete the permission
    Given the following request
    """
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: Permission used by groups shows group dependencies
    # WHEN a permission is assigned to multiple groups
    # THEN the dependencies endpoint SHALL return the list of group names (alphabetically sorted)
    
    Given the variable 'PERM_NAME' is set to 'shared-{{GUID()}}'
    And the variable 'GROUP_A' is set to 'team-a-{{GUID()}}'
    And the variable 'GROUP_B' is set to 'team-b-{{GUID()}}'
    
    # Create a permission
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

    # Create first group
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

    # Create second group
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

    # Assign permission to first group with ALLOW
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

    # Assign permission to second group with DENY
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

    # Check dependencies - should show both groups (alphabetically sorted)
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

    # Cleanup - delete groups first, then permission
    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_A_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_B_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: Group with no users shows empty dependencies
    # WHEN a group is created but has no users assigned
    # THEN the dependencies endpoint SHALL return an empty users list
    
    Given the variable 'GROUP_NAME' is set to 'empty-{{GUID()}}'
    
    # Create a group with no users
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
      "name": "{{GROUP_NAME}}"
    }
    """

    # Check dependencies - should show empty users list
    Given the following request
    """
    GET /api/v1/groups/{{GROUP_NAME}}/dependencies HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "groupName": "{{GROUP_NAME}}",
      "users": []
    }
    """

    # Cleanup - delete the group
    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: Dependencies prevent deletion with 409 Conflict
    # WHEN attempting to delete a permission that is assigned to a group
    # THEN the API SHALL return 409 Conflict with referential integrity violation details
    
    Given the variable 'PERM_NAME' is set to 'protected-{{GUID()}}'
    And the variable 'GROUP_NAME' is set to 'users-{{GUID()}}'
    
    # Create a permission
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

    # Create a group
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
      "name": "{{GROUP_NAME}}"
    }
    """

    # Assign permission to the group
    Given the following request
    """
    PUT /api/v1/groups/{{GROUP_NAME}}/permissions/{{PERM_NAME}} HTTP/1.1
    Content-Type: application/json

    {
      "access": "ALLOW"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Attempt to delete permission - should fail with 409 Conflict
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

    # Cleanup - remove permission from group first, then delete both
    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_NAME}}/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{GROUP_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{PERM_NAME}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """
