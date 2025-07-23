---
layout: default
---

Mobile App to export data from Xiaomi Scales:

- Mi Smart Scale
- [Mi Body Composition Scale 1 and 2](#steps-to-connect-mi-body-compostion-scale-1-and-2)
- [Mi Body Composition Scale S400](#steps-to-connect-xiaomi-body-composition-scale-s400) (only Android)

and upload it to [Garmin Connect Cloud.](#garmin-connect-upload)

It also allows you to upload manually entered body composition data to the Garmin cloud.

> [!CAUTION]  
> This application is not supported or endorsed by Xiaomi or Garmin. So it could stop working at any moment. It is intended for personal use only and is not to be used for financial gain. The creator takes no responsibility for any consequences that may arise from its use.

## If you like my work, you can buy me a coffee

<a href="https://www.buymeacoffee.com/lukaszswiderski" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Steps to Connect Mi Body Compostion Scale 1 and 2:

1. Add Scale to Zepp Life app.

2. You will need the Bluetooth address of the scale. Go to Zepp Life > Profile > My devices > Mi Body Composition Scale > Bluetooth address (hold to copy)

    <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/xiaomi.jpg"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/xiaomi.jpg" alt="Xiaomi settings" style="width: 400px; margin: auto;
    display: block;"></a>

3. If your scale supports "Weigh small object" - turn it off

4. Open Settings in MiSCale Exporter. Select Scale Model to Mi Body Compositon Scale 1 / 2 and paste Bluetooth address.

      <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/userdata.jpg"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/userdata.jpg" alt="MiScale Exporter settings" style="width: 400px; margin: auto; display: block;"></a>

5. Now it's time for measurement. Stand on your scale. Measure yourself and get data from the scale. Mi Body Composition Scale is active up to 15 min after the measurement.

<a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/measure.jpg"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/measure.jpg" alt="measure" style="width: 400px; margin: auto;
    display: block;"></a>

6. These types of scales do not measure body composition. They measure weight and impedance and estimate the result based on those measurements. The exact calculation algorithm is unknown, but with the help of reverse engineers, an approximate one has been achieved, which gives a satisfactory result. However, this means that the final result may differ slightly from that provided by the application. For this calculation proper age, height and sex is needed.

## Steps to Connect Xiaomi Body Composition Scale S400:

1. Add Scale to Xiaomi Home App and do first measurement.

2. You will need scale MAC address and BLE Key from Xiaomi Cloud. You can get it on many ways but I recommend 'Xiaomi Cloud Tokens Extractor'
   Go to  <a target="_blank" rel="noopener noreferrer" href="https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor">https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor</a> and use your preferred way
   Found Xiaomi Body Composition Scale S400 on the list of your devices and copy BLE KEY and MAC and save it for later.

   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/token_extractor.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/token_extractor.png" alt="Xiaomi Cloud Tokens Extractor" style="width: 836px; margin: auto; display: block;"></a>

3. Now you need to completely kill the app (so that it doesn't run in the background either). Or remove the scale from the list of devices - if you use other devices with Xiaomi Home. The scale will only send the needed data when it is not able to connect to the Xiaomi Home app! Every time when you add Scale as new device to Xiaomi Home new BLE key will be generated. You can disable Heart rate measurement to fast up whole process.

   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/xiaomi_home.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/xiaomi_home.png" alt="Xiaomi Homer" style="width: 400px; margin: auto;
    display: block;"></a>

4. Now got MiScale Exporter settings, select S400 scale and paste MAC address and BLE Key.
   
   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/s400_settings.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/s400_settings.png" alt="S400 settings" style="width: 400px; margin: auto;
    display: block;"></a>

5. The scale only sends data in a short time window at the end of weighing, so it's important to start the measurement before stepping on the scale. The Bluetooth icon should blink. (Watch the video below)
   
   [![Watch the video](https://img.youtube.com/vi/HtOZZwnkZHw/0.jpg)](https://www.youtube.com/shorts/HtOZZwnkZHw)

7. These types of scales do not measure body composition. They measure weight and impedance and estimate the result based on those measurements. The scale sends 3 values: Weight, impedance and Heart rate. To receive body composition data, impedance is processed by an algorithm known to be similar to that used by the Mi Body Composition Scale 2 (different from that used by the S400). Because of this, the result may differ from that of the Xiaomi Home app. For this calculation proper age, height and sex is needed.

8. Bear in mind that it is an experimental solution and errors may occur. If you encounter them, please contact me.

## Garmin Connect Upload

1. After successfully retrieving data from the scale, you will be redirected to the Garmin data form.

2. Here you can see the result of your measurement and send it to the Garmin Connect cloud.
   
   <a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/bodycomposition.jpg"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/bodycomposition.jpg" alt="calculated body composition" style="width: 400px; margin: auto;
    display: block;"></a>

3. If you have set up your email and password in the settings, you do not need to enter anything else here.

4. This app support 2FA/MFA codes. If you use MFA security, you will receive a message asking you to enter a code when you first try to send a message. It should be sent to your email address or as a text message to your phone (depending on your region). Close the message and enter the code in the new MFA field that appears at the bottom of the screen.

5. You don't have to enter the code every time you try to send a message. The returned token will be saved in the app and will be valid for several months.

<a target="_blank" rel="noopener noreferrer" href="https://github.com/lswiderski/mi-scale-exporter/blob/main/resources/img/screenshots/garmin.png"><img src="https://github.com/lswiderski/mi-scale-exporter/raw/main/resources/img/screenshots/garmin.png" alt="Garmin results" style="width: 722px; margin: auto;
    display: block;"></a>
