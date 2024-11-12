# PhonieCore
A simple Raspberry Pi RFID music box implemented in .NET Core 

Inspired by the [Phoniebox project](http://phoniebox.de)

## What it does
- Plays mp3 file(s) when a corresponding RFID tag is recognized (uid)
- When a new tag is recognized a folder is created in a samba share
- When an existing tag is recognized the media files in the folder are played. 
- The folder that corresponds with the current tag is marked with an @ in the end (so that one knows where to add the media files for that tag)

## Hardware
- Raspberry pi 3b running raspbian buster lite
- rc522 RFID reader connected via SPI
- Powerbank with pass through changing (to enable use and charge at the same time)
- Nice box that fits all components
- Audio hat (WM8960)

## Installation
### Update Raspbian
```
sudo apt update
sudo apt upgrade
sudo apt-get install git nano samba avahi-daemon
```

### Samba shares
```
mkdir -p ~/phoniecore
sudo nano /etc/samba/smb.conf
```
Add these lines at the bottom to get two open shares for the executable and the media:
```
[media]
path = /media/
public = yes
writable = yes
guest ok = yes

[core]
path = /phoniecore/
public = yes
writable = yes
guests ok = yes
```

### Zeroconf (use hostname for connections) 
```
sudo nano /etc/hostname
```

### Mopidy
```
wget -q -O - https://apt.mopidy.com/mopidy.gpg | sudo apt-key add -
sudo wget -q -O /etc/apt/sources.list.d/mopidy.list https://apt.mopidy.com/buster.list
sudo apt-get update
sudo apt-get install mopidy
sudo apt-get install mopidy-spotify
sudo apt-get install python3-pip


sudo apt-get install python-spotify
sudo apt-get install libspotify-dev
sudo python3 -m pip install Mopidy-Spotify
sudo systemctl enable mopidy
```
mopidy.conf
```
[spotify]
username = ...
password = ...
client_id = ...
client_secret = ...
```

### Audio HAT Driver
```
git clone https://github.com/waveshare/WM8960-Audio-HAT
cd WM8960-Audio-HAT
sudo ./install.sh 
sudo reboot
```

### .Net Core 8
```
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --verbose
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc
source ~/.bashrc
dotnet --version
```

### PhonieCore
```
Copy the binaries to the core share
```

### Run PhonieCore using systemd
https://devblogs.microsoft.com/dotnet/net-core-and-systemd/

## ToDo
- [ ] Proper shutdown

