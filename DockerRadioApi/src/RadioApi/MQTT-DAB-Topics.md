# SisLink PRO DAB MQTT

## Topics

<prefix>/<tenantId>/<deviceId>/<deviceId>/<type>/<object>
sislink/l008/sl008u04/slp001/dab

### GetSenderList

Request:
Topic: sislink/l008/sl008u04/slp001/read/dab/senderList

Payload:
``` json
{
}
```

Antwort
Topc: sislink/l008/sl008u04/slp001/status/dab/senderList

Payload: 
``` json
{
	stations: [
		{
			label: "1 THAALAM TAMIL",
			mhz: 188.928,
			serviceId: 3559,
			subChannelId: 15,
			strength: 27
		},
		{
			label: "Radio Grischa",
			mhz: 188.928,
			serviceId: 3577,
			subChannelId: 6,
			strength: 27
		}
	]
}
```

### GetCurrentSender

Request:
Topic: sislink/l008/sl008u04/slp001/read/dab/currentSender

Payload:
``` json
{
}
```

Antwort
Topc: sislink/l008/sl008u04/slp001/status/dab/currentSender

Payload: 
``` json
{
	station:
	{
		label: "1 THAALAM TAMIL",
		mhz: 188.928,
		serviceId: 3559,
		subChannelId: 15,
		strength: 27
	}
}
```

### StopSender

Request:
Topic: sislink/l008/sl008u04/slp001/write/dab/stop

Payload:
``` json
{
}
```

### PlaySender

Request:
Topic: sislink/l008/sl008u04/slp001/write/dab/play

Payload:
``` json
{
	station:
		{
			label: "1 THAALAM TAMIL",
			serviceId: 3559,
			subChannelId: 15,
		},
	volume: 50
}
```

### SetVolume

Request:
Topic: sislink/l008/sl008u04/slp001/write/dab/volume

Payload:
``` json
{
	volume: 50
}
```
