# Unity 6000.5.0a8 GraphToolkit API Migration Guide

## Key API Changes

### 1. Graph.Nodes → Graph.GetNodes()
```csharp
// OLD
foreach (var node in graph.Nodes) { }

// NEW
foreach (var node in graph.GetNodes()) { }
```

### 2. Graph.Connections → REMOVED
Use IPort.GetConnectedPorts() instead:
```csharp
// OLD
foreach (var connection in graph.Connections) { }

// NEW - iterate through nodes and their ports
foreach (var node in graph.GetNodes())
{
    foreach (var port in node.GetOutputPorts())
    {
        foreach (var connectedPort in port.GetConnectedPorts())
        {
            // Process connection
        }
    }
}
```

### 3. IPort.Node → REMOVED
Use INode.Graph to get graph, then find node by iterating:
```csharp
// OLD
var node = port.Node;

// NEW - Need to find node that owns this port
INode FindNodeForPort(Graph graph, IPort port)
{
    foreach (var node in graph.GetNodes())
    {
        foreach (var p in node.GetInputPorts())
            if (p == port) return node;
        foreach (var p in node.GetOutputPorts())
            if (p == port) return node;
    }
    return null;
}
```

### 4. Node.Name → Node.Title
```csharp
// OLD
string name = node.Name;

// NEW
string title = node.Title;
```

### 5. Node.Ports → Node.GetInputPorts() / GetOutputPorts()
```csharp
// OLD
foreach (var port in node.Ports) { }

// NEW
foreach (var port in node.GetInputPorts()) { }
foreach (var port in node.GetOutputPorts()) { }
```

### 6. IVariable Properties Changed
```csharp
// OLD
variable.Guid → REMOVED
variable.Type → variable.DataType
variable.Kind → variable.VariableKind
variable.Value → variable.TryGetDefaultValue() / TrySetDefaultValue()
```

### 7. Graph.Variables → Graph.GetVariables()
```csharp
// OLD
foreach (var v in graph.Variables) { }

// NEW
foreach (var v in graph.GetVariables()) { }
```

### 8. AddOption() - Need to check exact signature
The method exists but signature may have changed.

### 9. Graph.GetConnectedOutputPort() → Use IPort.FirstConnectedPort
```csharp
// OLD
var outputPort = graph.GetConnectedOutputPort(inputPort);

// NEW
var outputPort = inputPort.FirstConnectedPort;
// Or for multiple connections:
foreach (var connectedPort in inputPort.GetConnectedPorts()) { }
```

### 10. Graph.GetConnectedInputPort() → Use IPort.FirstConnectedPort
```csharp
// OLD
var inputPort = graph.GetConnectedInputPort(outputPort);

// NEW
var inputPort = outputPort.FirstConnectedPort;
// Or for multiple connections:
foreach (var connectedPort in outputPort.GetConnectedPorts()) { }
```

## Migration Strategy

1. Fix Graph.Nodes → GetNodes() (simple find/replace)
2. Fix Graph.Variables → GetVariables() (simple find/replace)
3. Fix IPort.Node → Create helper method
4. Fix Graph.Connections → Rewrite logic
5. Fix Node.Name → Node.Title
6. Fix IVariable properties
7. Fix GetConnectedOutputPort/InputPort → FirstConnectedPort
8. Test each tutorial after fixes
