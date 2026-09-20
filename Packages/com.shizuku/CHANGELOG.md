# Changelog

## Unreleased

## [0.9.2] - 2026-09-21

### Fixed

- Restored Unity's native grouped search tree for ordinary node creation
- Removed input-source navigation buttons from connected parameter ports

## [0.9.1] - 2026-09-20

### Fixed

- Git-installed packages now include the MCP Server project file required by the one-time build
- MCP Server setup now reports an incomplete package before invoking `dotnet publish`

## [0.9.0] - 2026-09-19

### Added

- Purpose/keyword search with recent nodes and compatible-port filtering, including create-and-connect from an empty-space port drop
- `NodeField` metadata for labels, units, summaries and required-field checks; node descriptions and field tooltips in the Inspector
- Input-source navigation, inline configuration warnings and a current-graph configuration check action
- Graph asset schema versioning, ordered automatic migration and detailed missing managed-reference diagnostics

### Changed

- Serialized node fields and input defaults use native Unity property editors, including inherited private fields and custom drawers
- Graph Editor and MCP graph mutations share a stateless operation layer; Unity Undo remains the only edit-history stack
- Undo/Redo covers node configuration, dynamic-port callback side effects, graph edits, variables and methods; open views refresh after restoration
- Generated identifiers and structural port collections are hidden from normal configuration; node identity remains available in read-only debug information
- Menu paths, serialized field names and port connection keys remain stable
- Unversioned v0.3.0-v0.8.0 graph assets migrate to schema version 1 when opened, then save and reimport automatically
- Renamed directional Tag cancellation rules to exclusion rules in the runtime API and TagConfig Inspector
- `TagCollection.TryAdd` is now the single rule-aware add path and applies blocking and directional exclusion atomically

### Fixed

- Runtime initialization no longer removes unresolved `SerializeReference` entries, preserving their serialized data for type recovery or explicit migration

## [0.8.0] - 2026-09-13

### Added

- Project-scoped stdio MCP server backed by the official Model Context Protocol C# SDK
- Loopback-only Unity Editor bridge with rotating project-local authentication tokens
- Semantic graph list, node catalog, read, validate and transactional apply MCP tools
- Project Settings workflow for building the server and configuring Codex, Claude Code and Cursor

### Changed

- Removed the hard-coded AI assistant mock UI from the Graph Editor

## [0.7.0] - 2026-09-13

### Added

- Public `ShizukuGraphRuntime<TGraph>` owner for cloning, initializing, executing, ticking and disposing runtime graph instances
- Graph Editor toolbar actions for creating nodes and framing the complete graph

### Changed

- `GraphRunner`, `BlueprintBehavior` and Timeline `GraphClipHandler` now share the unified runtime graph lifecycle
- Graph nodes, ports, selection states and canvas styling use a clearer compact visual system
- Initial graph framing waits for stable UI Toolkit layout while ordinary refreshes preserve the current viewport

### Fixed

- Multi-output control-flow port alignment
- Parameter and control-flow edges remaining visible or serialized after deleting their connected node
- Duplicate Blueprint editor extensions and side panels after repeatedly opening a Blueprint graph
- Timeline graph execution bypassing root and latent scheduling semantics

## [0.6.0] - 2026-09-12

### Added

- Public `IDynamicParameterPortProvider` and `DynamicParameterPortDescriptor` contracts for custom nodes with collection-backed input or output ports
- Public `INodeSerializedFieldChangeHandler` callback for synchronizing derived node structure after Inspector field writeback

### Changed

- Dynamic ports are synchronized generically for root graphs, method graphs, view refreshes, and runtime initialization
- AssetReference and other editable node fields share the same change notification and coalesced delayed view refresh path
- `PlayTimelineNode` now uses the generic extension contracts while preserving Track edge migration and removal behavior

## [0.5.1] - 2026-09-12

### Added

- Generic node Inspector editing for Addressables `AssetReference` subclasses through the native Addressables property drawer

### Changed

- AssetReference values now write through the graph asset's serialized property and mark the graph dirty without adding an Addressables dependency to the distributed package

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
