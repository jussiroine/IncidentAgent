# IncidentAgent

A security incident analysis agent powered by Microsoft.Extensions.AI that provides intelligent security recommendations based on incident alerts.

## Overview

IncidentAgent is a console application that acts as a security advisor, analyzing security incidents and alerts to provide actionable recommendations. It leverages AI capabilities through Ollama to deliver contextual security guidance with prioritized actions.

## Features

- **AI-Powered Analysis**: Uses Ollama with llama3.2:3b model for intelligent incident analysis
- **Security Recommendations**: Provides comprehensive lists of security actions based on alert data
- **Function Invocation**: Built-in security recommendation functions with automatic tool usage
- **Priority Highlighting**: Emphasizes the most critical recommended action
- **JSON Input Processing**: Reads and analyzes incident data from JSON files

## Prerequisites

- [Ollama](https://ollama.com/) server running locally on `http://localhost:11434/`
- llama3.2:3b model installed in Ollama
- .NET runtime environment
- An `incident.json` file containing security alert data (placed in parent directory). A minimal example is included in the repository root.

## Setup

1. Install and start Ollama server
2. Pull the required model:
   ```bash
   ollama pull llama3.2:3b
   ```
3. Create an `incident.json` file with your security alert data
4. Run the IncidentAgent application

## Usage

The application will:
1. Initialize connection to the Ollama server
2. Load incident data from `../incident.json`
3. Analyze the security alert using AI
4. Provide a list of recommended security actions
5. Highlight the most critical action to take

## Security Recommendations

The agent provides recommendations such as:
- Multi-factor authentication (MFA) implementation
- Password reset procedures
- Suspicious activity monitoring
- Account security monitoring
- Additional context-specific security measures

## Configuration

- **Endpoint**: `http://localhost:11434/` (Ollama server)
- **Model**: `llama3.2:3b`
- **Temperature**: 0.5 (balanced creativity/consistency)
- **Tool Mode**: Auto (automatic function invocation)

## Example `incident.json`

The repository includes a minimal `incident.json` demonstrating common fields:

```json
{
  "alertId": "EX-001",
  "alertType": "UnauthorizedAccess",
  "severity": "High",
  "description": "Multiple failed login attempts detected for admin account",
  "timestamp": "2024-05-21T10:15:00Z"
}
```
