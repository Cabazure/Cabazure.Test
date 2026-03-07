# Decision: Private Field Naming Convention — camelCase, No Prefix

**Proposed by:** Kaylee (Core Dev)
**Date:** 2026-03-07
**Status:** Proposed

## Decision

Private instance fields and private static fields in all C# source files use plain **camelCase** with **no prefix** (no underscore `_`, no `s_`).

## Rationale

- Aligns with the Cabazure sibling repos (e.g., `Cabazure.Client`) for a consistent codebase style across the organisation.
- The `.editorconfig` naming rules (`private_fields_should_be_camelcase`, `private_static_fields_should_be_camelcase`) enforce this at editor/analyzer level.
- `_` prefix is a Visual Studio default but not a .NET Runtime or BCL convention; camelCase is the BCL-preferred style per the .NET design guidelines for private members.
- Avoids accidental shadowing confusion between parameter names and field names — reviewers should rely on `this.` qualification if disambiguation is ever needed (which is rare in our codebase).

## Special Case

- `lock` is a reserved C# keyword; the sync-lock object in `SutFixtureCustomizations` is therefore named `syncLock` (not `lock`). This is not an exception to the rule — it is the correct camelCase name when the word "lock" conflicts with the language.

## Affected Files (at time of decision)

- `src/Cabazure.Test/Fixture/SutFixture.cs` — `_fixture` → `fixture`
- `src/Cabazure.Test/Customizations/SutFixtureCustomizations.cs` — `_customizations` → `customizations`, `_lock` → `syncLock`
