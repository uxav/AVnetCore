<p align="center">
   <img src="docs/assets/avnet_logo.svg">
</p>

<h1 align="center">AVnet Core</a></h1>

<p align="center">An open-source Crestron SimplSharp Pro framework for Crestron control systems</p>

<p align="center">
   <img alt="Status" src="https://img.shields.io/github/actions/workflow/status/uxav/AVnetCore/test.yml?branch=main&style=flat&logo=github&label=status">
   <img alt="NuGet Version" src="https://img.shields.io/nuget/v/UXAV.AVnet.Core?style=flat&logo=nuget">
   <img alt="Downloads" src="https://img.shields.io/nuget/dt/UXAV.AVnet.Core?style=flat&logo=nuget">
   <img alt="Issues" src="https://img.shields.io/github/issues/uxav/AVnetCore?style=flat&logo=github">
   <img alt="Pull Requests" src="https://img.shields.io/github/issues-pr/uxav/AVnetCore?style=flat&logo=github">
   <img alt="Licnse" src="https://img.shields.io/github/license/uxav/AVnetCore?style=flat">
</p>

<p align="center"><a href="https://uxav.github.io/AVnetCore">Explore Docs</a></p>

## Index

- [Quick start](#quick-start)
- [Links](#links)
- [Dependencies](#dependencies)
- [Documentation](#documentation)
- [Release Notes](#release-notes)
- [Contributing](#contributing)
- [License](LICENSE)

## Quick Start

To use this test library in your project, follow these steps:

1. Install the package via NuGet. You can use the following command in the Package Manager Console:

   ```
    dotnet add [<PROJECT>] package UXAV.AVnet.Core
   ```

2. Import the library classes in your code file(s):

   ```csharp
   using UXAV.AVnet.Core.Models;
   using UXAV.AVnet.Core.Models.Diagnostics;
   using UXAV.Logging;
   ```

3. Create a class that inherits from SystemBase:
   ```csharp
   public class MySystem : SystemBase
   {
      public MySystem(CrestronControlSystem controlSystem) : base(controlSystem)
      {
         // create all your instance logic here and any instances of rooms, devices or 
      }

      protected override void AppShouldRunUpgradeScripts()
      {
         // called when the program starts with a new version number
      }

      protected override void OnProgramStatusEventHandler(eProgramStatusEventType eventType)
      {
         if (eventType == eProgramStatusEventType.Stopping)
         {
            // anything you need to save, disconnect or stop... the program is stopping
         }
      }

      protected override IEnumerable<DiagnosticMessage> GenerateDiagnosticMessages()
      {
         return new DiagnosticMessage[]
         {
            // add any diagnostic messages here,
            // this is called for when the system needs to update the
            // status of stuff or the dashboard app requests it
         };
      }

      protected override void SystemShouldAddItemsToInitialize(Action<IInitializable> addItem)
      {
         addItem(myDeviceWithInitialization);
         addItem(myOtherDeviceWithInitialization);
      }

      protected override void WebScriptingHandlersShouldRegister()
      {
         // any web scripting handlers for API's can and should register here (see docs)
      }
   }
   ```

4. Load and Initialize your main instance of system
   ```csharp
   public class ControlSystem : CrestronControlSystem
   {
      private readonly SystemBase _mySystem;

      public ControlSystem()
      {
         try
         {
            // create your instance of MySystem
            _mySystem = new MySystem(this);
         }
         catch (Exception e)
         {
            Logger.Error(e);
         }
      }

      public override void InitializeSystem()
      {
         try
         {
            // start the initializing of MySystem
            _mySystem?.Initialize();
         }
         catch (Exception e)
         {
            Logger.Error(e);
         }
      }
   }
   ```

## Links

GitHub Repository: [AVnetCore](https://github.com/uxav/AVnetCore)

NuGet Package: [UXAV.AVnet.Core](https://www.nuget.org/packages/UXAV.AVnet.Core/)

## Dependencies

- [UXAV.Logging](https://www.nuget.org/packages/UXAV.Logging)
- [Crestron.SimpleSharp.SDK.ProgramLibrary](https://www.nuget.org/packages/Crestron.SimplSharp.SDK.ProgramLibrary)
- [Crestron.SimplSharp.SDK.ProgramLibrary](https://www.nuget.org/packages/)
- [Cronos](https://www.nuget.org/packages/Cronos)
- [CsvHelper](https://www.nuget.org/packages/CsvHelper)
- [Figgle](https://www.nuget.org/packages/Figgle)
- [Microsoft.AspNet.WebApi.Client](https://www.nuget.org/packages/Microsoft.AspNet.WebApi.Client)
- [Microsoft.CSharp](https://www.nuget.org/packages/Microsoft.CSharp)
- [MimeTypes](https://www.nuget.org/packages/MimeTypes)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
- [Newtonsoft.Json.Schema](https://www.nuget.org/packages/Newtonsoft.Json.Schema)
- [System.IO.Compression](https://www.nuget.org/packages/System.IO.Compression)
- [System.Net.Http](https://www.nuget.org/packages/System.Net.Http)
- [System.Reflection.MetadataLoadContext](https://www.nuget.org/packages/System.Reflection.MetadataLoadContext)
- [System.Runtime.Serialization.Json](https://www.nuget.org/packages/System.Runtime.Serialization.Json)
- [UXAV.Logging](https://www.nuget.org/packages/UXAV.Logging)
- [WebSocketSharp-netstandard](https://www.nuget.org/packages/WebSocketSharp-netstandard)

## Release Notes

### v2.0.0

- Reconfigured workspace to new style SDK format and added support for .NET 6.0
- MidnightNotifier removed completely, use a [CronJob](UXAV.AVnet.Core/CronJobs.cs)
- 

## Documentation

Documentation can be viewed in [GitHub Pages](https://uxav.github.io/AVnetCore)

## Contributing

Contributions are welcome! If you would like to contribute to this project, please follow these guidelines:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes and commit them.
4. Push your changes to your forked repository.
5. Submit a pull request to the main repository.

Please ensure that your code follows the project's coding conventions and includes appropriate tests.

- For feature branches use the name `feature/feature-name`
- Version numbers are checked against existing tags and fail CI on match

Thank you for your interest in contributing to this project!

## License

This project is licensed under the [MIT License](LICENSE.md).
