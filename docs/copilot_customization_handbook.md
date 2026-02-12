# GitHub Copilot Customization Handbook

**Instructions · Prompts · Agents · Skills**

A comprehensive guide to tailoring GitHub Copilot to your team's workflows in Visual Studio Code

*February 2026 · Based on VS Code Copilot Documentation*

---

## Table of Contents

1. [Understanding the Customization Landscape](#1-understanding-the-customization-landscape)
2. [Custom Instructions](#2-custom-instructions)
3. [Prompt Files](#3-prompt-files)
4. [Custom Agents](#4-custom-agents)
5. [Agent Skills](#5-agent-skills)
6. [How Context is Built](#6-how-context-is-built)
7. [Comparison Matrix](#7-comparison-matrix)
8. [Decision Framework](#8-decision-framework)
9. [Best Practices](#9-best-practices)

---

## 1. Understanding the Customization Landscape

GitHub Copilot in VS Code offers four distinct customization mechanisms, each designed to solve a different problem. Understanding the differences between them, and knowing when to reach for each one, is the key to building a productive, consistent AI-assisted development workflow across your team.

Think of these four mechanisms as layers of increasing specificity. Instructions set the baseline context that applies everywhere. Prompt files capture repeatable tasks you trigger on demand. Custom agents define entire operational personas with their own toolsets and rules. And agent skills package portable, specialized capabilities that activate automatically when relevant.

| Feature | Purpose | Activation | Location |
|---------|---------|------------|----------|
| Instructions | Always-on project context and coding standards | Automatic (every request) | `.github/copilot-instructions.md` or `*.instructions.md` |
| Prompt Files | Reusable task templates for common workflows | On-demand via `/command` | `.github/prompts/*.prompt.md` |
| Custom Agents | Named personas with specific tools and rules | User selects agent in chat | `.github/agents/*.agent.md` |
| Agent Skills | Portable specialized capabilities with resources | Auto-activated by prompt matching | `.github/skills/*/SKILL.md` |

> **INFO:** All customization files are Markdown-based with YAML frontmatter. They can be committed to your repository and shared with your entire team through version control.

---

## 2. Custom Instructions

Custom instructions are the foundation of Copilot customization. They define the always-on context that Copilot automatically includes in every chat interaction. Think of them as setting the ground rules: your project's coding conventions, architectural patterns, preferred libraries, and team standards.

### What They Are

Custom instructions are Markdown files that provide background context to Copilot. Unlike prompt files or agents that you trigger explicitly, instructions are injected into the context of every single chat request automatically. They ensure Copilot consistently understands your project's norms without you having to repeat yourself in every prompt.

### Types of Instruction Files

#### 1. Project-Wide Instructions — `copilot-instructions.md`

The primary instructions file lives at `.github/copilot-instructions.md` in your repository root. Its contents are automatically included in all chat interactions for anyone working in that workspace. This is the single most important customization file you can create.

```markdown
# Project Guidelines

## Architecture
- This is a React 18 + TypeScript monorepo
- Use the Repository pattern for data access
- All API calls go through the /services layer

## Code Style
- Use arrow functions for React components
- Prefer const over let; never use var
- Always include TypeScript type annotations
- Use descriptive variable names (no abbreviations)

## References
- [Architecture](../ARCHITECTURE.md)
- [Contributing Guide](../CONTRIBUTING.md)
```

#### 2. File-Targeted Instructions — `*.instructions.md`

For more granular control, you can create instruction files that apply only to specific file types or paths. These use the `applyTo` frontmatter field with glob patterns to target their scope. Store them anywhere in your workspace or in the `.github/instructions/` directory.

```markdown
---
applyTo: "**/*.tsx"
---
# React Component Standards

- Use functional components with hooks
- Export components as named exports
- Co-locate tests in __tests__ directories
- Use CSS Modules for styling
```

#### 3. User-Level Instructions

Personal instructions that apply across all your workspaces. Configure these in VS Code settings under `github.copilot.chat.codeGeneration.instructions` or through the Settings UI. These are useful for personal preferences like response format or tone.

### When to Use Instructions

Instructions are the right choice when you need:

- Project-wide coding standards that should apply to every interaction
- Architectural context that helps Copilot understand your codebase structure
- Technology-specific guidance (framework versions, library preferences)
- Language or file-type specific rules (e.g., different styles for `.tsx` vs `.py` files)
- Team conventions that every developer should follow consistently

> **TIP:** To generate a `copilot-instructions.md` file tailored to your project, click the **Configure Chat** gear icon in the Chat view and select **Generate Chat Instructions**. Review the generated file and make any necessary edits to match your team's standards.

> **NOTE:** Custom instructions do NOT affect inline suggestions as you type in the editor. They only apply to chat interactions (Ask, Edit, and Agent modes).

---

## 3. Prompt Files

Prompt files are reusable, on-demand task templates that you invoke explicitly in chat. While instructions set the background context, prompt files define specific workflows: generating a component, performing a code review, scaffolding a new feature, or running a migration. They are the recipes in your team's cookbook.

### What They Are

Prompt files are Markdown files with the `.prompt.md` extension. They contain a YAML frontmatter header with metadata (description, model, tools, agent) and a body with the actual prompt instructions. Unlike instructions that apply everywhere, prompt files are triggered on-demand when you type `/prompt-name` in the chat input field.

### File Structure

```markdown
---
description: 'Generate a new React form component'
agent: 'agent'
model: Claude Sonnet 4
tools: ['githubRepo', 'search/codebase']
---

Your goal is to generate a new React form component.
Ask for the form name and fields if not provided.

Requirements:
* Use form design system components:
  [design-system/Form.md](../docs/design-system/Form.md)
* Use `react-hook-form` for state management
* Use `yup` for validation
* Always define TypeScript types for form data
```

### Scopes

**Workspace prompt files** live in `.github/prompts/` and are available only within that workspace. They can be committed to version control and shared with your team.

**User prompt files** are stored in your VS Code profile and are available across all workspaces. Useful for personal productivity workflows that aren't project-specific.

### Key Frontmatter Fields

| Field | Purpose | Example |
|-------|---------|---------|
| `description` | Shown in the `/` command picker | `'Generate a React form'` |
| `agent` | Which agent to run the prompt in | `'agent'` or `'ask'` |
| `model` | Preferred LLM model | `'Claude Sonnet 4'` |
| `tools` | Tools available during execution | `['search/codebase', 'fetch']` |

### How to Run Prompt Files

- Type `/prompt-name` in the chat input field and optionally add extra context
- Run **Chat: Run Prompt** from the Command Palette and select a prompt
- Open the `.prompt.md` file in the editor and press the play button in the title bar

### When to Use Prompt Files

- Repeatable workflows you run frequently (component generation, code reviews, migrations)
- Tasks that need specific tools and model configurations each time
- Team-standard workflows that all developers should follow consistently
- Complex multi-step tasks that benefit from detailed, pre-written instructions

> **TIP:** Prompt files can reference custom instructions via Markdown links, avoiding duplication. For example: `[coding standards](../docs/standards.md)`

---

## 4. Custom Agents

Custom agents (formerly called "custom chat modes") define how Copilot Chat operates. They are named personas that set the boundaries for an entire conversation session: which tools are available, what instructions to follow, how to interact with the codebase, and even which language model to use. When you select a custom agent, every prompt in that session runs within its defined rules.

### What They Are

Custom agents are `.agent.md` files stored in `.github/agents/`. Each agent defines a specific operational context: a Planner agent that only creates implementation plans without making code edits, a Reviewer agent that focuses on code quality analysis, or a Feature Builder agent that has access to specific tools and follows particular architectural patterns.

### File Structure

```markdown
---
description: Generate an implementation plan for new features
name: Planner
tools: ['fetch', 'githubRepo', 'search', 'usages']
model: ['Claude Opus 4.5', 'GPT-5.2']
handoffs:
  - label: Implement Plan
    agent: agent
    prompt: Implement the plan outlined above.
    send: false
---

# Planning Instructions

You are in planning mode. Your task is to generate
an implementation plan. Don't make any code edits.

The plan should include:
* Overview of the feature or refactoring task
* Step-by-step implementation approach
* Files that need to be created or modified
* Testing strategy
```

### Key Capabilities

- **Tool restrictions:** Limit which tools the agent can use (e.g., a planning agent can search but not edit files)
- **Model selection:** Specify preferred models, with fallback ordering
- **Handoffs:** Define buttons that transition the user to another agent with a pre-filled prompt, creating multi-step workflows (e.g., Plan → Implement → Review)
- **Custom system prompts:** The Markdown body becomes the agent's operational instructions

### How Handoffs Work

Handoffs are a powerful feature that lets you chain agents together. When a user finishes with one agent, they see a button that transitions them to the next agent in the workflow. If `send: true`, the prompt auto-submits; if `send: false`, the user can review and modify it first.

### When to Use Custom Agents

- You need a named persona that consistently orchestrates tools for a workflow
- You want to restrict which tools are available to prevent unintended actions
- You need multi-step workflows with handoff transitions between phases
- Different team members work in different modes (planning vs. implementing vs. reviewing)
- You want to enforce that certain operations use specific, high-capability models

> **NOTE:** Custom agents define the session-level operating context. They work best as the outermost "wrapper" around a workflow. Combine them with instructions (for standards) and skills (for specialized tasks) for maximum effectiveness.

---

## 5. Agent Skills

Agent Skills are the newest and most portable of Copilot's customization features. They are folders of instructions, scripts, and resources that Copilot automatically loads when it detects your prompt is relevant to a skill's described capability. Unlike instructions (always-on) or prompts (user-triggered), skills are auto-activated based on intent matching, and unlike agents (session-level), skills are task-level.

### What They Are

Each skill is a directory containing a `SKILL.md` file with YAML frontmatter (name, description) and a Markdown body with detailed instructions. The directory can also include scripts, templates, example files, and other resources the AI can reference. Skills follow an open standard and work across multiple agents: GitHub Copilot in VS Code, GitHub Copilot CLI, GitHub's Copilot coding agent and Claude Code.

### File Structure

```
.github/skills/
├── webapp-testing/
│   ├── SKILL.md              # Main skill definition
│   ├── test-template.js      # Template file
│   └── examples/
│       └── login-test.spec.ts # Example test
└── react-components/
    ├── SKILL.md
    ├── component-template.tsx
    └── styles-guide.md
```

### SKILL.md Anatomy

```markdown
---
name: webapp-testing
description: >
  Guide for testing web applications using Playwright.
  Use this when asked to create or run browser-based
  tests.
---

# Web Application Testing with Playwright

## When to use this skill
- Create new Playwright tests for web apps
- Debug failing browser tests
- Set up test infrastructure

## Creating tests
1. Always use the Page Object Model pattern
2. Reference the test template:
   [test-template.js](./test-template.js)
3. Follow naming convention: feature.spec.ts
```

### How Progressive Disclosure Works

Skills use a three-level progressive disclosure model to keep context efficient. At startup, only the name and description from the frontmatter are loaded into Copilot's system prompt. This is level one. When Copilot determines a skill is relevant to the current task, it loads the full SKILL.md body (level two). Only if the instructions reference additional files in the skill directory does Copilot load those resources (level three).

This means you can install many skills without bloating the context window.

### Storage Locations

| Type | Path | Scope |
|------|------|-------|
| Project skills | `.github/skills/` (recommended) `.claude/skills/` (legacy) | Repository-specific |
| Personal skills | `~/.copilot/skills/` (recommended) `~/.claude/skills/` (legacy) | All your workspaces |

### Skills vs. Instructions

| Aspect | Custom Instructions | Agent Skills |
|--------|---------------------|--------------|
| Activation | Always included in every request | Auto-loaded only when relevant |
| Scope | Project-wide or file-targeted | Task-specific capabilities |
| Resources | Markdown text only | Can include scripts, templates, examples |
| Portability | VS Code specific | Open standard across agents |
| Best for | Coding standards, architecture context | Specialized workflows, tools, procedures |

> **TIP:** Skills are an open standard. A skill you create for GitHub Copilot in VS Code also works with GitHub Copilot CLI, the Copilot coding agent, and Claude Code.

---

## 6. How Context is Built

Understanding how Copilot assembles context from all these sources is essential for effective customization. When you send a chat message, Copilot constructs a context window by layering information from multiple sources in a specific priority order.

### The Context Assembly Process

When you type a message in Copilot Chat, here is what happens behind the scenes:

1. **Agent selection:** The active agent (built-in or custom) establishes the session's operational boundaries, including which tools are available and what system-level instructions apply.

2. **Instructions injection:** All applicable instruction files are collected and injected. This includes the project-wide `copilot-instructions.md`, any file-targeted `*.instructions.md` files that match the current context, and user-level instructions from VS Code settings. No specific order is guaranteed when multiple instruction types are combined.

3. **Skill matching:** Copilot examines the skill descriptions it has pre-loaded and determines if any are relevant to your prompt. If a match is found, the full SKILL.md body is loaded into context.

4. **Explicit context:** Any files, symbols, terminal output, or other context you explicitly attached to the prompt via `#`-mentions is included.

5. **Prompt file content:** If you triggered a prompt file via `/command`, its instructions are added to the context.

6. **Your message:** Finally, your actual message text is added as the user prompt.

> **INFO:** If multiple types of customization files exist in your project, VS Code combines them all. Use the diagnostics view (right-click in Chat → Diagnostics) to see all loaded customization files and troubleshoot issues.

### Context Priority for Tools

When a prompt file and a custom agent both specify tools, the effective tool list is resolved by priority. A prompt file's tools override the agent's tools for that request. If neither specifies tools, the default set for the selected agent is used. Tools that aren't available in the environment are silently ignored.

---

## 7. Comparison Matrix

The following table provides a detailed side-by-side comparison of all four customization mechanisms.

| Dimension | Instructions | Prompt Files | Custom Agents | Agent Skills |
|-----------|--------------|--------------|---------------|--------------|
| File extension | `.instructions.md` or `copilot-instructions.md` | `.prompt.md` | `.agent.md` | `SKILL.md` |
| Location | `.github/` or anywhere in workspace | `.github/prompts/` or user profile | `.github/agents/` or user profile | `.github/skills/*/SKILL.md` |
| Activation | Automatic (every request) | On-demand (`/` command) | User selects in chat | Auto-matched by description |
| Scope | Global or file-targeted | Per-invocation | Session-level | Task-level |
| Can include scripts/files | No (Markdown only) | No (Markdown only) | No (Markdown only) | Yes (full directory) |
| Specifies tools | No | Yes | Yes | No |
| Specifies model | No | Yes | Yes | No |
| Portable across agents | VS Code only | VS Code only | VS Code only | Yes (open standard) |
| Version controllable | Yes | Yes | Yes | Yes |
| Affects inline suggestions | No | No | No | No |

---

## 8. Decision Framework

Use the following decision tree to determine which customization type best fits your need.

| If you need to... | Use | Why |
|-------------------|-----|-----|
| Set project-wide coding standards that apply to every interaction | Instructions | Always-on, zero friction, ensures consistent baseline |
| Apply rules only to specific file types (e.g., `.tsx` vs `.py`) | Instructions (file-targeted) | `applyTo` glob patterns target precisely |
| Create a repeatable workflow that team members invoke on demand | Prompt Files | On-demand, sharable, configurable tools and model |
| Define a named operational persona with restricted tools | Custom Agents | Session-level control over behavior and capabilities |
| Chain multiple workflow phases together (plan → implement → review) | Custom Agents (with handoffs) | Handoff transitions create structured pipelines |
| Package a specialized capability with scripts and templates | Agent Skills | Self-contained, auto-activated, portable across agents |
| Share capabilities across teams or the community | Agent Skills | Open standard works beyond VS Code |
| Give Copilot the ability to run project-specific scripts | Agent Skills | Skills can include executable scripts and resources |

### Combining Customizations

These features are not mutually exclusive — they are designed to work together. A recommended layered approach:

- **Layer 1 — Instructions:** Start with `copilot-instructions.md` for project-wide standards. Add file-targeted instructions for language-specific rules. Keep instructions concise and focused on high-level guidelines.

- **Layer 2 — Prompt Files:** Create prompt files for common workflows your team performs repeatedly. Reference your instruction files via Markdown links to avoid duplication.

- **Layer 3 — Custom Agents:** Define agents for distinct operational modes (planning, implementing, reviewing). Use handoffs to chain agents into multi-step workflows.

- **Layer 4 — Agent Skills:** Build skills for specialized, self-contained capabilities that include scripts, templates, or examples. Let them auto-activate when relevant.

---

## 9. Best Practices

### Getting Started

- Use **Configure Chat** (gear icon) → **Generate Chat Instructions** to auto-generate a `copilot-instructions.md` based on your project structure
- Keep your initial instructions file concise — one page maximum. Add detail over time based on where Copilot falls short.
- Commit all customization files to version control so your entire team benefits
- Use the diagnostics view (right-click Chat → Diagnostics) to verify which files are loaded

### Writing Effective Instructions

- Be specific and actionable — "Use arrow functions for React components" is better than "Write clean code"
- Reference supporting documentation via Markdown links rather than inlining everything
- Include both positive guidance ("do this") and negative guidance ("avoid that")
- Override the model's default behavior where needed (e.g., "Don't apologize" or "Don't add comments unless asked")

### Organizing Skills

- Write clear, keyword-rich descriptions — Copilot matches skills to prompts based on the description field
- Use progressive disclosure: put the most important instructions in SKILL.md, details in reference files
- Include example inputs and outputs to demonstrate the expected behavior
- Test skills by prompting Copilot with tasks that should trigger them and verifying they activate
- Review community-contributed skills before using them to ensure quality and security

### Team Adoption

- Establish a single `copilot-instructions.md` as your team's "source of truth" for AI-assisted development
- Build a shared library of prompt files for common team workflows (PRs, migrations, component scaffolding)
- Document your customization strategy in CONTRIBUTING.md so new team members onboard quickly
- Iterate continuously — review Copilot's outputs and refine your customization files accordingly

> **INFO:** For more community-contributed examples of instructions, prompts, agents, and skills, visit the **github/awesome-copilot** repository on GitHub.
