# Инструкции по сборке Steam Trade Confirmer

## Требования

- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (рекомендуется) или Visual Studio Code

## Сборка из исходного кода

### 1. Клонирование репозитория

```bash
git clone <repository-url>
cd SteamKit/SteamTradeConfirmer
```

### 2. Восстановление зависимостей

```bash
dotnet restore
```

### 3. Сборка проекта

```bash
# Debug сборка
dotnet build --configuration Debug

# Release сборка
dotnet build --configuration Release
```

### 4. Запуск приложения

```bash
dotnet run --project SteamTradeConfirmer
```

### 5. Создание исполняемого файла

```bash
# Создание self-contained приложения
dotnet publish --configuration Release --runtime win-x64 --self-contained true

# Создание framework-dependent приложения
dotnet publish --configuration Release --runtime win-x64 --self-contained false
```

## Сборка в Visual Studio

1. Откройте `SteamTradeConfirmer.sln` в Visual Studio 2022
2. Выберите конфигурацию (Debug/Release)
3. Нажмите Build → Build Solution (Ctrl+Shift+B)
4. Для запуска нажмите F5 или Debug → Start Debugging

## Структура выходных файлов

После сборки файлы будут находиться в:
- Debug: `bin/Debug/net8.0-windows/`
- Release: `bin/Release/net8.0-windows/`

## Зависимости

Проект автоматически ссылается на:
- SteamKit2 (локальная сборка)
- System.Security.Cryptography.ProtectedData
- System.Text.Json
- Newtonsoft.Json

## Устранение проблем

### Ошибка "SDK not found"

Убедитесь, что установлен .NET 8.0 SDK:
```bash
dotnet --version
```

### Ошибки компиляции SteamKit2

Убедитесь, что SteamKit2 собран:
```bash
cd ../SteamKit2/SteamKit2
dotnet build
```

### Ошибки с пакетами NuGet

Очистите кэш и восстановите пакеты:
```bash
dotnet nuget locals all --clear
dotnet restore
```

## Создание установщика

Для создания MSI установщика можно использовать WiX Toolset или Advanced Installer.

### Пример WiX конфигурации

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="Steam Trade Confirmer" Language="1033" Version="1.0.0.0" Manufacturer="SteamRE" UpgradeCode="PUT-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />
    <MediaTemplate />
    
    <Feature Id="ProductFeature" Title="Steam Trade Confirmer" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>
  
  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="SteamTradeConfirmer" />
      </Directory>
    </Directory>
  </Fragment>
  
  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="MainExecutable">
        <File Id="SteamTradeConfirmer.exe" Source="$(var.SteamTradeConfirmer.TargetDir)SteamTradeConfirmer.exe" KeyPath="yes" />
      </Component>
    </ComponentGroup>
  </Fragment>
</Wix>
```

## Тестирование

Для запуска тестов (если они будут добавлены):
```bash
dotnet test
```

## Отладка

### Включение подробного логирования

Добавьте в `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SteamKit2": "Trace"
    }
  }
}
```

### Отладка в Visual Studio

1. Установите точки останова в коде
2. Запустите в режиме отладки (F5)
3. Используйте окна Debug → Windows для анализа переменных

## Производительность

### Оптимизация сборки

```bash
# Включение оптимизаций
dotnet build --configuration Release --verbosity minimal

# Параллельная сборка
dotnet build --maxcpucount
```

### Анализ размера

```bash
# Анализ зависимостей
dotnet list package --include-transitive

# Создание отчета о размере
dotnet publish --configuration Release --self-contained true --verbosity detailed
```
