# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 (6000.0.5a8+) project containing a complete GraphToolkit tutorial series — 10 tutorials progressing from basic graph concepts to a fully graph-driven URP rendering pipeline. All code is in C# targeting the Unity Editor and Runtime.

**GraphToolkit** is a Unity 6 internal module (`UnityEditor.GraphToolkitModule.dll`) under the namespace `Unity.GraphToolkit.Editor`. It is not a package — it ships as a built-in DLL and has no source code in this repo.

## Unity-Specific Development Notes

There is no build CLI for Unity projects. All compilation and testing happens inside the Unity Editor:
- Open the project in Unity 6000.0.5a8 or later
- Unity compiles C# automatically on file save
- Check the Console window for compile errors
- Use Window → Analysis → Frame Debugger to debug render passes
- Use Window → Analysis → Profiler for performance analysis

Assembly definitions (`.asmdef`) control compilation boundaries. Each tutorial has its own asmdef(s) — editor-only assemblies reference `Unity.GraphToolkit.Editor`; runtime assemblies must not.

## Architecture

### Two Graph Paradigms

**Data Flow (Pull model)** — tutorials 1, 2, 4, 5, 6:
- Nodes implement an evaluator interface (e.g. `ICalculatorNode`, `ITextureNode`)
- The Graph class exposes an `EvaluatePort(IPort)` method
- Evaluation is recursive: each node pulls values from its connected input ports
- No runtime/editor split needed — evaluation happens entirely in the editor (ScriptedImporter)

**Execution Flow (Push model)** — tutorials 3, 7, 8, 9, 10:
- Editor nodes define the graph structure and serialize to runtime nodes via `CreateRuntimeNode(graph)`
- Runtime nodes are plain `[Serializable]` data classes with index references (no Unity Editor API)
- A MonoBehaviour runner (or `ScriptableRendererFeature`) traverses the runtime graph and dispatches to executor classes
- Executor pattern: each node type has a corresponding executor that contains the actual logic

### Editor/Runtime Split Pattern

Every execution-flow tutorial follows this structure:
```
Editor/
  XxxGraph.cs          ← [Graph("ext")] attribute, CreateRuntime() method
  Nodes/XxxNode.cs     ← inherits Node, implements CreateRuntimeNode()
  XxxImporter.cs       ← [ScriptedImporter] converts .ext asset → runtime ScriptableObject
Runtime/
  XxxRuntimeGraph.cs   ← plain ScriptableObject, list of runtime nodes
  XxxRuntimeNode.cs    ← abstract base, [Serializable], int indices for connections
  Nodes/RuntimeNodes.cs← concrete runtime node data
  IXxxExecutor.cs      ← executor interface
  Executors/           ← one executor per node type
  XxxRunner.cs         ← MonoBehaviour, coroutine-driven execution loop
```

### Key GraphToolkit API Patterns

```csharp
// Graph definition
[Graph("fileext", GraphOptions.None)]
internal class MyGraph : Graph { }

// Node definition
[Node("Display Name", "Category/Subcategory")]
[UseWithGraph(typeof(MyGraph))]
internal class MyNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("In").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        context.AddOutputPort<float>("Value").Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Label", () => m_Field, v => m_Field = v).Delayed().Build();
    }
}

// Traversing connections (editor-side)
var connectedPort = graph.GetConnectedInputPort(outputPort);   // output→next node
var connectedPort = graph.GetConnectedOutputPort(inputPort);   // input←previous node
```

### URP Integration (tutorials 9–10)

`GraphDrivenURPFeature` / `GraphDrivenRendererFeature` extend `ScriptableRendererFeature`. On `Create()`, the editor graph asset is converted to a runtime graph and traversed to build a `List<ScriptableRenderPass>`. Branch nodes (`QualityBranchNode`, `PlatformBranchNode`) are resolved at `Create()` time using `QualitySettings.GetQualityLevel()` and platform defines. The feature must be added to a URP Renderer asset via the Inspector.

## File Locations

| Path | Purpose |
|------|---------|
| `Assets/Tutorials/0N_*/Editor/` | Editor-only graph and node definitions |
| `Assets/Tutorials/0N_*/Runtime/` | Runtime data, executors, MonoBehaviours |
| `Assets/Documentation/` | 13 Markdown tutorial docs (Chinese) |
| `Assets/Settings/` | URP pipeline assets (PC deferred + Mobile forward) |
| `Assets/Common/` | Shared utilities |

## Coding Conventions

- All tutorial namespaces: `GraphToolkitTutorials.<TutorialName>` (editor) and `GraphToolkitTutorials.<TutorialName>.Runtime` (runtime)
- Editor node classes are `internal`; runtime classes used by MonoBehaviours are `public`
- Node connection indices use `-1` to mean "not connected"
- `[SerializeField] private` for all node data fields
- Options use `.Delayed().Build()` for string/numeric fields to avoid rebuilding on every keystroke
