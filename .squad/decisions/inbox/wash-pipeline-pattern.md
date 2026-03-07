### Pipeline pattern: GitHub Release tag → NuGet version
- ci.yml: push/PR to main → build + test + coverage badges
- release.yml: v*.*.* tag (must be on main) → build + test + pack + NuGet publish
- release-preview.yml: v*.*.*-previewN tag → build + pack + NuGet publish (no main guard)
- Version flows from tag → VERSION env var → -p:Version=${VERSION} on build+pack
- NUGET_KEY secret required in repo settings before first release
