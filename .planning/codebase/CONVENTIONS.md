# Coding Conventions

**Analysis Date:** 2026-02-26

## Naming Patterns

**Files:**
- Kebab-case for hook files: `gsd-statusline.js`, `gsd-context-monitor.js`, `gsd-check-update.js`
- Configuration files use camelCase for content names but kebab-case for file naming: `vite.dashboard.config.ts`

**Functions:**
- Named functions not found in primary codebase; instead, code uses inline function calls and anonymous functions
- Callback functions declared inline using arrow function syntax: `(data) => { /* handler */ }`
- Event handlers use descriptive names derived from the event: `process.stdin.on('end', () => { })`

**Variables:**
- camelCase for all variable declarations: `homeDir`, `cacheFile`, `sessionId`, `metricsPath`, `isCritical`
- Constants use UPPER_SNAKE_CASE: `WARNING_THRESHOLD`, `CRITICAL_THRESHOLD`, `STALE_SECONDS`, `DEBOUNCE_CALLS`
- Short-lived or loop variables use single letter names only in constrained scopes
- Meaningful names preferred: `remaining` instead of `r`, `filled` instead of `f`

**Types:**
- No TypeScript in hook files; JavaScript used exclusively
- Configuration files use TypeScript: `vite.dashboard.config.ts`
- Objects use camelCase for property keys: `remaining_percentage`, `session_id`, `lastLevel`
- JSON keys from external sources preserve original casing: `remaining_percentage`, `update_available`

## Code Style

**Formatting:**
- No automated formatter detected; manual formatting observed
- 2-space indentation used throughout
- Lines kept reasonably short (70-100 character typical length)
- Semicolons always used at statement ends

**Linting:**
- No ESLint or similar linting configuration detected
- No `.eslintrc` files found
- Code follows implicit patterns observed in existing hooks

## Import Organization

**Order:**
1. Built-in Node.js modules first: `const fs = require('fs');`, `const path = require('path');`, `const os = require('os');`
2. Destructured imports follow: `const { spawn } = require('child_process');`
3. No third-party npm imports in main hook code

**Path Aliases:**
- No path aliases detected or used
- Absolute paths preferred where needed: `path.join(homeDir, '.claude', 'todos')`

## Error Handling

**Patterns:**
- Silent failure is preferred for non-critical operations
- Empty catch blocks with `catch (e) {}` pattern: used to suppress errors when failure is recoverable
- Exit codes used for process termination: `process.exit(0)` for successful graceful exits
- Try-catch wrapping used around file system operations and JSON parsing
- Defensive checks precede file operations: `if (fs.existsSync(filePath))` before read/write
- No custom Error classes defined; only standard JavaScript try-catch

**Example Pattern:**
```javascript
try {
  const data = JSON.parse(input);
  // Process data
} catch (e) {
  // Silent fail - don't break statusline on parse errors
}
```

## Logging

**Framework:** Console used via process.stdout/process.stderr

**Patterns:**
- `process.stdout.write()` for output: `process.stdout.write(\`${message}\`)`
- ANSI escape codes used for terminal colors: `\x1b[32m` (green), `\x1b[33m` (yellow), `\x1b[31m` (red)
- No logging framework or Winston/Pino detected
- Output is minimal; only critical info written to stdout
- Newlines not added by default (using `write()` instead of `console.log()`)

## Comments

**When to Comment:**
- Comments explain the "why" rather than the "what"
- File header comments document purpose: `// Claude Code Statusline - GSD Edition`
- Inline comments explain non-obvious logic or thresholds
- Magic numbers documented: `const filled = Math.floor(used / 10); // Build progress bar (10 segments)`

**JSDoc/TSDoc:**
- Not used in hook files
- No JSDocs or type annotations in JavaScript code
- TypeScript config files may have comments but no JSDoc patterns

**Example Comments:**
```javascript
// Context window display (shows USED percentage scaled to 80% limit)
// Claude Code enforces an 80% context limit, so we scale to show 100% at that point

// Silent fail -- bridge is best-effort, don't break statusline

// Severity escalation bypasses debounce (WARNING -> CRITICAL fires immediately)
```

## Function Design

**Size:**
- Functions kept concise, generally 5-20 lines for event handlers
- Complex logic organized into logical blocks with explanatory comments
- No deeply nested logic (max 2-3 levels typical)

**Parameters:**
- Event handlers accept single parameter: `(data) => {}`
- Process-level stdin handlers use closure patterns
- Path construction uses multiple arguments: `path.join(dir1, dir2, file)`

**Return Values:**
- Event handlers use `process.exit()` for control flow rather than return values
- Functions writing output use `process.stdout.write()` for side effects
- No explicit return statements in event handlers (control flow via exit codes)

## Module Design

**Exports:**
- No explicit exports in hook files
- Files are standalone scripts executed via shebang: `#!/usr/bin/env node`
- Code uses `require()` for Node.js CommonJS modules

**Barrel Files:**
- Not applicable; each file is an independent hook script
- Configuration aggregation via environment variables and file system paths

## File System Patterns

**Path Construction:**
- Always use `path.join()` for cross-platform compatibility, never string concatenation
- Home directory accessed via `os.homedir()`
- Temporary directory accessed via `os.tmpdir()`
- File existence checked before operations: `fs.existsSync()`

**Synchronous Operations:**
- Synchronous file operations used: `fs.readFileSync()`, `fs.writeFileSync()`
- No async/await patterns observed
- JSON serialization uses `JSON.parse()` and `JSON.stringify()`

---

*Convention analysis: 2026-02-26*
