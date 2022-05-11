# Instructions

1. Run server in *GrpcTestService*: `dotnet run -c Release`
2. Run client in *GrpcTestClient*: `dotnet run -c Release`

Test can be modified to use `WinHttpHandler`:

1. In the client's *Program.cs* uncomment configuring `WinHttpHandler` where channel is created and change address to `https`.
2. In the server's *Program.cs* uncomment `UseHttps()` on the port.