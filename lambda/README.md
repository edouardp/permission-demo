# Lambda Local Testing

Run Lambda Python code locally as HTTP endpoints behind nginx.

## Quick Start with uv/uvx (Recommended)

```bash
# Option 1: Run directly with uv
cd lambda
uv run local_server.py

# Option 2: Use as installed tool
uv tool install --from . lambda-server
lambda-server

# Option 3: One-liner with uvx
cd lambda && uvx --from . lambda-server
```

## Alternative: Plain Python

```bash
cd lambda
pip install -r requirements.txt
python local_server.py
```

## Alternative: AWS SAM CLI

```bash
# Install SAM CLI
brew install aws-sam-cli

# Run Lambda locally
sam local start-api --port 5001
```

## Usage Through nginx Gateway

```bash
# Start all services
dotnet run --project src/PermissionsApi &
dotnet run --project src/PermissionsSharedBff &
python lambda/local_server.py &
docker compose up

# Call Lambda through gateway
curl http://localhost:8080/lambda/test
curl -X POST http://localhost:8080/lambda/process -d '{"data": "test"}'
```

## Writing Lambda Functions

Your Lambda code works unchanged in both local and AWS:

```python
def lambda_handler(event, context):
    # event contains API Gateway proxy format
    method = event['httpMethod']
    path = event['path']
    body = event.get('body')
    
    return {
        'statusCode': 200,
        'headers': {'Content-Type': 'application/json'},
        'body': '{"result": "success"}'
    }
```

## Routing

- `/lambda/*` → Lambda function (port 5001)
- `/api/*` → PermissionsApi (port 5000)
- `/bff/*` → PermissionsSharedBff (port 5017)

## Deploy to AWS

Same code deploys to Lambda without changes:

```bash
# Package
zip function.zip handler.py

# Deploy
aws lambda create-function \
  --function-name my-function \
  --runtime python3.12 \
  --handler handler.lambda_handler \
  --zip-file fileb://function.zip \
  --role arn:aws:iam::ACCOUNT:role/lambda-role
```
