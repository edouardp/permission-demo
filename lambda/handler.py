def lambda_handler(event, context):
    """Standard Lambda handler - works in AWS and locally"""
    body = event.get('body', '{}')
    path = event.get('path', '/')
    method = event.get('httpMethod', 'GET')
    
    return {
        'statusCode': 200,
        'headers': {'Content-Type': 'application/json'},
        'body': f'{{"message": "Hello from Lambda", "path": "{path}", "method": "{method}"}}'
    }
