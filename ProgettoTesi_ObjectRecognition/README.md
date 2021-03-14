# Creazione modulo di riconoscimento oggetti in AR con uso di Neural Network

## POC_PhoneBatteryReplace_Barracuda
Object detection app build on Unity Barracuda and YOLOv2 Tiny
This project is based on: https://github.com/wojciechp6/YOLO-UnityBarracuda

## About
This project implements a simple open source project presenting example of use Unity Barracuda. It use lite version of YOLOv2 (v3 is not currently supported). Target platform are mobile devices but you can use it also on other devices. This sample uses camera as input.
This project also implements TXT's WEAVR technology. It is used for writing the battery replacement procedure.


## Dependencies
- [**Unity Barracuda**](https://docs.unity3d.com/Packages/com.unity.barracuda@1.2/manual/index.html) installed by Package Manager (tested on version 1.2.0)
- **YOLOv2 Tiny model** already contained in Assets. There are two models. One has been trained on a dataset of 4 different cards. The other one has been trained on a dataset with 3 different classes on a Samsung Galaxy S2.

## How to run
***Just open Scenes/POC_PhoneBatteryReplace and run!***
- Set the value "Extra" in "CameraAR" to specify if you have extra to draw.
- If necessary also set the "support" variable
For the cards model run without extra and support.
For the Galaxy S2 model run with extra=arrow.

There are only two MonoBehaviour scripts:
- *WebCamDetector.cs* which take texture from camera and run model
- *OnGUICanvasRelativeDrawer.cs* required by previous script to render text

## Performance 
It runs in ~30FPS on my laptop (GeForce 940MX and i7-7700HQ).



