Feature: History Tracking

  Scenario: History records all changes with timestamps and entity details
    # WHEN entities are created, updated, and deleted
    # THEN the system SHALL record each change with UTC timestamp, change type, and entity state
    
    Given the variable 'TEST_PERMISSION' is set to 'test-{{GUID()}}'
    Given the variable 'TEST_GROUP' is set to 'group-{{GUID()}}'
    Given the variable 'TEST_USER' is set to 'user-{{GUID()}}@example.com'

    # Create a permission - should be recorded in history
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{TEST_PERMISSION}}",
      "description": "Test permission for history",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{TEST_PERMISSION}}",
      "description": "Test permission for history"
    }
    """

    # Create a group - should be recorded in history
    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{TEST_GROUP}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{TEST_GROUP}}"
    }
    """

    # Create a user - should be recorded in history
    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{TEST_USER}}",
      "groups": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Update the permission - should be recorded in history
    Given the following request
    """
    PUT /api/v1/permissions/{{TEST_PERMISSION}} HTTP/1.1
    Content-Type: application/json

    {
      "description": "Updated test permission for history"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Check history - should show all changes
    Given the following request
    """
    GET /api/v1/history HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "UPDATE",
        "entityType": "Permission",
        "entityId": "{{TEST_PERMISSION}}",
        "entityAfterChange": {
          "name": "{{TEST_PERMISSION}}",
          "description": "Updated test permission for history",
          "isDefault": false
        }
      },
      {
        "changeType": "CREATE",
        "entityType": "User",
        "entityId": "{{TEST_USER}}",
        "entityAfterChange": {
          "email": "{{TEST_USER}}",
          "groups": []
        }
      },
      {
        "changeType": "CREATE",
        "entityType": "Group",
        "entityId": "{{TEST_GROUP}}",
        "entityAfterChange": {
          "name": "{{TEST_GROUP}}"
        }
      },
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{TEST_PERMISSION}}",
        "entityAfterChange": {
          "name": "{{TEST_PERMISSION}}",
          "description": "Test permission for history",
          "isDefault": false
        }
      }
    ]
    """

    # Cleanup
    Given the following request
    """
    DELETE /api/v1/users/{{TEST_USER}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{TEST_GROUP}} HTTP/1.1
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

  Scenario: Entity-specific history endpoints return filtered results
    # WHEN requesting history for specific entities
    # THEN the system SHALL return only changes for that entity
    
    Given the variable 'ENTITY_PERMISSION' is set to 'entity-{{GUID()}}'
    Given the variable 'ENTITY_GROUP' is set to 'entity-group-{{GUID()}}'
    Given the variable 'ENTITY_USER' is set to 'entity-user-{{GUID()}}@example.com'

    # Create entities
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{ENTITY_PERMISSION}}",
      "description": "Entity test permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{ENTITY_PERMISSION}}",
      "description": "Entity test permission"
    }
    """

    Given the following request
    """
    POST /api/v1/groups HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{ENTITY_GROUP}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{ENTITY_GROUP}}"
    }
    """

    Given the following request
    """
    POST /api/v1/users HTTP/1.1
    Content-Type: application/json

    {
      "email": "{{ENTITY_USER}}",
      "groups": []
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    """

    # Update permission to create more history
    Given the following request
    """
    PUT /api/v1/permissions/{{ENTITY_PERMISSION}} HTTP/1.1
    Content-Type: application/json

    {
      "description": "Updated entity test permission"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    """

    # Test permission-specific history
    Given the following request
    """
    GET /api/v1/permissions/{{ENTITY_PERMISSION}}/history HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "UPDATE",
        "entityType": "Permission",
        "entityId": "{{ENTITY_PERMISSION}}",
        "entityAfterChange": {
          "name": "{{ENTITY_PERMISSION}}",
          "description": "Updated entity test permission",
          "isDefault": false
        }
      },
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{ENTITY_PERMISSION}}",
        "entityAfterChange": {
          "name": "{{ENTITY_PERMISSION}}",
          "description": "Entity test permission",
          "isDefault": false
        }
      }
    ]
    """

    # Test user-specific history
    Given the following request
    """
    GET /api/v1/users/{{ENTITY_USER}}/history HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "CREATE",
        "entityType": "User",
        "entityId": "{{ENTITY_USER}}",
        "entityAfterChange": {
          "email": "{{ENTITY_USER}}",
          "groups": []
        }
      }
    ]
    """

    # Test group-specific history
    Given the following request
    """
    GET /api/v1/groups/{{ENTITY_GROUP}}/history HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "CREATE",
        "entityType": "Group",
        "entityId": "{{ENTITY_GROUP}}",
        "entityAfterChange": {
          "name": "{{ENTITY_GROUP}}"
        }
      }
    ]
    """

    # Cleanup
    Given the following request
    """
    DELETE /api/v1/users/{{ENTITY_USER}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/groups/{{ENTITY_GROUP}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{ENTITY_PERMISSION}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: History endpoint supports paging with skip and count parameters
    # WHEN requesting history with paging parameters
    # THEN the system SHALL return the specified subset of results
    
    Given the variable 'PAGE_PERM1' is set to 'page:one:{{GUID()}}'
    Given the variable 'PAGE_PERM2' is set to 'page:two:{{GUID()}}'
    Given the variable 'PAGE_PERM3' is set to 'page:three:{{GUID()}}'

    # Create multiple permissions to have enough history entries
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PAGE_PERM1}}",
      "description": "First permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PAGE_PERM1}}",
      "description": "First permission"
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PAGE_PERM2}}",
      "description": "Second permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PAGE_PERM2}}",
      "description": "Second permission"
    }
    """

    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{PAGE_PERM3}}",
      "description": "Third permission",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{PAGE_PERM3}}",
      "description": "Third permission"
    }
    """

    # Test count parameter - get only 2 entries
    Given the following request
    """
    GET /api/v1/history?count=2 HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{PAGE_PERM3}}"
      },
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{PAGE_PERM2}}"
      }
    ]
    """

    # Test skip parameter - skip first entry, get next 2
    Given the following request
    """
    GET /api/v1/history?skip=1&count=2 HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{PAGE_PERM2}}"
      },
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{PAGE_PERM1}}"
      }
    ]
    """

    # Cleanup
    Given the following request
    """
    DELETE /api/v1/permissions/{{PAGE_PERM1}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{PAGE_PERM2}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    Given the following request
    """
    DELETE /api/v1/permissions/{{PAGE_PERM3}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

  Scenario: DELETE operations are recorded in history with EmptyEntity
    # WHEN entities are deleted
    # THEN the system SHALL record DELETE with EmptyEntity that can be serialized
    # This is a regression test for: "Runtime type 'EmptyEntity' is not supported by polymorphic type 'IEntity'"
    
    Given the variable 'DELETE_PERM' is set to 'delete-test-{{GUID()}}'
    
    # Create a permission
    Given the following request
    """
    POST /api/v1/permissions HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{DELETE_PERM}}",
      "description": "Permission to be deleted",
      "isDefault": false
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "name": "{{DELETE_PERM}}",
      "description": "Permission to be deleted",
      "isDefault": false
    }
    """

    # Delete the permission
    Given the following request
    """
    DELETE /api/v1/permissions/{{DELETE_PERM}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 204 NoContent
    """

    # Verify DELETE is recorded in history and can be serialized (no exception thrown)
    Given the following request
    """
    GET /api/v1/permissions/{{DELETE_PERM}}/history HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    [
      {
        "changeType": "DELETE",
        "entityType": "Permission",
        "entityId": "{{DELETE_PERM}}"
      },
      {
        "changeType": "CREATE",
        "entityType": "Permission",
        "entityId": "{{DELETE_PERM}}"
      }
    ]
    """
