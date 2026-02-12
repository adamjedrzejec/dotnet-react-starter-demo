# dotnet-react-starter — Usage Guide

A .NET 8 Web API + React 18 template app pre-configured with GitHub Copilot customizations:
- 3 custom agents
- 3 prompt files
- 1 skill (API standards reference)

This guide focuses on **execution** — how to get your copy, run the app, and use the Copilot customizations.

---

## 1) Get your copy of the template

1. Open https://github.com/maxmash1/dotnet-react-starter-demo
2. Click **Use this template** → **Create a new repository**
3. Choose owner, repo name, and visibility → **Create repository**

---

## 2) Start the app — Codespaces (recommended, zero setup)

The repo includes a `.devcontainer/devcontainer.json` that pre-installs everything (.NET 8 SDK, Node.js 20, VS Code extensions including GitHub Copilot). No local tooling is required.

1. In your new repo, click **Code** → **Codespaces** → **Create codespace on main**
2. Wait for the environment to build (dependencies install automatically via `postCreateCommand`)
3. Start backend and frontend in two terminals:

```bash
# Terminal 1 — Backend
cd backend
dotnet run --project src/Api
```

```bash
# Terminal 2 — Frontend
cd frontend
npm run dev
```

- **Backend API:** port 5000 — Swagger UI at `/swagger`
- **Frontend:** port 5173 — auto-opens in preview

Both ports are auto-forwarded; Codespaces will notify you when they're ready.

---

## 3) Start the app — Local development (alternative)

**Prerequisites:** .NET SDK 8.x, Node.js 20+, Git, VS Code with GitHub Copilot.

### Backend
```bash
cd backend
dotnet restore
dotnet build
dotnet test          # 6 tests should pass
dotnet run --project src/Api
```

### Frontend
```bash
cd frontend
npm install
npm run dev
```

- **Backend API:** http://localhost:5000 — Swagger UI at `/swagger`
- **Frontend:** http://localhost:5173

---

## 4) Copilot customization assets (what they are)

### Custom agents (3)
Specialized Copilot personas you select from the **Agents** dropdown in Copilot Chat. Located in `.github/agents/`.
- **api-builder** — scaffolds new API endpoints end-to-end (hands off to `test-coverage`)
- **test-coverage** — finds coverage gaps and generates tests
- **asp-migrator** — migrates Classic ASP to .NET 8 + React (hands off to `test-coverage` and `api-builder`)

### Prompt files (3)
Reusable task templates invoked from the Copilot Chat prompt via the **slash-command picker** (type `/`). Located in `.github/prompts/`.
- **/create-api-endpoint** — step-by-step scaffold checklist for a new endpoint
- **/add-unit-tests** — test generation checklist for a given class
- **/migrate-asp-form** — migration checklist for a Classic ASP page

### Skill (1)
A set of reference code examples that agents read at runtime to produce standards-compliant output. Located in `.github/skills/dotnet-api-standards/`.
- **dotnet-api-standards** — contains `SKILL.md` plus example files (`controller-example.cs`, `envelope-dto-example.cs`, `repository-example.cs`)

---

## 5) Do the prompt files and agents work together?

**Short answer:** In this particular project, they are **independent** tools. Prompt files and agents *can* work together in general — for example, a prompt file can reference an agent, or an agent's instructions can invoke a prompt file — but in the dotnet-react-starter, they were designed as separate workflows.

- **In this repo, prompt files do not auto-trigger agents**, and **agents do not auto-trigger prompt files**.
- Agents are selected from the **Agents dropdown**.
- Prompt files are invoked via **slash commands**.
- Agents can hand off to other agents (e.g., `api-builder` → `test-coverage`).
- The **dotnet-api-standards skill** is **referenced by agents** (not by prompt files). In practice, the `api-builder` and `asp-migrator` agent instructions explicitly tell the agent to read and follow the skill content.

---

## 6) Choose the right agent (quick flow)

### Use case A — Create new API endpoints
**Agent:** `api-builder`

**Typical flow:**
1. Select **api-builder** in Copilot Chat
2. Provide resource name, properties, and operations
3. Review the plan it proposes
4. Approve to scaffold files
5. Use handoff to `test-coverage` if desired

**Sample prompts (pick one):**
- "Create a new resource called `companies` with properties id:int, name:string, activeIndicator:bool. Implement GET all, GET by id, and POST."
- "Add an `invoices` endpoint with filters for companyId:int and status:string, plus pagination."
- "Create an API for `employees` including GET all, GET by id, and PUT."

---

### Use case B — Add or expand tests
**Agent:** `test-coverage`

**Typical flow:**
1. Select **test-coverage** in Copilot Chat
2. Ask for a coverage report and new tests
3. Review coverage gap report
4. Approve to generate tests

**Sample prompts (pick one):**
- “Analyze the backend and generate a coverage gap report, then add missing tests.”
- “Add unit tests for the controllers and services added in the last change.”
- “Focus on repository tests and include pagination and filter scenarios.”

---

### Use case C — Migrate Classic ASP to .NET + React
**Agent:** `asp-migrator`

**Typical flow:**
1. Select **asp-migrator** in Copilot Chat
2. Provide the ASP file path and target resource name
3. Review migration plan
4. Approve to scaffold backend + frontend
5. Use handoffs to `api-builder` and `test-coverage` if needed

**Sample prompts (pick one):**
- “Migrate legacy/employee-list.asp into a new `employees` API and React page.”
- “Convert the ASP form to .NET 8 + React, keep the same fields and filters.”
- “Migrate the ASP page and add tests for the new backend endpoints.”

---

## 7) Prompt files (optional alternative to agents)

Use these when you want **single-task, templated guidance** instead of a full agent workflow.

- **/create-api-endpoint** → scaffold endpoint instructions
- **/add-unit-tests** → add tests for specific classes
- **/migrate-asp-form** → step-by-step migration checklist

---

## 8) Repo layout at a glance

```
├── .devcontainer/              # Codespaces / Dev Container config
├── backend/
│   ├── src/Api/                # .NET 8 Web API
│   └── tests/Api.Tests/        # xUnit tests
├── frontend/                   # React 18 + TypeScript + Vite + Tailwind
├── legacy/                     # Classic ASP file (migration demo input)
│   └── employee-list.asp
├── docs/BUILD_INSTRUCTIONS.md  # Detailed build reference
└── .github/
    ├── copilot-instructions.md # Always-on Copilot standards
    ├── agents/                 # Custom agents (3)
    ├── prompts/                # Prompt files (3)
    └── skills/                 # Skills (1)
```

---

## 9) Quick success checklist

- [ ] Backend builds (`dotnet build`) and tests pass (`dotnet test` — 6 tests)
- [ ] Frontend builds (`npm run build`) and dev server runs (`npm run dev`)
- [ ] Swagger UI loads at `/swagger`
- [ ] You can select agents from the Agents dropdown in Copilot Chat
- [ ] You can invoke prompt files via `/` slash commands
- [ ] Agents read the `dotnet-api-standards` skill when scaffolding APIs
