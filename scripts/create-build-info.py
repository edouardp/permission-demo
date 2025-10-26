#!/usr/bin/env python3
import sys
import subprocess
import json
import os
import platform
import datetime
from pathlib import Path

def get_git_info():
    try:
        return {
            'hash': subprocess.run(['git', 'rev-parse', '--short', 'HEAD'], capture_output=True, text=True, check=True).stdout.strip(),
            'fullHash': subprocess.run(['git', 'rev-parse', 'HEAD'], capture_output=True, text=True, check=True).stdout.strip(),
            'branch': subprocess.run(['git', 'rev-parse', '--abbrev-ref', 'HEAD'], capture_output=True, text=True, check=True).stdout.strip(),
            'repo': subprocess.run(['git', 'config', '--get', 'remote.origin.url'], capture_output=True, text=True, check=False).stdout.strip() or 'unknown',
            'tag': subprocess.run(['git', 'describe', '--tags', '--exact-match'], capture_output=True, text=True, check=False).stdout.strip() or None,
            'isDirty': subprocess.run(['git', 'diff', '--quiet'], check=False).returncode != 0
        }
    except:
        return {'hash': 'unknown', 'fullHash': 'unknown', 'branch': 'unknown', 'repo': 'unknown', 'tag': None, 'isDirty': False}

def get_ci_info():
    # GitHub Actions
    if os.getenv('GITHUB_ACTIONS'):
        return {
            'provider': 'GitHub Actions',
            'buildId': os.getenv('GITHUB_RUN_ID'),
            'buildNumber': os.getenv('GITHUB_RUN_NUMBER'),
            'buildUrl': f"https://github.com/{os.getenv('GITHUB_REPOSITORY')}/actions/runs/{os.getenv('GITHUB_RUN_ID')}",
            'pipeline': os.getenv('GITHUB_WORKFLOW'),
            'actor': os.getenv('GITHUB_ACTOR'),
            'ref': os.getenv('GITHUB_REF')
        }
    
    # Azure DevOps
    elif os.getenv('AZURE_HTTP_USER_AGENT'):
        return {
            'provider': 'Azure DevOps',
            'buildId': os.getenv('BUILD_BUILDID'),
            'buildNumber': os.getenv('BUILD_BUILDNUMBER'),
            'buildUrl': f"{os.getenv('SYSTEM_TEAMFOUNDATIONCOLLECTIONURI')}{os.getenv('SYSTEM_TEAMPROJECT')}/_build/results?buildId={os.getenv('BUILD_BUILDID')}",
            'pipeline': os.getenv('BUILD_DEFINITIONNAME'),
            'actor': os.getenv('BUILD_REQUESTEDFOR'),
            'ref': os.getenv('BUILD_SOURCEBRANCH')
        }
    
    # Jenkins
    elif os.getenv('JENKINS_URL'):
        return {
            'provider': 'Jenkins',
            'buildId': os.getenv('BUILD_ID'),
            'buildNumber': os.getenv('BUILD_NUMBER'),
            'buildUrl': os.getenv('BUILD_URL'),
            'pipeline': os.getenv('JOB_NAME'),
            'actor': os.getenv('BUILD_USER', 'unknown'),
            'ref': os.getenv('GIT_BRANCH')
        }
    
    # GitLab CI
    elif os.getenv('GITLAB_CI'):
        return {
            'provider': 'GitLab CI',
            'buildId': os.getenv('CI_PIPELINE_ID'),
            'buildNumber': os.getenv('CI_PIPELINE_IID'),
            'buildUrl': os.getenv('CI_PIPELINE_URL'),
            'pipeline': os.getenv('CI_PROJECT_NAME'),
            'actor': os.getenv('GITLAB_USER_NAME'),
            'ref': os.getenv('CI_COMMIT_REF_NAME')
        }
    
    # Buildkite
    elif os.getenv('BUILDKITE'):
        return {
            'provider': 'Buildkite',
            'buildId': os.getenv('BUILDKITE_BUILD_ID'),
            'buildNumber': os.getenv('BUILDKITE_BUILD_NUMBER'),
            'buildUrl': os.getenv('BUILDKITE_BUILD_URL'),
            'pipeline': os.getenv('BUILDKITE_PIPELINE_SLUG'),
            'actor': os.getenv('BUILDKITE_BUILD_CREATOR'),
            'ref': os.getenv('BUILDKITE_BRANCH'),
            'agent': os.getenv('BUILDKITE_AGENT_NAME'),
            'job': os.getenv('BUILDKITE_JOB_ID')
        }
    
    return {'provider': 'local', 'buildId': None, 'buildNumber': None, 'buildUrl': None, 'pipeline': None, 'actor': None, 'ref': None}

def get_build_info():
    return {
        'timestamp': datetime.datetime.utcnow().isoformat() + 'Z',
        'machine': platform.node(),
        'os': f"{platform.system()} {platform.release()}",
        'arch': platform.machine(),
        'python': platform.python_version(),
        'user': os.getenv('USER', os.getenv('USERNAME', 'unknown')),
        'workingDir': str(Path.cwd()),
        'dotnetVersion': subprocess.run(['dotnet', '--version'], capture_output=True, text=True, check=False).stdout.strip() or 'unknown'
    }

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print("Usage: create-build-info.py <output-directory>")
        sys.exit(1)
    
    build_info = {
        'git': get_git_info(),
        'ci': get_ci_info(),
        'build': get_build_info()
    }
    
    output_file = os.path.join(sys.argv[1], 'build-info.json')
    
    with open(output_file, 'w') as f:
        json.dump(build_info, f, indent=2)
    
    print(f"Created {output_file}")
    print(f"Provider: {build_info['ci']['provider']}")
    print(f"Git: {build_info['git']['branch']}@{build_info['git']['hash']}")
    if build_info['ci']['buildUrl']:
        print(f"Build: {build_info['ci']['buildUrl']}")
