# Combined Watching for Jellyfin

A lightweight Jellyfin plugin that merges **Continue Watching** and **Next Up** into a single, chronologically sorted home section.

![Jellyfin](https://img.shields.io/badge/Jellyfin-10.11.x-00A4DC?style=flat-square&logo=jellyfin)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square)

## The Problem

Jellyfin separates "Continue Watching" (partially watched items) and "Next Up" (next episodes in your series) into two distinct home sections. This means you have to mentally merge them yourself to decide what to watch next.

Every major streaming service (Netflix, Disney+, Prime Video) combines these into a single row. [This has been requested by the Jellyfin community for years](https://features.jellyfin.org/posts/1055/give-the-option-to-combine-next-up-and-continue-watching) (180+ votes).

## The Solution

This plugin merges both sections into one, sorted chronologically by when you last interacted with each title:

- **Paused a movie 5 minutes ago?** It appears first.
- **Finished an episode of a series yesterday?** The next episode appears right after your recent items.
- **No duplicates** — if an item appears in both Continue Watching and Next Up, it shows up once.

## How It Works

1. **Server-side**: A custom API endpoint (`/CombinedWatching/Items`) merges results from Jellyfin's Resume and NextUp queries, deduplicates them, and sorts by the most relevant date (last played for resume items, series last watched for next-up episodes).

2. **Client-side**: A small inline script intercepts the web client's API calls — redirecting the Resume request to the combined endpoint and suppressing the separate Next Up request. The result: one unified "Weiterschauen" section instead of two.

## Requirements

- **Jellyfin 10.11.x** (tested on 10.11.6)
- **[File Transformation Plugin](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** (required for web client injection)

## Installation

### From Release

1. Download `Jellyfin.Plugin.CombinedWatching.dll` from the [latest release](../../releases/latest)
2. Place it in your Jellyfin plugins directory:
   ```
   <jellyfin-config>/plugins/CombinedWatching_1.0.0.0/Jellyfin.Plugin.CombinedWatching.dll
   ```
3. Restart Jellyfin
4. Clear your browser cache and reload

### Build from Source

```bash
git clone https://github.com/YOUR_USERNAME/jellyfin-plugin-combined-watching.git
cd jellyfin-plugin-combined-watching
dotnet build --configuration Release
```

The compiled DLL will be at `bin/Release/net9.0/Jellyfin.Plugin.CombinedWatching.dll`.

## Configuration

Go to **Dashboard → Plugins → Combined Watching** to configure:

| Setting | Default | Description |
|---------|---------|-------------|
| Enabled | `true` | Toggle the combined view on/off |
| Section Title | `Weiterschauen` | Title shown for the combined section |
| Max Items | `24` | Maximum number of items to display |
| Next Up Cutoff | `365` days | Only include series watched within this period |

## Architecture

```
┌─────────────────────────────────────────────────┐
│ Jellyfin Web Client                             │
│                                                 │
│  ┌─────────────┐    ┌────────────────────────┐  │
│  │ inline <script>  │  Patches window.fetch  │  │
│  │ (before all  │──▶│  before Jellyfin loads  │  │
│  │  JS bundles) │    └────────────────────────┘  │
│  └─────────────┘              │                  │
│                    ┌──────────┴──────────┐       │
│                    ▼                     ▼       │
│            /Items/Resume          /Shows/NextUp  │
│            redirected to:         returns empty   │
│            /CombinedWatching/Items               │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│ CombinedWatchingController                      │
│                                                 │
│  1. Query Resume items (ILibraryManager)        │
│  2. Query NextUp items (ITVSeriesManager)       │
│  3. Get sort dates (IUserDataManager)           │
│  4. Deduplicate, sort chronologically           │
│  5. Return merged QueryResult<BaseItemDto>      │
└─────────────────────────────────────────────────┘
```

**Key design decision**: The fetch interception runs as an inline `<script>` injected right after `<head>`, *before* any Jellyfin JavaScript loads. This ensures `window.fetch` is patched before Jellyfin's API client captures a reference to it.

## Scope

This plugin modifies the **Jellyfin web client only**. Mobile apps (Streamyfin, Swiftfin, Findroid) and TV clients are not affected — they make their own API calls independently.

If you use [Moonfin](https://github.com/Moonfin-Client/Plugin) or [Streamyfin](https://github.com/streamyfin/streamyfin), those clients handle their own home screen layout.

## Credits

Built with the [Jellyfin Plugin Template](https://github.com/jellyfin/jellyfin-plugin-template) and uses the [File Transformation Plugin](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) by IAmParadox27 for web client injection.

Inspired by the community feature request: [Give the option to combine Next Up and Continue Watching](https://features.jellyfin.org/posts/1055/give-the-option-to-combine-next-up-and-continue-watching).

## License

MIT
