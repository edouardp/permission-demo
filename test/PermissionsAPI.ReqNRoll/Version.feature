Feature: Version Information

  Scenario: Get version information
    # WHEN the version endpoint is called
    # THEN the system SHALL return comprehensive version information
    # AND the system SHALL return HTTP 200 OK
    # AND the system SHALL return valid JSON with version field
    #
    # The actual values aren't tested, just the structure of the response.
    
    Given the following request
    """
    GET /api/v1/version HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
     "version": [[_]],
     "fileVersion": [[_]],
     "informationalVersion": [[_]],
     "runtimeVersion": [[_]],
     "frameworkDescription": [[_]],
     "osDescription": [[_]],
     "git": {
       "hash": [[_]],
       "fullHash": [[_]],
       "branch": [[_]],
       "repo": [[_]],
       "tag": [[_]],
       "isDirty": [[_]]
     },
     "ci": [[_]],
     "build": [[_]],
     "assemblies": [[_]]
    }
    """
