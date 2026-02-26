# External Integrations

**Analysis Date:** 2026-02-26

## APIs & External Services

**Anthropic Claude API:**
- Integration type: Primary agent runtime
- SDK/Client: Claude Code CLI (built-in)
- Usage: All agent execution, context management, model selection
- Context passed as: `data.model.display_name`, `data.context_window`, `data.session_id`

**npm Registry:**
- Service: Package registry for GSD framework
- What it's used for: Version checking and updates
  - Checks: `npm view get-shit-done-cc version`
  - Location: `gsd-check-update.js` runs on SessionStart
  - Result stored in: `.claude/cache/gsd-update-check.json`

## Data Storage

**Databases:**
- None detected in core codebase

**File Storage:**
- Local filesystem only
  - Home directory cache: `~/.claude/cache/`
  - Temporary files: `/tmp/claude-ctx-*.json`
  - Project-local GSD framework: `.claude/get-shit-done/`

**Caching:**
- File-based caching in `.claude/cache/`
  - `gsd-update-check.json` - Stores npm version check results
  - No TTL on cache files (persistence until manual cleanup)

## Authentication & Identity

**Auth Provider:**
- Implicit via Claude Code runtime
- Session tracking: `session_id` passed from Claude Code to hooks
- No explicit API key management detected

**Session Management:**
- Session ID passed to all hooks via stdin JSON
- Used to:
  - Create isolated context metrics files (`claude-ctx-{session_id}.json`)
  - Track warning state per session
  - Display in statusline for user identification

## Monitoring & Observability

**Error Tracking:**
- None detected

**Logs:**
- Console output from statusline hook (silent on errors, returns formatted status)
- Hook output directed to stdout (JSON formatted for PostToolUse hook)
- Error states logged silently to prevent breaking tool execution

**Context Monitoring:**
- Custom implementation in `.claude/hooks/gsd-context-monitor.js`
  - Tracks remaining context percentage
  - Triggers warnings at 35% remaining (WARNING), 25% remaining (CRITICAL)
  - Debounced at 5 tool uses between warnings
  - Severity escalation (WARNING → CRITICAL) bypasses debounce

## CI/CD & Deployment

**Hosting:**
- GSD framework distributed via npm: `get-shit-done-cc`
- Deployed as project-local copy in `.claude/get-shit-done/`

**CI Pipeline:**
- Version update checking integrated into SessionStart hook
- Statusline hook on every prompt (built-in to Claude Code)
- PostToolUse hook on every tool execution

**Distribution:**
- npm package registry (`npm view get-shit-done-cc version`)
- Installed locally in project directory

## Environment Configuration

**Required env vars:**
- None explicitly required (all data passed via stdin JSON and environment APIs)

**System paths expected:**
- `$HOME/.claude/` - User configuration directory
- `$TMPDIR` or `/tmp/` - Temporary directory for metrics files
- Current working directory (`process.cwd()`) - For project detection

**Secrets location:**
- No secrets detected in codebase
- No .env files present or referenced
- Authentication handled implicitly by Claude Code runtime

## Hooks & Callbacks

**Incoming:**
- SessionStart hook: Triggered on session initialization
- PostToolUse hook: Triggered after each tool execution
- Statusline hook: Invoked on every prompt display

**Outgoing:**
- No external webhooks detected
- Internal hook communication via:
  - Stdout JSON (PostToolUse hook output)
  - File-based bridge files (`/tmp/claude-ctx-{session_id}.json`)
  - Cache files (`.claude/cache/gsd-update-check.json`)

## Integration Patterns

**Hook Communication:**
- Statusline hook → Context bridge file (metrics update)
- Context monitor hook → Reads bridge file → Outputs JSON to stdout
- Debounce state → Temporary file tracking (`claude-ctx-{session_id}-warned.json`)

**Version Management:**
- Check on SessionStart (background spawn, non-blocking)
- Cache result with timestamp for status display
- Statusline hook reads cache and displays update notification if available

---

*Integration audit: 2026-02-26*
