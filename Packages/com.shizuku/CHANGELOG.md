# Changelog

## [0.5.0] - 2026-09-10

### Added

- Latent node execution model for multi-frame Graph and void Blueprint Event control flow
- Timeline control-flow outputs for immediate start, completion and failure
- Project-scoped output paths for Blueprint and generated node source files

### Changed

- Timeline playback is now a lifecycle-owned latent operation; active re-entry reports an error without interrupting playback
- ShizukuMethod and return-valued Blueprint Events explicitly reject latent execution

### Fixed

- Root re-entry while a latent execution is pending
- Blueprint Event parameter overwrite when a reachable latent node rejects re-entry
- Blueprint code generation for behavior types declared in user namespaces
- Debugger resume semantics for root-owned latent execution

## [0.4.0] - 2026-09-09

### Added

- Graph clipboard support for selected nodes and their internal edges
- Lifecycle-safe Timeline playback node with dynamic Track binding ports
- Scene-object variable references and Inspector binding
- Project Settings toggle for the optional `SHIZUKU_TAG` module

### Changed

- Unified node menu paths and search names
- Strengthened variable name, type, value and duplicate-name validation
- Moved automated tests outside the distributed UPM package

### Fixed

- Graph edge persistence and graph-window state across Play Mode transitions
- Timeline playback lifecycle and non-preemptive repeated execution
- Stale Blueprint event arguments on parameterless events
- DebugKit bootstrap on a fresh package installation

完整历史记录见 [仓库 CHANGELOG](https://github.com/zjulzy/Shizuku/blob/main/CHANGELOG.md)。
