# Build references

This directory is for local Slay the Spire 2 assemblies required to compile the mod.

Copy these files from your own local Slay the Spire 2 installation before building:

- `sts2.dll`
- `GodotSharp.dll`

Do not commit or redistribute these DLLs. They are game/runtime files, not part of this open-source project.

On Linux Steam, the files are usually somewhere under:

```text
~/.steam/steam/steamapps/common/Slay the Spire 2/
```

For this development environment they were found/copied from the local game installation into `references/`.
