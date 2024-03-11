## Getting Started

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