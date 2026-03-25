# Test-Anleitung für RadioBlazorApp

## Option 1: Mit Test-MQTT-Broker

### Schritt 1: Test-Broker starten

Öffnen Sie ein separates PowerShell-Fenster und führen Sie aus:

```powershell
cd D:\git\hackathon-scp-radio\RadioBlazorApp
dotnet run --project TestMqttBroker.cs
```

Der Test-Broker startet auf Port 1883 und antwortet automatisch auf GET_STATIONS Kommandos.

### Schritt 2: Blazor App starten

In einem anderen PowerShell-Fenster:

```powershell
cd D:\git\hackathon-scp-radio\RadioBlazorApp
dotnet run
```

### Schritt 3: App testen

1. Öffnen Sie Browser auf `https://localhost:5001`
2. Navigieren Sie zu "DAB+ Radio" im Menü
3. Die Standardeinstellungen sind bereits korrekt (localhost:1883)
4. Klicken Sie auf "Verbinden"
5. Klicken Sie auf "Sender abrufen"
6. Sie sollten 10 Test-Sender sehen

## Option 2: Mit echtem MQTT-Broker

### Voraussetzung: Mosquitto installieren

Download: https://mosquitto.org/download/

### Schritt 1: Mosquitto starten

```powershell
# In Windows
net start mosquitto

# Oder direkt
mosquitto -v
```

### Schritt 2: Simulator für DAB+ Radio erstellen

Sie benötigen einen separaten Service/Skript, der:
1. Auf Topic `radio/command` lauscht
2. Bei "GET_STATIONS" eine JSON-Liste mit Sendern auf `radio/stations` veröffentlicht

### Schritt 3: App konfigurieren und starten

1. Starten Sie die Blazor App: `dotnet run`
2. Öffnen Sie Browser
3. Konfigurieren Sie die MQTT-Verbindung
4. Verbinden und testen

## MQTT Nachrichtenformat

### Request
- **Topic**: `radio/command`
- **Payload**: `GET_STATIONS`

### Response
- **Topic**: `radio/stations`
- **Payload**: JSON Array

```json
[
  {
    "name": "Radio SRF 1",
    "frequency": "97.6 MHz",
    "signalStrength": 95,
    "genre": "Nachrichten"
  },
  {
    "name": "Radio Energy",
    "frequency": "100.9 MHz",
    "signalStrength": 78,
    "genre": "Pop"
  }
]
```

## Test mit MQTT-Client (mosquitto_pub/sub)

### Terminal 1: Subscribe zu Responses
```bash
mosquitto_sub -t "radio/stations" -v
```

### Terminal 2: Sende Kommando
```bash
mosquitto_pub -t "radio/command" -m "GET_STATIONS"
```

### Terminal 3: Simuliere Response (für Test)
```bash
mosquitto_pub -t "radio/stations" -m '[{"name":"Test Radio","frequency":"100 MHz","signalStrength":80,"genre":"Test"}]'
```

## Troubleshooting

### Port 1883 bereits belegt
- Ändern Sie den Port in der App-Konfiguration
- Oder stoppen Sie andere MQTT-Broker: `net stop mosquitto`

### Keine Verbindung möglich
- Überprüfen Sie Firewall-Einstellungen
- Stellen Sie sicher, dass der Broker läuft
- Überprüfen Sie die Broker-Adresse (localhost vs. IP-Adresse)

### Keine Sender empfangen
- Überprüfen Sie das JSON-Format der Response
- Überprüfen Sie die Topics (Request/Response müssen übereinstimmen)
- Schauen Sie in die Status-Nachrichten der App

### Build-Fehler
```powershell
# Clean und rebuild
dotnet clean
dotnet restore
dotnet build
```
