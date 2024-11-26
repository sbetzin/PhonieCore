# PhonieCore
A simple Raspberry Pi RFID music box implemented in .NET Core 

Inspired by the [Phoniebox project](http://phoniebox.de)

## What it does
- Plays mp3 file(s) when a corresponding RFID tag is recognized (uid)
- When a new tag is recognized a folder is created in a samba share
- When an existing tag is recognized the media files in the folder are played. 
- The folder that corresponds with the current tag is mapped to a symlink folder _current. So that you can upload new files to the current scanned tag

## Hardware
- Raspberry pi 3b running raspbian buster lite
- rc522 RFID reader connected via SPI
- Powerbank with pass through changing (to enable use and charge at the same time)
- Nice box that fits all components
- Audio hat (WM8960)

# Installation Guide for Audio Shield and PhonieCore

This guide provides step-by-step instructions to set up an Audio Shield on your device, configure peripherals, create Samba shares, install Mopidy with streaming libraries and web client, set up .NET, and register PhonieCore as a service.

---

## Table of Contents

- [Basis OS Installation](#basis-os-installation)
- [Enable SPI](#enable-spi)
- [Install Basic Packages](#install-basic-packages)
- [Install Audio Shield Driver](#install-audio-shield-driver)
- [Configure Peripherals](#configure-peripherals)
- [Create Samba Shares](#create-samba-shares)
- [Install Mopidy](#install-mopidy)
  - [Install Mopidy Streaming Libraries](#install-mopidy-streaming-libraries)
  - [Configure Mopidy](#configure-mopidy)
- [Install Mopidy Web Client](#install-mopidy-web-client)
- [Install .NET](#install-net)
- [Set Up PhonieCore Service](#set-up-phoniecore-service)
- [Install On-Off Shim](#install-on-off-shim)
- [Deploy New Binaries](#deploy-new-binaries)

---

# Basis OS Installation

- Raspberry Pi OS Lite
- Release date: October 22nd 2024
- System: 32-bit
- Kernel version: 6.6
- Debian version: 12 (bookworm)


# Enable SPI

sudo raspi-config

![alt text](img/configure_interface.png)

![alt text](img/enable_spi.png)

# Install Basic Packages

```bash
sudo apt-get update && sudo apt-get upgrade -y && sudo apt-get install -y git mpg123 alsa-utils nano samba avahi-daemon 
```

# Install Audio Shield Driver

Clone the WM8960 Audio HAT driver repository and install the driver:

```bash
git clone https://github.com/waveshare/WM8960-Audio-HAT
cd WM8960-Audio-HAT
sudo ./install.sh 
sudo reboot
```

---

# Configure Peripherals

Edit the `/boot/firmware/config.txt` file:

```bash
sudo nano /boot/firmware/config.txt
```

Add or modify the following lines:

```ini
dtparam=audio=off
dtoverlay=i2s-mmap
dtoverlay=wm8960-soundcard
dtoverlay=vc4-kms-v3d,noaudio
```

---

# Create Samba Shares

Create directories and set permissions:

```bash
sudo mkdir -p /srv/phoniecore
sudo chown -R nobody:nogroup /srv/phoniecore
sudo chmod -R 775 /srv/phoniecore
sudo chown -R nobody:nogroup /media
sudo chmod -R 775 /media
```

Edit the Samba configuration:

```bash
sudo nano /etc/samba/smb.conf
```

Add the following shares:

```ini
[media]
path = /media/
browseable = yes
read only = no
guest ok = yes
create mask = 0775
directory mask = 0775
force user = nobody

[phoniecore]
path = /srv/phoniecore
browseable = yes
read only = no
guest ok = yes
create mask = 0775
directory mask = 0775
force user = nobody
```

Restart the Samba service:

```bash
sudo systemctl restart smbd
```

---

# Install Mopidy

Add the Mopidy APT repository and install Mopidy:

```bash
sudo mkdir -p /etc/apt/keyrings
sudo wget -q -O /etc/apt/keyrings/mopidy-archive-keyring.gpg  https://apt.mopidy.com/mopidy.gpg
sudo wget -q -O /etc/apt/sources.list.d/mopidy.list https://apt.mopidy.com/bullseye.list
sudo apt update
sudo apt install -y mopidy gstreamer1.0-libav mopidy-local mpc mpd mopidy-mpd python3-pip
```

## Install Mopidy Streaming Libraries

Install additional GStreamer plugins:

```bash
sudo apt install -y python3-gst-1.0 gir1.2-gstreamer-1.0 gir1.2-gst-plugins-base-1.0 gstreamer1.0-plugins-good gstreamer1.0-plugins-ugly gstreamer1.0-tools
```

Enable Mopidy to start on boot:

```bash
sudo systemctl enable mopidy
```

## Configure Mopidy

Edit the Mopidy configuration file:

```bash
sudo nano /etc/mopidy/mopidy.conf
```

Update the configuration as follows:

```ini
[http]
enabled = true
hostname = 0.0.0.0
port = 6680

[audio]
output = alsasink device=hw:0,0
mixer = software

[file]
enabled = true
media_dirs = /media/

[local]
enabled = true
media_dir = /media/
scan_flush_threshold = 100

[mpd]
enabled = true
hostname = 0.0.0.0
port = 6600

[stream]
enabled = true
protocols =
    http
    https
    mms
    rtmp
    rtmps
    rtsp
timeout = 5000
metadata_blacklist =
```

Restart the Mopidy service:

```bash
sudo systemctl restart mopidy
```

---

## Install Mopidy Web Client

Install the MusicBox Webclient:

```bash
sudo pip install Mopidy-MusicBox-Webclient --break-system-packages
sudo systemctl restart mopidy
```

---

# Install .NET

Create the installation directory and install the latest version of .NET:

```bash
sudo mkdir -p /usr/share/dotnet
curl -sSL https://dot.net/v1/dotnet-install.sh | sudo bash /dev/stdin --install-dir /usr/share/dotnet --version latest --verbose
```

Update environment variables:

```bash
echo -e '\nexport DOTNET_ROOT=/usr/share/dotnet\nexport PATH=$PATH:$DOTNET_ROOT' | sudo tee -a /etc/profile
source /etc/profile
source ~/.bashrc
```

Verify the installation:

```bash
dotnet --version
```

---

# Set Up PhonieCore Service

Copy the .NET application files to `/srv/phoniecore`.

Create the service file:

```bash
sudo nano /etc/systemd/system/PhonieCore.service
```

Add the following content:

```ini
[Unit]
Description=PhonieCore Service
After=mopidy.service wm8960-soundcard.service network.target
Requires=mopidy.service wm8960-soundcard.service

[Service]
Type=simple
ExecStart=/usr/share/dotnet/dotnet /srv/phoniecore/PhonieCore.dll
Restart=always
User=phonie
Group=phonie
WorkingDirectory=/srv/phoniecore

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
sudo systemctl enable PhonieCore.service
sudo systemctl start PhonieCore.service
```

Check the service status:

```bash
systemctl status PhonieCore.service
journalctl -u PhonieCore.service -n 50
```

---

# Install On-Off Shim

Follow the instructions from the [clean-shutdown repository](https://github.com/pimoroni/clean-shutdown).

Install the On-Off Shim script:

```bash
curl https://get.pimoroni.com/onoffshim | bash
```

Edit the `/boot/firmware/config.txt` file:

```bash
sudo nano /boot/firmware/config.txt
```

Add the following lines:

```ini
dtoverlay=gpio-poweroff,gpiopin=4,active_low=1,input=1
dtoverlay=gpio-shutdown,gpio_pin=17,active_low=1
```

Configure the clean shutdown daemon:

```bash
sudo nano /etc/cleanshutd.conf
```

Update the configuration:

```ini
# Config for cleanshutd

# OnOff SHIM uses trigger 17 and poweroff 4

daemon_active=1
trigger_pin=17
led_pin=17
poweroff_pin=4
hold_time=2
shutdown_delay=0
polling_rate=1
```

---

# Deploy New Binaries

Deploy the .net binaries to the \phoniecore samba share or via ssh. Please use .net publish to create  a deployable version with all binaries and dependecies. Just copyiing the compiled bin folder is not working.

## Stop and start the PhonieCore service:

```bash
sudo systemctl stop PhonieCore.service
sudo systemctl start PhonieCore.service
```

## Check the service log for errors
```bash
journalctl -u PhonieCore -f
```

## Alternatively, start PhonieCore manually:

```bash
sudo $HOME/.dotnet/dotnet /srv/phoniecore/PhonieCore.dll
```

---

# Links

SPI
https://github.com/dotnet/iot/blob/main/Documentation/raspi-spi.md

Github Projekt
https://github.com/sbetzin/PhonieCore

Mopidy
https://docs.mopidy.com/stable/installation

Web Client
https://mopidy.com/ext/musicbox-webclient/


# Troubleshoot Befehle

## Raspi Config
```bash
sudo nano /boot/firmware/config.txt
```

## Status vom Sound Shield Service
```bash
systemctl status wm8960-soundcard.service
```

## I2C Bus auslesen und schauen ob es Geräte daran gibt
```bash
i2cdetect -y 1
i2cdetect -y 2
```

## Kernel Messages nach dem Audioshield durchsuchen
```bash
dmesg | grep wm8960
```

## Test MP3 runterladen
```bash
wget https://www.myinstants.com/media/sounds/oh-no-no-no-tik-tok-song-sound-effect.mp3
```

## Service neu laden
```bash
sudo systemctl daemon-reload
```

## Test MP3 abspielen
```bash
mpg123  oh-no-no-no-tik-tok-song-sound-effect.mp3
```

## Modipy restarten
```bash
sudo systemctl restart mopidy
```

## Journal des Services ausgeben
```bash
journalctl -u mopidy | tail -n 50
journalctl -u cleanshutd | tail -n 50
journalctl -u PhonieCore -f

mpc help
mpc stop
mpc playlist
```
