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

---

## ⚠️ Security Notice

**Current Status:** Proof of Concept - Not Production Ready

This application currently contains security vulnerabilities that must be addressed before production use. A comprehensive security analysis has been performed.

### 📚 Documentation & Analysis

For detailed security analysis, development guidance, and improvement recommendations, see:

#### 🎯 Start Here
- **[EXECUTIVE_SUMMARY.md](./EXECUTIVE_SUMMARY.md)** - High-level overview, business impact, and quick start guide

#### 🔍 Detailed Analysis  
- **[SECURITY_ANALYSIS.md](./SECURITY_ANALYSIS.md)** - Complete security vulnerability report (10 issues identified)
- **[SECURITY_CHECKLIST.md](./SECURITY_CHECKLIST.md)** - Comprehensive security tracking checklist

#### 🛠️ Implementation Guides
- **[DEVELOPMENT_ROADMAP.md](./DEVELOPMENT_ROADMAP.md)** - 5-phase development plan with detailed tasks
- **[IMPLEMENTATION_EXAMPLES.md](./IMPLEMENTATION_EXAMPLES.md)** - Working code examples for security improvements

#### 💡 Future Development
- **[INNOVATION_IDEAS.md](./INNOVATION_IDEAS.md)** - 32 innovative features for future enhancement

### 🔴 Critical Issues Identified

1. **Path Traversal Vulnerability** - File path validation needed
2. **No Input Validation** - JSON content not validated
3. **No Error Handling** - Application crashes expose information
4. **Hard-coded Configuration** - Endpoint and credentials in source code

**Estimated Effort to Production-Ready:** 2-3 months

See [SECURITY_ANALYSIS.md](./SECURITY_ANALYSIS.md) for complete details and [IMPLEMENTATION_EXAMPLES.md](./IMPLEMENTATION_EXAMPLES.md) for code examples to fix these issues.

---

## 🚀 Quick Start for Contributors

### For Developers:
1. Read [SECURITY_ANALYSIS.md](./SECURITY_ANALYSIS.md) to understand current issues
2. Review [IMPLEMENTATION_EXAMPLES.md](./IMPLEMENTATION_EXAMPLES.md) for solution patterns
3. Use [SECURITY_CHECKLIST.md](./SECURITY_CHECKLIST.md) to track improvements

### For Project Managers:
1. Review [EXECUTIVE_SUMMARY.md](./EXECUTIVE_SUMMARY.md) for business context
2. Use [DEVELOPMENT_ROADMAP.md](./DEVELOPMENT_ROADMAP.md) for planning
3. Check [INNOVATION_IDEAS.md](./INNOVATION_IDEAS.md) for feature roadmap

### For Security Engineers:
1. Analyze [SECURITY_ANALYSIS.md](./SECURITY_ANALYSIS.md) for vulnerability details
2. Validate with [SECURITY_CHECKLIST.md](./SECURITY_CHECKLIST.md)
3. Test fixes using [IMPLEMENTATION_EXAMPLES.md](./IMPLEMENTATION_EXAMPLES.md)

---

## 📈 Development Status

| Category | Status |
|----------|--------|
| **Concept** | ✅ Validated |
| **Security** | 🔴 Critical Issues (10 identified) |
| **Architecture** | 🟡 Needs Refactoring |
| **Testing** | 🔴 No Tests |
| **Documentation** | ✅ Comprehensive |
| **Production Ready** | ❌ No (2-3 months estimated) |

---

## 🤝 Contributing

Contributions are welcome! Please:
1. Review the security documentation first
2. Follow the implementation examples provided
3. Add tests for new functionality
4. Ensure security best practices are followed

---

## 📝 License

[Add your license information here]

---

## 🙏 Acknowledgments

Security analysis and development guidance provided by Security Posture Agent.
