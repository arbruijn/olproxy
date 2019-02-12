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

-  Start olproxy (you should see a console window with the text Ready.)

-  Start the Overload server

-  Share your IP or hostname (but only one of them, everybody must use the same text)

#### How to run on Windows

Download a binary release and run olproxy.exe

#### How to run on Linux

Install .NET Core SDK 2.2, download the source code and run:

`dotnet run -f netcoreapp2.2`
