# simac Server installation on ubuntu.

install nunrar:

      sudo apt-get install rar unrar
or  

      sudo apt install rar unrar

install Server:
  
    apt update && apt upgrade && wget https://github.com/Slincess/simac-server/releases/download/0.04/serverapp && wget https://github.com/Slincess/simac-server/releases/download/0.04/wwwroot.rar

unpack wwwroot:

    unrar x wwwroot

turn serverapp into executable

    chmod +x serverapp

after the installation type `ip a` to see your ip.

type `./serverapp` to start the server.

# planned features
-webUI  
|>port changing  
|>server disk size  
|>server stats  

# important!  
TCP server will be on port 5000 on default.
WebUI will be on port 5001 on default.

# on process WebUI 
<img width="1912" height="960" alt="image" src="https://github.com/user-attachments/assets/d0609cc1-f98d-4b21-b51d-bd9baa4ddc8d" />


# License  
[MIT license ©](https://github.com/Slincess/simac-server/blob/main/LICENSE)
