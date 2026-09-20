# Red-Black Tree Visualizer

An interactive web application that shows **how a red-black tree actually works** — not just the
final shape, but every rotation, every recolouring and every delete fix-up, played back step by
step in the browser.

Built with **ASP.NET Core 9 (MVC)** on the server and **D3.js** on the client.

> University diploma project. The tree itself is implemented from scratch following the CLRS
> algorithms — no data-structure library is used.

---

## Features

| | |
|---|---|
| **Insert / Delete / Search** | Full red-black tree operations with automatic rebalancing |
| **Step-by-step playback** | Every algorithm step is recorded and replayed with animated rotations |
| **Manual & auto play** | Step forward/back, autoplay, or drive it with the arrow keys |
| **Live explanations** | Each step is described in plain language as it happens |
| **Smart camera** | Large trees automatically pan to the node being worked on |
| **Zoom & pan** | Explore big trees freely |
| **Import / Export** | Save a tree to JSON and load it back later |
| **Statistics** | Node count, minimum, maximum and height |

The tree survives page reloads: each visitor gets their own tree, kept in the session.

---

## Getting started

```bash
git clone https://github.com/<your-username>/red-black-tree-visualizer.git
cd red-black-tree-visualizer
dotnet run --project RedBlackTree
```

Then open <https://localhost:7033>.

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download). The web project has **no NuGet
dependencies at all** — everything it needs ships with the framework.

### Running the tests

```bash
dotnet test
```

---

## How it is put together

The interesting design decision is that **the tree knows nothing about the web application**, and
the web application knows nothing about how the tree is implemented. Each piece has exactly one job:

```
Controllers/     HTTP only — parse the request, call a service, redirect
Services/        Orchestration — what happens on "insert", "import", "export"
Models/          The red-black tree itself: a pure algorithm, no framework types
Recording/       Captures algorithm steps for the animation (optional, pluggable)
Serialization/   Turns a tree into JSON and back
Storage/         Where a visitor's tree lives between requests
Comparers/       How two values are ordered
Resources/       User-facing text
```

Everything is wired together through interfaces (`IRedBlackTreeService`, `ITreeStore`,
`ITreeSerializer`, `ITreeStepRecorder`), so any layer can be swapped or tested in isolation.

### Recording steps without slowing the tree down

The animation needs a snapshot of the whole tree after every single operation — but building those
snapshots is expensive, and most of the time nobody is watching an animation.

So the tree talks to an `ITreeStepRecorder` and hands it a *function* that would build the snapshot,
rather than the snapshot itself. When no animation is needed, the tree runs with
`NullTreeStepRecorder` (the Null Object pattern): the function is never called and the cost is zero.

```csharp
public interface ITreeStepRecorder<T>
{
    bool IsEnabled { get; }
    void Record(StepAction action, Func<TreeNodeModel?> stateFactory, ...);
}
```

### Ordering values

Values are stored as text, but `"10"` must come *after* `"9"` — not before it, as plain string
comparison would have it. `NumericAwareStringComparer` compares two values numerically when both
parse as numbers and alphabetically otherwise, so a tree can hold `3, 9, 10, 100` and `ա, բ, գ` at
the same time.

The tree receives this as an `IComparer<T>` and never looks at the values itself.

---

## Correctness

A red-black tree is only useful if its five invariants hold after *every* operation:

1. Every node is either red or black
2. The root is black
3. Every leaf (NIL) is black
4. A red node's children are both black
5. Every path from a node to its leaves contains the same number of black nodes

Together these guarantee the height stays below `2·log₂(n+1)`, which is what makes lookups
`O(log n)`.

The test suite checks all five after every insert and every delete across hundreds of randomised
operations, and verifies the height bound is never exceeded — including the worst case of inserting
500 values in ascending order, which would degrade an ordinary binary search tree into a linked list.

---

## Screenshots

<!-- Add screenshots here: the tree view, and the step-by-step panel mid-rotation. -->

---

## Licence

MIT
