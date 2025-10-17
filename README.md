# Lahav Controller Support

This repository contains a simple C# console application that reads input from a connected controller (gamepad/joystick) and prints the values to the console.

## Getting started

1. Install the [.NET 7.0 SDK](https://dotnet.microsoft.com/download).
2. Restore dependencies and build the project:

   ```bash
   dotnet build ControllerReader
   ```

3. Run the console application:

   ```bash
   dotnet run --project ControllerReader
   ```

While the application is running it will print button presses, POV hat angles, and axis values it detects on the controller. Press <kbd>Ctrl</kbd> + <kbd>C</kbd> to exit.
