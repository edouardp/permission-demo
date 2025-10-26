from flask import Flask, request, jsonify
from handler import lambda_handler

app = Flask(__name__)

@app.route('/<path:path>', methods=['GET', 'POST', 'PUT', 'DELETE', 'PATCH'])
@app.route('/', defaults={'path': ''}, methods=['GET', 'POST', 'PUT', 'DELETE', 'PATCH'])
def proxy(path):
    """Convert HTTP request to Lambda event format"""
    event = {
        'httpMethod': request.method,
        'path': f'/{path}',
        'queryStringParameters': dict(request.args),
        'headers': dict(request.headers),
        'body': request.get_data(as_text=True) if request.data else None
    }
    
    response = lambda_handler(event, {})
    
    return (
        response.get('body', ''),
        response.get('statusCode', 200),
        response.get('headers', {})
    )

def main():
    app.run(host='0.0.0.0', port=5001, debug=True)

if __name__ == '__main__':
    main()
