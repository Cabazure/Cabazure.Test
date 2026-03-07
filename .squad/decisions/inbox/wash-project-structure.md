# Decision: Project Structure and Build Configuration

**Date:** 2026-03-07  
**Author:** Wash  
**Status:** Proposed  

## Context

We need to establish the solution structure, project configuration, and NuGet dependencies for Cabazure.Test. This is a library that extends xUnit 3 with custom data attributes, so getting the package references right is critical.

## Decision

### Solution Structure
- **Solution File:** `Cabazure.Test.slnx` at repo root
- **Main Library:** `src/Cabazure.Test/Cabazure.Test.csproj`
- **Test Project:** `tests/Cabazure.Test.Tests/Cabazure.Test.Tests.csproj`
- **Directory Organization:**
  - `src/Cabazure.Test/Attributes/` — xUnit data attributes
  - `src/Cabazure.Test/Customizations/` — AutoFixture customizations
  - `src/Cabazure.Test/Fixture/` — Fixture and builder types

### xUnit 3 Package References (Library)
For a library that extends xUnit 3 with custom attributes:
- `xunit.v3.extensibility.core` version 3.2.2
- `xunit.v3.assert` version 3.2.2

**NOT** `xunit.v3` — that's for test projects and requires `OutputType=Exe`.

### Supporting Packages (Library)
- `AutoFixture` version 4.18.1
- `AutoFixture.AutoNSubstitute` version 4.18.1
- `NSubstitute` version 5.3.0
- `FluentAssertions` version 7.0.0

### Test Project Packages
- `xunit.v3` version 3.2.2 (full test framework)
- `xunit.runner.visualstudio` version 3.1.5
- `Microsoft.NET.Test.Sdk` version 17.12.0
- `coverlet.collector` version 6.0.4
- Project reference to `Cabazure.Test`

### Project Settings
- TargetFramework: `net9.0`
- LangVersion: `latest`
- Nullable: `enable`
- ImplicitUsings: `enable`

### Code Style (.editorconfig)
- C# indent: 4 spaces
- CRLF line endings
- UTF-8 charset
- `csharp_using_directive_placement = outside_namespace:warning`
- `csharp_style_var_for_built_in_types = false:suggestion`
- `csharp_style_var_when_type_is_apparent = true:suggestion`

## Rationale

1. **xUnit 3 Package Split:** The xUnit team split v3 into focused packages. Libraries extending xUnit should use `extensibility.core` to avoid the test runner overhead and OutputType requirements.

2. **Version Locking:** We're locking all package versions explicitly to ensure reproducible builds and avoid surprise breaking changes.

3. **Directory Structure:** Separates concerns — Attributes (xUnit integration), Customizations (AutoFixture), Fixture (core test builders).

4. **NuGet Metadata:** Included in the main .csproj for future packaging and publishing to NuGet.

## Consequences

- **Positive:** Clear separation of concerns, explicit dependencies, ready for NuGet publishing
- **Positive:** xUnit 3 extensibility packages give us access to `IDataAttribute` and other extensibility points
- **Positive:** Test project can consume library as users would
- **Negative:** Need to keep xUnit v3 package versions in sync across projects
- **Risk:** xUnit 3 is still evolving; API surface may change in future versions

## Open Questions

- Do we need AutoFixture 5.x (which has preview xUnit 3 support) or stick with 4.18.1?
- Should we add more aggressive nullability and warning-as-error settings?

## Next Steps

1. Kaylee needs to verify the xUnit 3 extensibility API matches what `AutoNSubstituteDataAttribute` expects
2. Zoe should validate the test project can run tests and generate coverage
3. Consider adding a NuGet.config if we need to pull from myget or other feeds
