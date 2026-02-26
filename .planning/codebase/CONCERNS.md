# Codebase Concerns

**Analysis Date:** 2026-02-26

## Tech Debt

**Large Agent Files - High Complexity:**
- Issue: Core planning and execution agents exceed 1000 lines, creating maintenance burden and potential for divergent behavior
- Files: `agents/gsd-planner.md` (1275 lines), `agents/gsd-debugger.md` (1246 lines)
- Impact: Difficult to test in isolation, requires comprehensive context to modify, high cognitive load for future maintainers, difficult to trace execution paths
- Fix approach: Extract reusable components into dedicated reference documents. Consider decomposing large agents into focused sub-agents with clear contracts

**Missing Test Coverage for Tool Suite:**
- Issue: No tests found for core `gsd-tools.cjs` and its 11 library modules (phase.cjs, verify.cjs, state.cjs, etc.)
- Files: `/get-shit-done/bin/gsd-tools.cjs`, `/get-shit-done/bin/lib/*.cjs`
- Impact: Regression detection relies on manual integration testing, edge cases in phase numbering/renumbering can break silently, state mutations may cascade through workflows
- Fix approach: Establish Jest/Vitest test suite covering: state transitions, frontmatter parsing, phase arithmetic, file I/O error handling, git integration edge cases

**Error Handling Consistency:**
- Issue: Tool modules use `error()` function that calls `process.exit(1)` immediately, preventing graceful error aggregation or recovery
- Files: `/get-shit-done/bin/lib/core.cjs` (error function), all `/get-shit-done/bin/lib/*.cjs` modules
- Impact: CLI failures don't allow partial success reporting, no ability to collect multiple validation errors before failing, cannot retry transient failures
- Fix approach: Implement error result type or exception-based error handling with recovery strategies for transient failures (git conflicts, file locks)

## Known Bugs & Limitations

**Phase Numbering Edge Case - Decimal Calculation:**
- Symptoms: Potential off-by-one errors when inserting phases between decimal ranges (e.g., phase 1.7 between 1 and 2)
- Files: `/get-shit-done/bin/lib/phase.cjs` (phase next-decimal, phase insert commands)
- Trigger: Inserting new phase after phase 1.9, roadmap parsing with non-standard decimal values
- Impact: Phase numbers can become inconsistent with ROADMAP.md ordering, downstream phase lookups may fail
- Workaround: Use decimal phase removal and re-addition to normalize numbering

**Silent File System Failures in Hooks:**
- Issue: Hook scripts (statusline, context-monitor, check-update) wrap all file operations in try-catch with silent failures
- Files: `/hooks/gsd-statusline.js` (lines 42-44, 68-82), `/hooks/gsd-check-update.js`, `/hooks/gsd-context-monitor.js`
- Symptoms: Context metrics not reported when tmp directory unavailable, update checks never shown if cache unreadable
- Impact: Users don't know when context monitoring fails, updates may go unnoticed, status display becomes unreliable without feedback
- Fix approach: Log silent failures to stderr or dedicated hook log file, provide visibility into hook execution health

**Large Output Handling - Temporary File Coupling:**
- Issue: `gsd-tools.cjs` outputs >50KB responses to temp files with `@file:` prefix, but no cleanup mechanism
- Files: `/get-shit-done/bin/lib/core.cjs` (lines 34-37), `/get-shit-done/bin/lib/init.cjs` (uses output), callers must detect and read @file: prefix
- Impact: Temp files accumulate over time, coupling tight between output format and consumers, consumers must handle two different output modes
- Fix approach: Implement automatic cleanup of temp files after reading, consider streaming large responses or paging

## Security Considerations

**Frontmatter Injection via Unvalidated User Input:**
- Risk: Frontmatter merge/set operations don't validate field values, users could inject YAML/JSON malformed data
- Files: `/get-shit-done/bin/lib/frontmatter.cjs` (set, merge operations)
- Current mitigation: Frontmatter functions assume valid JSON for merge, string values for set
- Recommendations: Add schema validation before writing, sanitize string values that could break YAML, validate JSON structure before merge

**No Verification of Commit Message Injection:**
- Risk: `gsd-tools.cjs commit` command directly interpolates variables into git commit messages without escaping
- Files: `/get-shit-done/bin/lib/core.cjs` (execGit function), usage in multiple agents
- Current mitigation: None detected
- Recommendations: Use git-safe escaping for all user-provided strings in commit messages, consider using git config/env vars instead of CLI args

**Config File Permissions:**
- Risk: `.planning/config.json` may contain sensitive settings (API keys via env var references) with default file permissions
- Files: `/get-shit-done/bin/lib/config.cjs` (config-ensure-section creates file with default umask)
- Current mitigation: Secrets stored in .env, config.json only references them
- Recommendations: Explicitly set file permissions to 0600 when creating .planning/config.json

**State File Integrity:**
- Risk: STATE.md directly edited by agents without atomic operations or locking, concurrent edits can corrupt state
- Files: `/get-shit-done/bin/lib/state.cjs` (read-modify-write pattern), multiple agents update STATE.md
- Current mitigation: Markdown structure expected to remain valid, but no validation after write
- Recommendations: Implement file locking or atomic write patterns (write to temp, rename), validate frontmatter after state mutations

## Performance Bottlenecks

**Agent Context Window Usage - Large Markdown Files:**
- Problem: Agent instructions themselves are large (1275 lines for planner), consuming 20-30% of context before execution starts
- Files: `agents/gsd-planner.md`, `agents/gsd-executor.md`, `agents/gsd-debugger.md`
- Cause: Detailed specifications, examples, templates included inline in agent definitions
- Improvement path: Extract reference material to separate `.planning/references/` documents, load selectively. Use front-loading for critical patterns only in agent spec

**Roadmap Parsing - Linear Search:**
- Problem: Phase lookups iterate through entire ROADMAP.md structure sequentially
- Files: `/get-shit-done/bin/lib/roadmap.cjs` (get-phase, analyze operations)
- Cause: No indexing of phase headers before processing
- Improvement path: Build phase directory index on load, cache for repeated lookups

**File System I/O in Hot Paths:**
- Problem: `gsd-tools.cjs` may read STATE.md, ROADMAP.md, and config.json multiple times per command
- Files: `/get-shit-done/bin/lib/state.cjs`, `/get-shit-done/bin/lib/roadmap.cjs`, `/get-shit-done/bin/lib/config.cjs`
- Cause: No memoization or caching across function calls
- Improvement path: Load and cache config/state once per CLI invocation, invalidate selectively

**Grep-Based Verification:**
- Problem: Plan verification uses regex matching to find task elements, scales poorly with large plans (>50 tasks)
- Files: `/get-shit-done/bin/lib/verify.cjs` (plan-structure command)
- Cause: Pattern matching over full file content repeatedly
- Improvement path: Parse PLAN.md once into AST, query AST for verification rules

## Fragile Areas

**State Machine Edge Cases - Plan Completion:**
- Files: `/get-shit-done/bin/lib/state.cjs` (advance-plan, state progression), `/agents/gsd-executor.md` (state updates section)
- Why fragile: Completing plans involves: incrementing counter, detecting last-plan, updating progress, updating ROADMAP, triggering next phase. Any step failure leaves inconsistent state
- Safe modification: Always use atomic batch operations (multiple fields in single write), validate state invariants after each transition, test edge cases (last plan, phase boundaries, missing files)
- Test coverage: No unit tests for state machine transitions detected

**Frontmatter Format Assumptions:**
- Files: Multiple agents that read and modify frontmatter in PLAN.md, SUMMARY.md, verification reports
- Why fragile: Format parsed with regex patterns, inconsistent indentation breaks parsing, YAML field ordering expected to be stable
- Safe modification: Always use frontmatter.cjs library instead of manual regex parsing, validate schema after modifications, use strict parsing (fail on unexpected format)
- Test coverage: Template validation exists but edge cases (nested arrays, special characters) untested

**Checkpoint Continuation Logic:**
- Files: `/agents/gsd-executor.md` (continuation_handling section), checkpoint_protocol section
- Why fragile: Resuming after checkpoint requires: matching previous commit hashes, detecting checkpoint type, applying correct resume logic based on type, preventing task re-execution
- Safe modification: Add explicit checkpoint state tracking (location in SUMMARY.md), validate all required context before continuing, test continuation paths for each checkpoint type
- Test coverage: Checkpoint flow documented but no integration tests

**Wave Computation:**
- Files: `/agents/gsd-planner.md` (assign_waves section), `/get-shit-done/bin/lib/phase.cjs` (phase-plan-index)
- Why fragile: Wave numbers calculated from dependency graph but graph built manually by planner, typos in depends_on break wave computation
- Safe modification: Validate depends_on references before saving PLAN.md, compute waves in gsd-tools instead of planner, verify no circular dependencies
- Test coverage: Wave validation exists in checker but prevents problematic plans rather than helping construct correct ones

## Scaling Limits

**Single File Roadmap:**
- Current capacity: ~100-200 phases before manual navigation becomes difficult
- Limit: Where it breaks: Phase header parsing becomes O(n), phase arithmetic errors compound with complexity
- Scaling path: Split into multiple ROADMAP files by milestone, build phase registry index on load, implement hierarchical phase lookup

**State File Growth:**
- Current capacity: ~50KB (can grow with decisions, issues, sessions)
- Limit: Where frontmatter parsing becomes slow and large state becomes context-expensive to pass between agents
- Scaling path: Archive old decisions/sessions periodically, keep active state separate from history, implement state versioning

**Module File Size:**
- Current capacity: Largest tool module is phase.cjs at 871 lines
- Limit: Where individual file understanding becomes difficult, testing complexity increases, changes have high risk
- Scaling path: Split phase.cjs into phase-crud.cjs (create/delete), phase-compute.cjs (arithmetic), phase-index.cjs (lookup)

## Dependencies at Risk

**No Version Pinning in gsd-tools Dependencies:**
- Risk: Uses standard node modules (fs, path, child_process) with no explicit versions, relies on Node.js environment
- Impact: Changes in Node.js versions could affect behavior (execSync differences, path normalization changes)
- Migration plan: Test against Node.js LTS versions, document minimum version requirement

**Markdown Parsing Fragility:**
- Risk: Frontmatter and task parsing relies on regex patterns without structured parser
- Impact: Markdown syntax variations break parsing, future PLAN.md format changes require regex updates
- Migration plan: Implement proper YAML/Markdown parser library (yaml, gray-matter, remark), create migration guide for format changes

**Hard-Coded Model Profile Table:**
- Risk: MODEL_PROFILES in core.cjs maps agents to specific Claude models, new agents require code change
- Impact: Can't add new agents or change model assignments without modifying core.cjs and redeploying
- Migration plan: Move model profiles to config.json, allow runtime model reassignment, implement model compatibility checking

## Missing Critical Features

**No Verification Before Committing:**
- Problem: Plans are committed to git without structural validation in main flow
- Blocks: Can't detect malformed plans until execution fails, checklist not enforced before publish
- Impact: Downstream executors fail on plan structure errors instead of planner fixing them
- Solution: Add pre-commit validation hook or explicit verification step before plan commit

**No Idempotency Tracking for Tasks:**
- Problem: If executor crashes mid-task, resuming re-executes partial task
- Blocks: Can't safely resume without risking duplicate work (duplicate git commits, duplicate file creates)
- Impact: Checkpoint continuation could silently create duplicates
- Solution: Implement task transaction log, track completion boundaries, enable safe resumption

**No Built-In Plan Versioning:**
- Problem: Modifying PLAN.md after execution started breaks executor assumptions
- Blocks: Can't update plans in-flight, can't handle mid-execution requirement changes
- Impact: If plan discovered to have issues during execution, no safe way to patch it
- Solution: Add plan versioning to frontmatter, track versions in STATE.md, implement three-way merge for changes

**No Test Template Generation:**
- Problem: Executor requires tests to exist but doesn't help create test scaffolds
- Blocks: TDD tasks fail if no test framework configured in project
- Impact: Executor deviation rules miss detecting missing test infrastructure
- Solution: Add test scaffold creation to TDD task handling, detect test framework from package.json/imports

## Test Coverage Gaps

**Tool Module Testing - No Tests:**
- What's not tested: gsd-tools.cjs CLI commands (50+ commands), frontmatter operations, phase arithmetic, state transitions, git integration
- Files: `/get-shit-done/bin/gsd-tools.cjs`, `/get-shit-done/bin/lib/*.cjs`
- Risk: Regressions in core workflows undetected, edge cases (empty files, missing directories) crash at runtime
- Priority: **High** — impacts all GSD workflows, prevents safe refactoring

**Hook Error Handling - No Tests:**
- What's not tested: Error conditions in statusline (no tmp dir), context-monitor (file system full), check-update (network failure)
- Files: `/hooks/gsd-*.js`
- Risk: Hooks fail silently in production, users don't know when monitoring breaks
- Priority: **Medium** — affects user experience but not core workflow

**Agent Checkpoint Logic - No Tests:**
- What's not tested: Checkpoint resumption for each type (human-verify, decision, human-action), auth gate creation, completion detection
- Files: `agents/gsd-executor.md` (checkpoint_protocol section, continuation_handling)
- Risk: Checkpoint workflows hit untested paths, resumption could fail or skip tasks
- Priority: **High** — blocks human-interactive flows, single-path coverage only

**Verification Rules - Partial Testing:**
- What's not tested: Complex wiring verification (dynamic state, circular dependencies), artifacts with glob patterns, key-links across renamed files
- Files: `/get-shit-done/bin/lib/verify.cjs` (key-links, artifacts verification)
- Risk: False positives/negatives in verification prevent good plans or allow bad ones
- Priority: **Medium** — checker catches some issues but not all

**Wave Computation - No Tests:**
- What's not tested: Circular dependency detection, transitive dependency calculation, wave assignment with complex graphs
- Files: `/agents/gsd-planner.md` (build_dependency_graph, assign_waves), `/get-shit-done/bin/lib/phase.cjs` (phase-plan-index)
- Risk: Plans with subtle dependency errors pass checker but fail executor
- Priority: **High** — silent failures create cascading execution blocks

---

*Concerns audit: 2026-02-26*
