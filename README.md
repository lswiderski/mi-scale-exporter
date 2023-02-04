# mi-scale-exporter

Mobile App to export data from Mi Body Composition Scale (works with Mi Scale too) and upload it to Garmin Connect Cloud. It also allows you to upload manually entered body composition data to the Garmin cloud.

Tested on Oneplus 5T (Android 10) and Mi Body Composition Scale (XMTZC02HM)

## Download

- Google play: https://play.google.com/store/apps/details?id=com.lukaszswiderski.MiScaleExporter
- APK/ABB installers: https://github.com/lswiderski/mi-scale-exporter/releases

## Instruction

- Stand on your scale. Measure yourself. Complete the user form data, Scale Bluetooth address and get data from the scale. Mi Body Composition Scale is active up to 15 min after the measurement. (Bluetooth address can be found in Zepp Life > Profile > My devices > Mi Body Composition Scale > Bluetooth address (hold to copy)).

- If your scale supports "Weigh small object" - turn it off

- Then you can review your data and upload it to Garmin Cloud. If you do not have Mi scale and just want to manually insert the data, you can so.

- You can save the Garmin password in this App but you don't have to. Passwords Managers like KeePass2 works well too. If you do not provide a password in the settings, you will be asked for it each time before sending. No password will be saved in app.

- The unofficial Garmin API does not support 2FA. To use this application, you must disable it.

- This App pass your data, email and password directly to Garmin Connect Cloud or you can change it to proxy API server and then it sends to Garmin Cloud.

- The Proxy API does not store or log anything, it's just a middleware between this App and Garmin services.

- Proxy API repository: https://github.com/lswiderski/bodycomposition-webapi

- If you afraid of your data, you can host your own API server. Just change the server address in Settings. For now you can use default one: https://garmin.bieda.it/ tut it will be removed soon.

## Diagram of the flow with Web Proxy

```mermaid
sequenceDiagram
    participant Mobile App
    participant Mi Body Composition Scale
    participant API Endpoint Proxy
    participant Garmin Cloud
    Mobile App->>Mi Body Composition Scale: Connect and get data
    Mi Body Composition Scale-->>Mobile App: Weight and Impedance data
    loop
        Mobile App->>Mobile App: Calculate Body Composition
    end
    Mobile App->>API Endpoint Proxy: Body Composition data
    API Endpoint Proxy->>Garmin Cloud: Body Composition data
    Garmin Cloud-->>API Endpoint Proxy: Result
    API Endpoint Proxy-->>Mobile App: Result

```

## API Endpoint used in the app ([source](https://github.com/lswiderski/bodycomposition-webapi))

```http
https://garmin.bieda.it/
```

## Diagram of the flow with direct send to Garmin Cloud

```mermaid
sequenceDiagram
    participant Mobile App
    participant Mi Body Composition Scale
    participant Garmin Cloud
    Mobile App->>Mi Body Composition Scale: Connect and get data
    Mi Body Composition Scale-->>Mobile App: Weight and Impedance data
    loop
        Mobile App->>Mobile App: Calculate Body Composition
    end
    Mobile App->>Garmin Cloud: Body Composition data
    Garmin Cloud-->>Mobile App: Result

```

## Stack

- MAUI & .NET 7 (C#)
- Autofac
- Plugin.BLE - To receive data via Bluetooth from Mi scale
- Xamarin.Essentials
- API Backend in GOlang

## Images

- Xiaomi settings (Bluetooth adress - Zepp Life)

![xiaomi settings](https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/xiaomi.jpg)

- required user data

![required user data](https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/userdata.jpg)

- settings

![settings](https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/settings.jpg)

- measure

![measure](https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/measure.jpg)

- Calculated body composition

![calculated body composition](https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/bodycomposition.jpg)

- results in Gamrin Cloud

![garmin result](https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/garmin.png)

## Inspiration

- https://github.com/RobertWojtowicz/miscale2garmin
- https://github.com/davidkroell/bodycomposition

## If you like my work, you can buy me a coffee

<a href="https://www.buymeacoffee.com/lukaszswiderski" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>
