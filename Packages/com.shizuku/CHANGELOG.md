# Changelog

## [0.4.0] - 2026-09-09

### Added

- Graph clipboard support for selected nodes and their internal edges
- Lifecycle-safe Timeline playback node with dynamic Track binding ports
- Scene-object variable references and Inspector binding
- Project Settings toggle for the optional `SHIZUKU_TAG` module
- Project-scoped output paths for Blueprint and generated node source files

### Changed

- Unified node menu paths and search names
- Strengthened variable name, type, value and duplicate-name validation
- Moved automated tests outside the distributed UPM package

### Fixed

- Graph edge persistence and graph-window state across Play Mode transitions
- Timeline playback lifecycle and non-preemptive repeated execution
- Stale Blueprint event arguments on parameterless events
- Blueprint code generation for behavior types declared in user namespaces
- DebugKit bootstrap on a fresh package installation

完整历史记录见 [仓库 CHANGELOG](https://github.com/zjulzy/Shizuku/blob/main/CHANGELOG.md)。
