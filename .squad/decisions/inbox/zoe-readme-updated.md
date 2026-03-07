# README Now in Sync with Shipped API

**Date:** 2026-03-07
**Author:** Zoe (QA / Docs Owner)
**Status:** For review / merge into decisions.md

## Summary

README.md has been fully rewritten and is now in sync with the library's current public API (as of the FixtureFactory refactor in Phase 8).

## What Was Updated

- Replaced defunct SutFixture API with FixtureFactory
- Fixed broken CI badge (was uild.yml, corrected to ci.yml)
- Added complete sections for all four theory data attributes
- Added sections for RecursionCustomization and ImmutableCollectionCustomization
- Added section for SutFixtureCustomizations project-wide registration pattern
- Added section for [CustomizeWith] method/class-level customization
- Added section for [Frozen] parameter freezing
- Committed as: docs: update README with full library documentation

## Recommendation

The README should be kept in sync whenever a public API surface changes. As QA/docs owner, Zoe will flag README gaps in future task reviews.
