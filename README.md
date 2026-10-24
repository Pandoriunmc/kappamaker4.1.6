# KappaMarker

A SPT-AKI mod that marks quests required for Kappa container/Collector quest. It marks both quest names and descriptions.

## Requirements

- **SPT-AKI version 4.0.X**
- **.NET SDK 9.0**

## Build Instructions

**1. Make sure you have [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed**  
```bash
dotnet --version
```

**2. Clone the repository**
```bash
git clone https://github.com/rafalsoja/KappaMarker.git
cd KappaMarker
```

**3. Build the project**
```bash
dotnet build KappaMarker.csproj -c Release
```

Compiled dll will be located in:
```
bin/Release/KappaMarker/KappaMarker.dll
```

Create folder for plugin inside your game location and copy dll file inside it.
```
Escape from Tarkov/SPT/user/mods/KappaMarker/KappaMarker.dll
```
