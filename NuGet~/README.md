## Developer documentation

### Important: Regarding the name of the directory: `NuGet~`

This directory must either start with `.` or end with `~` in order for Unity to ignore it.
Otherwise, Unity will print a lot of errors.

See: https://docs.unity3d.com/Manual/SpecialFolders.html

### How to publish a NuGet package
1. Open VTS-Sharp.sln in an IDE of your choice
2. Build the project
3. A NuGet package (VTS-Sharp.*.nupkg) will be generated inside [bin/Debug](bin/Debug).
4. Upload it on https://www.nuget.org/
