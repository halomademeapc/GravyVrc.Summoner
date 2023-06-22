![Summoner Logo](https://github.com/halomademeapc/GravyVrc.Summoner/blob/main/assets/app_tile_icon_300px.png?raw=true)
# GravyVRC Summoner
Control your VRChat avatar with NFC tags!


![Main Build](https://github.com/halomademeapc/GravyVrc.Summoner/actions/workflows/ci-build.yml/badge.svg)

## Hardware
Summoner was developed against this hardware.  I cannot guaruntee compatibility with other scanners/tags.
- [ACR122U](https://www.acs.com.hk/en/products/3/acr122u-usb-nfc-reader/)
- [NTAG213](https://www.nxp.com/products/rfid-nfc/nfc-hf/ntag-for-tags-and-labels/ntag-213-215-216-nfc-forum-type-2-tag-compliant-ic-with-144-504-888-bytes-user-memory:NTAG213_215_216)

You should be able to find these easily on your favorite online marketplace.

## Installation

### Microsoft Store
The easiest way to install summoner is via the Microsoft Store.  
![[Get it from the Microsoft Store](https://apps.microsoft.com/store/detail/9PBSBFXXP0DF?launch=true&mode=mini)](https://get.microsoft.com/images/en-US%20dark.svg)

### Manual Installation
You can also head to the [releases](https://github.com/halomademeapc/GravyVrc.Summoner/releases) page and download unpackaged builds from there for your platform.

## Usage
![image](https://github.com/halomademeapc/GravyVrc.Summoner/assets/5904472/d33a4426-c59e-4db3-a92a-810ef4432c75)

Click **Add Parameter** to add parameters to set on your avatar anytime the tag is scanned while the app is open.  

![image](https://github.com/halomademeapc/GravyVrc.Summoner/assets/5904472/2d8e5483-2eaf-40b9-91d0-4d7f8af46e4e)

Parameters can be a integer, float, or boolean type.

![image](https://github.com/halomademeapc/GravyVrc.Summoner/assets/5904472/f31216f2-c4bc-45d2-8a72-148f8131b691)

The **Write Tag** button will be enabled if all parameters are valid and a tag is detected on a compatible reader.  You can also use the **Set Value** button to test your parameters without overwriting a tag or without a tag present.  Be aware that placing a tag on the scanner will load its values into the UI so make sure you place the tag down before you start making your edits otherwise they may be lost.

## Technical Bits
### Libraries
Summoner's UI is built on MAUI with .NET 7.  The app talks to your NFC reader using [pcsc-sharp](https://github.com/danm-de/pcsc-sharp) and to VRChat via [VRCOscLib](https://github.com/ChanyaVRC/VRCOscLib).  Data is serialized using [MessagePack](https://github.com/neuecc/MessagePack-CSharp).

### Data Format
Data is stored on the tags in 4-byte blocks using the following arrangement:
| Block | Usage                 |
|-------|-----------------------|
| 4     | Identifying Signature |
| 5     | Content Size Header   |
| 6..   | Serialized Content    |
