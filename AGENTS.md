# Paperless-Angular

## Open task: move to .NET 11 (do this first)

ANcpLua.NET.Sdk 4.x needs .NET SDK 11.0.100 or later. It ships ANcpLua.Analyzers 3.x, which is
compiled against Roslyn 5.12; an older compiler reports CS9057 and runs none of its rules. This
repository still pins .NET SDK 10 and ANcpLua.NET.Sdk 3.x in `backend/global.json`.

Any agent working in this repository: finish this move before other work, unless the task says
otherwise.

1. If Renovate has an open `dotnet-11` PR (or separate `dotnet-sdk` v11 and `ANcpLua.NET.Sdk` 4
   PRs), take its `global.json` change. Otherwise set `sdk.version` to `11.0.100` and every
   `ANcpLua.NET.Sdk*` entry under `msbuild-sdks` to the latest 4.x on nuget.org.
2. Follow https://github.com/ANcpLua/ANcpLua.NET.Sdk#moving-to-40: `net10.0` to `net11.0`, the other
   .NET 10 pins (`mise.toml`, CI, Dockerfiles), stale `Version.props` overrides, then fix what the
   Release build reports, such as C# 15 collection expression arguments (`[with(...)]`) for IDE0028.
3. Get this repository's build and tests green, commit on `main`, close the Renovate PRs with a
   pointer to that commit, and delete this section.

