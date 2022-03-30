# InteractML_Telemetry
Addon for InteractML to log info about IML graphs and upload logs to a server

## Installation as a submodule of [InteractML](https://github.com/Interactml/iml-unity)
```
# go to your Unity project folder
cd [your_project_folder]

# add InteractML as a submodule (if not done already)
git submodule add -b master --force https://github.com/Interactml/iml-unity.git Assets/iml-unity

# add InteractML_Telemetry as a submodule 
git submodule add https://github.com/carlotes247/InteractML_Telemetry.git Assets/iml-unity-telemetry
```

## Dependencies
**This module also has a dependency to [InteractML](https://github.com/Interactml/iml-unity). It won't work as a standalone module without it.**
It also has a dependency to the following C# Nuget packages (included in project for set-up simplicity).
- [AsyncEnumerator](https://github.com/Dasync/AsyncEnumerable): Makes asynchronous enumeration as easy as the synchronous counterpart. Used to upload files to server asynchronously
- Microsoft.BcI.AsyncInterfaces 1.0.0: Provides the IAsyncEnumerable<T> and IAsyncDisposable interfaces and helper types. AsyncEnumerator depends on this.
- System.Threading.Tasks.Extensions 4.5.2: Provides additional types that simplify the work of writing concurrent and asynchronous code. AsyncEnumerator depends on this.
- System.Runtime.CompilerServices.Unsafe: Provides generic, low-level functionality for manipulating pointers.
