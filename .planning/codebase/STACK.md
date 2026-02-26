# Technology Stack

**Analysis Date:** 2026-02-26

## Languages

**Primary:**
- JavaScript (Node.js) - CLI hooks and status monitoring in `.claude/hooks/`
- Markdown - Documentation and specifications in `EventCenter/docs/`

**Secondary:**
- TypeScript - Configuration files (Vite configs referenced in project structure)

## Runtime

**Environment:**
- Node.js (version not explicitly specified, minimum v12 for spawn/child_process usage)

**Package Manager:**
- npm - Used for package management and GSD framework distribution
- Lockfile: Not present in analyzed directories

## Frameworks

**Core:**
- GSD (Get Shit Done) CLI Framework v1.21.0 - Agent orchestration and task management
  - Location: `.claude/get-shit-done/`
  - Purpose: Multi-phase project planning and execution framework

**Build/Dev:**
- Vite - Frontend build tool (referenced in `ProjekteGIT/compose/kausalassist-apps/data/fma/vite.*.config.ts`)
  - Two configuration files: `vite.dashboard.config.ts`, `vite.frontend.config.ts`

## Key Dependencies

**Critical:**
- Node.js child_process module - Process spawning for background tasks in `.claude/hooks/gsd-check-update.js`
- Node.js fs module - File I/O and cache management
- Node.js path module - Cross-platform file path handling
- Node.js os module - Temporary directory and home directory resolution

**Infrastructure:**
- npm package registry - For GSD version checking (`npm view get-shit-done-cc version`)
- Claude Code API/Context Management - For session tracking and context window monitoring

## Configuration

**Environment:**
- Session-based configuration via context window data structures
- Hook-based configuration in `.claude/settings.json`:
  - SessionStart hooks: Version update checking
  - PostToolUse hooks: Context monitoring and warnings

**Build:**
- Vite configuration files in `ProjekteGIT/compose/kausalassist-apps/data/fma/`:
  - `vite.dashboard.config.ts` - Dashboard build configuration
  - `vite.frontend.config.ts` - Frontend build configuration
- TypeScript configuration files (referenced but not analyzed in detail)

**CLI Configuration:**
- `.claude/package.json` - Declares CommonJS module type
- `.claude/settings.json` - Hook definitions and status line configuration

## Platform Requirements

**Development:**
- Node.js runtime (v12+)
- npm package manager
- POSIX-compatible shell (for CLI execution)
- Temporary directory access (for context metrics and warning state files)

**Production:**
- Claude Code runtime environment (for agent execution)
- Anthropic Claude API access (v4.5 or compatible)
- Session and workspace context management

## Special Configuration Files

**Hooks System:**
- `.claude/hooks/gsd-statusline.js` - Displays model, task, directory, and context usage (10-segment progress bar with color coding)
- `.claude/hooks/gsd-check-update.js` - Background version update checker, writes to `.claude/cache/gsd-update-check.json`
- `.claude/hooks/gsd-context-monitor.js` - PostToolUse hook that monitors context usage and injects warnings (debounced at 5 tool uses)

**Cache/State:**
- `$TMPDIR/claude-ctx-{session_id}.json` - Context metrics bridge file (updated by statusline hook)
- `$TMPDIR/claude-ctx-{session_id}-warned.json` - Warning debounce tracking
- `.claude/cache/gsd-update-check.json` - Update availability cache

---

*Stack analysis: 2026-02-26*
