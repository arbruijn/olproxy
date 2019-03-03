## olproxy - Overload LAN proxy

**Run Overload LAN games over the internet**

[Overload](https://playoverload.com) is a registered trademark of [Revival Productions, LLC](https://www.revivalprod.com).
This is an unaffiliated, unsupported tool. Use at your own risk.

#### If you want to use somebody's server running olproxy

- Start olproxy (you should see a console window with the text Ready.)

- Start Overload

- Create/Join a LAN match

- Use the server IP or hostname as the password

#### If you want to host a server

-  Make sure UDP ports 7000-8001 are open/forwarded to the server computer. You
   can use a guide on the internet to configure your router, for example https://portforward.com/router.htm

-  If you want your server to appear on http://olproxy.otl.gg/, edit
   `appsettings.json`. Change the value after `isServer` from `false` to `true` and set
   the `serverName` and `notes` text. Your server will appear after starting the first match.

-  Start olproxy (you should see a console window with the text Ready.)

-  Start the Overload server

-  Share your internet (WAN) IP or hostname (but only one of them, everybody must use the same text)

#### How to run on Windows

Download a binary release and run olproxy.exe

#### How to run on Linux

Install .NET Core SDK 2.2, download the source code and run:

`dotnet run -f netcoreapp2.2`

#### How does it work

Overload sends broadcast packets to every host on the LAN to look for a server. The olproxy program intercepts these broadcast packets and sends them to a remote server, based on the password in the packets. When the remote server responds, it sends the responses back as broadcast packets that Overload will see. This way Overload can find the remote server. To make sure Overload connects to the remote server, the olproxy program changes the IP adres from the server in the packets.

After a server is selected, olproxy is no longer involved. Overload will then communicate directly with the remote server.
