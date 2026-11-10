# MicroMonitor

MicroMonitor è un widget leggero per Windows che mostra CPU, RAM, GPU e temperature direttamente nella barra delle applicazioni, a sinistra dell’area “mostra icone nascoste”.

<img width="427" height="46" alt="image" src="https://github.com/user-attachments/assets/91b44daa-5879-47b5-aacf-17fe892a2e2e" />

## Installazione da zero

### 1. Installa .NET 8 SDK

Scarica e installa **.NET 8 SDK per Windows x64**:

https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.410/dotnet-sdk-8.0.410-win-x64.exe

In alternativa, da PowerShell:

```powershell
winget install Microsoft.DotNet.SDK.8
```

Verifica l’installazione:

```powershell
dotnet --version
```

### 2. Scarica il progetto

Clicca su **Code** → **Download ZIP**, poi estrai la cartella scaricata.

Oppure, se hai Git installato:

```powershell
git clone https://github.com/vincenzo051016-blip/MicroMonitor.git
cd MicroMonitor
```

### 3. Compila il progetto

Apri PowerShell nella cartella del progetto e lancia:

```powershell
dotnet restore
dotnet build -c Release
```

### 4. Avvio rapido per test

```powershell
dotnet run --project src/MicroMonitor
```

### 5. Crea l’eseguibile definitivo

Per creare un file `.exe` autonomo in `C:\MicroMonitor`:

```powershell
dotnet publish src/MicroMonitor -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o C:\MicroMonitor
```

L’eseguibile finale sarà:

```text
C:\MicroMonitor\MicroMonitor.exe
```

### 6. Avvio automatico al login di Windows

Apri PowerShell **come Amministratore** ed esegui:

```powershell
schtasks /create /tn "MicroMonitor" /tr "C:\MicroMonitor\MicroMonitor.exe" /sc onlogon /rl highest /f
```

Per avviarlo subito senza riavviare il PC:

```powershell
schtasks /run /tn "MicroMonitor"
```

Per rimuovere l’avvio automatico:

```powershell
schtasks /delete /tn "MicroMonitor" /f
```

## Impostazioni

```text
%LOCALAPPDATA%\MicroMonitor\settings.json
```

```json
{
  "RefreshIntervalMs": 2000,
  "ShowCpuTemperature": true,
  "ShowGpuTemperature": true
}
```

Per alcune temperature CPU può essere necessario avviare l’app come amministratore.
