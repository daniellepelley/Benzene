# Documentation Writer Agent Guide

## Overview

The **documentation-writer** agent is a specialized Claude agent designed to create comprehensive, engaging, and accurate documentation for Benzene. It can generate three types of documentation:

1. **Getting Started Guides** - Hands-on tutorials for maximum engagement
2. **Reference Documentation** - Detailed technical documentation
3. **Cookbooks** - Practical recipes for specific scenarios

## How to Use the Documentation Writer

### Using the Task Tool

The documentation writer is available through Claude Code's Task tool. You don't need to invoke it manually - just ask Claude Code to create documentation, and it will automatically use this agent when appropriate.

**Example requests:**

```
Write a getting started guide for using Benzene with Azure Functions
```

```
Create reference documentation for the middleware pipeline
```

```
Write a cookbook for handling SQS message failures with retry logic
```

### Direct Invocation (Advanced)

If you want to explicitly invoke the documentation writer agent, you can reference the agent file:

```
Task: Using the documentation-writer agent, create a cookbook for implementing rate limiting in Benzene
```

## Types of Documentation

### 1. Getting Started Guides

**Purpose**: Help developers go from zero to a working, deployed solution quickly.

**When to create**:
- Introducing a new platform (AWS Lambda, Azure Functions, etc.)
- Major feature that needs hands-on introduction
- Integration with a new cloud service

**Example topics**:
- Getting Started with Benzene on AWS Lambda
- Getting Started with Benzene on Azure Functions
- Getting Started with ASP.NET Core Integration
- Getting Started with gRPC in Benzene

**Structure**:
- Start from an empty project
- Step-by-step incremental build
- Complete, runnable code at each step
- End with deployment instructions
- Include troubleshooting section

### 2. Reference Documentation

**Purpose**: Comprehensive technical documentation of features, APIs, and configuration.

**When to create**:
- Documenting a new package or feature
- Explaining middleware or core concepts
- Detailing configuration options
- API reference for public interfaces

**Example topics**:
- Middleware Pipeline Reference
- Message Handler Reference
- Validation Framework Integration
- Health Checks Reference
- OpenTelemetry Integration

**Structure**:
- Concise summary
- When and why to use
- All configuration options
- Simple to advanced examples
- Cross-references to related features

### 3. Cookbooks

**Purpose**: Solve specific, real-world problems with complete, copy-pasteable solutions.

**When to create**:
- Common integration scenarios
- Best practices for specific patterns
- Solutions to frequent user questions
- Platform-specific recipes (AWS/Azure)

**Example topics**:
- Logging to Application Insights
- Handling SQS Message Failures
- Distributed Tracing with OpenTelemetry
- Entity Framework Core Integration
- Custom Authentication Middleware
- Circuit Breaker Pattern
- Request Correlation Across Services

**Structure**:
- Clear problem statement
- Prerequisites listed
- Step-by-step implementation
- Testing instructions
- Troubleshooting common issues
- Variations and alternatives
- Further reading links

## Documentation Workflow

### 1. Planning Phase

Before creating documentation, the agent should:
- Read existing documentation for style consistency
- Examine source code for accuracy
- Review examples and tests for patterns
- Identify related documentation for cross-references

### 2. Writing Phase

During writing:
- Use clear, direct language
- Provide complete code examples
- Include all using statements and package references
- Follow Benzene conventions
- Cross-reference related docs

### 3. Review Phase

After writing:
- Verify all code examples are complete
- Check package names and versions
- Ensure using statements are correct
- Validate cross-references
- Test code snippets where possible

## Best Practices

### Code Examples

**DO**:
- Include complete, runnable examples
- Show all necessary using statements
- Use actual Benzene packages and APIs
- Follow C# and Benzene conventions
- Provide context for each example

**DON'T**:
- Show incomplete code fragments without context
- Assume readers know implicit details
- Use pseudo-code unless explicitly demonstrating concepts
- Skip error handling in production examples

### Writing Style

**DO**:
- Use active voice ("Add the middleware" not "The middleware is added")
- Be direct and concise
- Explain *why* not just *what*
- Provide troubleshooting guidance
- Include real-world context

**DON'T**:
- Use passive voice unnecessarily
- Add fluff or filler content
- Assume prior knowledge without stating prerequisites
- Leave readers stuck without next steps

### Accuracy

**DO**:
- Read source code to verify behavior
- Check actual package names from .csproj files
- Verify API signatures from source
- Test examples where possible
- Update docs when APIs change

**DON'T**:
- Guess about implementation details
- Document features that don't exist
- Show deprecated patterns without warnings
- Leave outdated information

## Common Documentation Tasks

### Creating a New Getting Started Guide

1. Identify the target platform/scenario
2. Research existing examples in `examples/`
3. Check existing getting-started docs in `docs/`
4. Create incremental tutorial from scratch to deployment
5. Test each code snippet
6. Add troubleshooting section

**Location**: `docs/getting-started-{platform}.md`

### Creating Reference Documentation

1. Identify the feature/package
2. Read source code in `src/`
3. Find all configuration options
4. Document all public APIs
5. Create basic, intermediate, advanced examples
6. Add cross-references

**Location**: `docs/{feature-name}.md`

### Creating a Cookbook

1. Define the specific problem
2. List all prerequisites and packages
3. Research integration points in source
4. Write step-by-step implementation
5. Add testing instructions
6. Include troubleshooting section
7. Suggest variations

**Location**: `docs/cookbooks/{cookbook-name}.md`

## Existing Documentation Structure

```
docs/
├── index.md                          # Main documentation index
├── Overview.md                       # High-level overview
├── getting-started-aws.md            # AWS Lambda getting started
├── azure-functions.md                # Azure Functions getting started
├── asp-net-core.md                   # ASP.NET Core integration
├── message-handlers.md               # Message handler reference
├── middleware.md                     # Middleware reference
├── common-middleware.md              # Common middleware patterns
├── monitoring.md                     # Observability features
├── health-checks.md                  # Health check reference
├── fluent-validation.md              # Validation integration
├── correlation-ids.md                # Correlation ID patterns
├── testing-benzene.md                # Testing guide
└── cookbooks/                        # Cookbook collection
    ├── README.md                     # Cookbook index
    ├── logging-application-insights.md
    ├── handling-sqs-failures.md
    ├── distributed-tracing-opentelemetry.md
    └── ...
```

## Agent Capabilities

The documentation-writer agent has access to:

- **Read**: Read source code, docs, examples
- **Glob**: Find files by pattern
- **Grep**: Search code for patterns
- **Bash**: Run commands to verify information
- **WebFetch**: Research external integrations

## Examples of Agent Usage

### Example 1: Creating a Cookbook

**User Request**:
> "Write a cookbook for implementing retry logic for SQS message processing failures"

**Agent Actions**:
1. Read existing SQS integration code in `src/Benzene.Aws.Lambda.Sqs/`
2. Check for retry-related middleware in `src/Benzene.Resilience/`
3. Review SQS examples in `examples/Aws/`
4. Research AWS SQS retry patterns
5. Create cookbook with complete implementation
6. Include DLQ configuration and troubleshooting

### Example 2: Creating Reference Documentation

**User Request**:
> "Create reference documentation for the health check system"

**Agent Actions**:
1. Read health check source in `src/Benzene.HealthChecks.Core/`
2. Find all health check implementations in `src/Benzene.HealthChecks.*/`
3. Review usage in examples
4. Document all configuration options
5. Create examples for HTTP, EF, custom checks
6. Cross-reference monitoring docs

### Example 3: Creating Getting Started Guide

**User Request**:
> "Create a getting started guide for using Benzene with Google Cloud Functions"

**Agent Actions**:
1. Check if Google Cloud support exists in `src/`
2. Review Google Cloud example in `examples/Google/`
3. Research Google Cloud Functions .NET integration
4. Create step-by-step tutorial
5. Include deployment with `gcloud` CLI
6. Add troubleshooting for common GCP issues

## Quality Standards

All documentation must meet these standards:

- [ ] **Accurate**: Verified against source code
- [ ] **Complete**: No missing steps or prerequisites
- [ ] **Tested**: Code examples are valid
- [ ] **Consistent**: Follows existing doc patterns
- [ ] **Clear**: Easy to understand for target audience
- [ ] **Practical**: Includes real-world examples
- [ ] **Maintainable**: Cross-references are correct
- [ ] **Troubleshooting**: Includes common issues

## Maintenance

### Updating Documentation

When APIs change:
1. Update all affected documentation
2. Add migration notes if breaking changes
3. Update code examples
4. Check cross-references
5. Mark deprecated features clearly

### Documentation Debt

Track documentation that needs work:
- Outdated examples
- Missing features
- User-reported confusion
- API changes without doc updates

## Getting Help

If the documentation writer needs clarification:
- Ask specific questions about the feature
- Request access to relevant source code
- Ask for examples of the feature in use
- Request information about target audience

## Continuous Improvement

The documentation writer should:
- Learn from user feedback
- Improve based on common questions
- Evolve with the product
- Stay consistent with Benzene conventions
- Maintain high quality standards

---

**For more information**:
- See `.claude/agents/documentation-writer.md` for agent instructions
- See `docs/` for existing documentation
- See `examples/` for code examples
- See product owner docs for domain expertise
