# MicroMonitor

MicroMonitor è un widget leggero per Windows che mostra CPU, RAM, GPU e temperature nella barra delle applicazioni, a sinistra dell’area “mostra icone nascoste”.

<img width="427" height="46" alt="image" src="https://github.com/user-attachments/assets/91b44daa-5879-47b5-aacf-17fe892a2e2e" />


## Compilazione

```powershell
dotnet restore
dotnet build -c Release
```

## Avvio rapido

```powershell
dotnet run --project src/MicroMonitor
```

## Eseguibile plug-and-play

```powershell
dotnet publish src/MicroMonitor -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

L’eseguibile si trova in:

```text
src/MicroMonitor/bin/Release/net8.0-windows/win-x64/publish/
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
