### 2026-03-07: SutFixtureCustomizations merged into FixtureFactory.Customizations

**By:** Ricky (via Copilot)  
**What:** Removed standalone `SutFixtureCustomizations` static class. Moved functionality into `FixtureFactory.Customizations` (type: `FixtureCustomizationCollection`). Collection is pre-seeded with `AutoNSubstituteCustomization`. Supports `Add`, `Remove(instance)`, `Remove<T>()`, `Clear`, `Count`, and `IEnumerable<ICustomization>`.  
**Why:** Better discoverability — users find the API through `FixtureFactory` rather than a separate class. Richer API with Remove/Clear/Count. `SutFixtureCustomizations` name was a holdover from the removed `SutFixture` class.
