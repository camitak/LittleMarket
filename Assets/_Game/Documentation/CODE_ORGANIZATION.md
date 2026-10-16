# Little Market Code Organization

## Refactoring rule

Refactor around responsibilities, not line count alone.

A long cohesive class can be acceptable.
A shorter class doing several unrelated jobs is a stronger design problem.

## Preferred boundaries

### MonoBehaviours

Use MonoBehaviours when code needs:
- Unity lifecycle methods
- Inspector references
- a GameObject / Transform
- scene presence

### Plain C# classes

Prefer plain C# classes when code represents:
- state
- calculations
- policies
- selection logic
- orchestration that does not require Unity lifecycle

Examples:
- PlayerCarryState
- PlacementUtility
- InteractionTargetFinder

## Refactor timing

Refactor when:
- a class clearly owns multiple responsibilities
- adding features requires changing unrelated code
- logic is duplicated
- old architecture remains after a system redesign
- a stable boundary has become obvious

Avoid large speculative refactors while a gameplay system is still changing rapidly.

## Size warning signs

These are review triggers, not hard rules:

- ~400 lines: check whether the class is still cohesive
- ~800 lines: likely worth reviewing responsibilities
- ~1000+ lines: strong reason to inspect architecture

## Compatibility

During refactors, preserve existing public APIs when practical.

First:
1. move responsibility
2. verify behavior
3. later simplify public APIs

Do not combine major behavior changes and structural refactors unless necessary.