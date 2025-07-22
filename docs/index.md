---
layout: default
---

Mobile App to export data from Xiaomi Scales:

- Mi Smart Scale
- Mi Body Composition Scale 1 and 2
- Mi Body Composition Scale S400 (only Android)

and upload it to Garmin Connect Cloud.

It also allows you to upload manually entered body composition data to the Garmin cloud.

## If you like my work, you can buy me a coffee

<a href="https://www.buymeacoffee.com/lukaszswiderski" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Steps to Connect Xiaomi Body Composition Scale S400:

1. Add Scale to Xiaomi Home App and do first measurement.

2. You will need scale MAC address and BLE Key from Xiaomi Cloud. You can get it on many ways but I recommend 'Xiaomi Cloud Tokens Extractor'
   Go to  <a target="_blank" rel="noopener noreferrer" href="https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor">https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor</a> and use your preferred way
   Found Xiaomi Body Composition Scale S400 on the list of your devices and copy BLE KEY and MAC and save it for later.

   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/token_extractor.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/token_extractor.png" alt="Xiaomi Cloud Tokens Extractor" style="width: 836px;"></a>

3. Now you need to completely kill the app (so that it doesn't run in the background either). Or remove the scale from the list of devices - if you use other devices with Xiaomi Home. The scale will only send the needed data when it is not able to connect to the Xiaomi Home app! Every time when you add Scale as new device to Xiaomi Home new BLE key will be generated. You can disable Heart rate measurement to fast up whole process.

   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/xiaomi_home.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/xiaomi_home.png" alt="Xiaomi Homer" style="width: 400px;"></a>

4. Now got MiScale Exporter settings, select S400 scale and paste MAC address and BLE Key.
   
   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/s400_settings.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/s400_settings.png" alt="S400 settings" style="width: 400px;"></a>

5. The scale only sends data in a short time window at the end of weighing, so it's important to start the measurement before stepping on the scale. The Bluetooth icon should blink.
   
   [![Watch the video](https://img.youtube.com/vi/HtOZZwnkZHw/0.jpg)](https://www.youtube.com/shorts/HtOZZwnkZHw)

6. These types of scales do not measure body composition. They measure weight and impedance and estimate the result based on those measurements. The scale sends 3 values: Weight, impedance and Heart rate. To receive body composition data, impedance is processed by an algorithm known to be similar to that used by the Mi Body Composition Scale 2 (different from that used by the S400). Because of this, the result may differ from that of the Xiaomi Home app.

7. Bear in mind that it is an experimental solution and errors may occur. If you encounter them, please contact me.
