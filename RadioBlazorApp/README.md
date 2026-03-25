# RadioBlazorApp - DAB+ Radio Sender Viewer

Eine Blazor Server-Anwendung zur Anzeige von DAB+ Radio-Sendern über MQTT.

## Features

- **MQTT-Client-Integration**: Verbindung zu einem MQTT-Broker
- **Kommando-Steuerung**: Senden von Befehlen zum Abrufen von Sendern
- **Senderliste**: Übersichtliche Darstellung der empfangenen DAB+ Sender
- **Signalstärke-Anzeige**: Visuelle Darstellung der Signalqualität
- **Filterung**: Automatische Sortierung nach Signalstärke

## Verwendung

### 1. Anwendung starten

```bash
cd D:\git\hackathon-scp-radio\RadioBlazorApp
dotnet run
```

Die Anwendung läuft dann auf `https://localhost:5001` oder `http://localhost:5000`

### 2. MQTT-Konfiguration

In der Anwendung können Sie folgende Parameter konfigurieren:

- **Broker Adresse**: Die IP-Adresse oder Hostname des MQTT-Brokers (Standard: `localhost`)
- **Port**: Der MQTT-Port (Standard: `1883`)
- **Request Topic**: Topic für Befehle (Standard: `radio/command`)
- **Response Topic**: Topic für Antworten (Standard: `radio/stations`)

### 3. Verbinden und Sender abrufen

1. Geben Sie die MQTT-Broker-Details ein
2. Klicken Sie auf "Verbinden"
3. Nach erfolgreicher Verbindung klicken Sie auf "Sender abrufen"
4. Die Senderliste wird automatisch aktualisiert

## MQTT-Nachrichtenformat

### Request (Befehl senden)
- **Topic**: `radio/command` (konfigurierbar)
- **Payload**: `GET_STATIONS`

### Response (Sender empfangen)
- **Topic**: `radio/stations` (konfigurierbar)
- **Payload**: JSON-Array mit RadioStation-Objekten

Beispiel Response:
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

## Technologien

- **.NET 10**
- **Blazor Server**
- **MQTTnet 4.3.7**
- **Bootstrap 5** (für UI-Styling)

## Projekt-Struktur

```
RadioBlazorApp/
├── Components/
│   ├── Layout/
│   │   └── NavMenu.razor         # Navigation
│   └── Pages/
│       └── RadioStations.razor   # Hauptseite für Senderliste
├── Services/
│   └── MqttClientService.cs      # MQTT-Client-Service
├── Program.cs                     # Anwendungs-Konfiguration
└── README.md                      # Diese Datei
```

## Entwicklung

### Abhängigkeiten hinzufügen

```bash
dotnet add package MQTTnet
```

### Build

```bash
dotnet build
```

### Ausführen

```bash
dotnet run
```

## Troubleshooting

### Verbindung schlägt fehl
- Überprüfen Sie, ob der MQTT-Broker läuft
- Überprüfen Sie Firewall-Einstellungen
- Stellen Sie sicher, dass die Broker-Adresse und der Port korrekt sind

### Keine Sender empfangen
- Überprüfen Sie, ob das Response-Topic korrekt ist
- Überprüfen Sie das JSON-Format der Antwort
- Schauen Sie in die Status-Meldungen für Details

## Lizenz

Dieses Projekt wurde für den Hackathon SCP Radio erstellt.
