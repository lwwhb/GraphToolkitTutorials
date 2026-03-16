# GraphToolkit API Migration Required

## Problem
This project was written for an older version of GraphToolkit, but Unity 6000.5.0a8 has significantly different APIs.

## Major API Changes Detected

### 1. IPort.Node Property - REMOVED
**Old API:**
```csharp
var node = port.Node;
```

**New API:** Unknown - need to find alternative
- Possibly: Graph needs to track port-to-node mapping
- Or: Ports may need to be accessed through INode methods

### 2. Graph.Nodes Property - CHANGED
**Old API:**
```csharp
foreach (var node in graph.Nodes) { }
```

**New API:**
```csharp
// Use GetNodes() method instead
foreach (var node in graph.GetNodes()) { }
```

### 3. Graph.Connections Property - REMOVED
**Old API:**
```csharp
foreach (var connection in graph.Connections) { }
```

**New API:** Unknown - need to find alternative
- Possibly: Use IPort.GetConnectedPorts()
- Or: Graph.Connect/Disconnect tracking

### 4. AddOption() Signature - CHANGED
**Old API:**
```csharp
context.AddOption("Label", () => m_Field, v => m_Field = v).Build();
```

**New API:** Need to check IOptionDefinitionContext.AddOption signature

### 5. IVariable Properties - CHANGED
**Old API:**
```csharp
variable.Guid
variable.Type
variable.Kind
variable.Value
```

**New API:** Need to check IVariable interface

### 6. Node Properties - CHANGED
**Old API:**
```csharp
node.Name
node.Ports
```

**New API:**
```csharp
// Use INode.Title instead of Name?
// Use INode.GetInputPorts() / GetOutputPorts() instead of Ports?
```

### 7. PortCapacity - Access Changed
**Old API:**
```csharp
.WithCapacity(PortCapacity.Multiple)
```

**New API:** PortCapacity enum is now inaccessible or renamed

### 8. Execute() Method - Signature Changed
**Old API (URP):**
```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
```

**New API:** Method signature may have changed in Unity 6

## Affected Files Count
- **200+ compilation errors** across all tutorials
- **All 11 tutorials** need updates
- **~9,750 lines of code** need review

## Recommendation

Given the scope of changes, we have three options:

### Option 1: Complete Rewrite (Recommended)
- Start fresh with Unity 6000.5.0a8 GraphToolkit API
- Use official Unity samples as reference
- Rewrite tutorials 1-11 from scratch
- Estimated time: 40-60 hours

### Option 2: Systematic Migration
- Document all API changes from Unity docs
- Create migration scripts where possible
- Update each tutorial one by one
- Estimated time: 30-40 hours

### Option 3: Downgrade Unity
- Use Unity 6000.0.5a8 (the version specified in README)
- Current code should work without changes
- No migration needed
- Estimated time: 0 hours

## Next Steps

Please decide which option you prefer:
1. Complete rewrite for Unity 6000.5.0a8
2. Systematic migration
3. Downgrade to Unity 6000.0.5a8

If choosing option 1 or 2, I'll need to:
1. Extract complete API documentation from Unity 6000.5.0a8
2. Create a comprehensive API mapping document
3. Start migrating tutorials one by one
