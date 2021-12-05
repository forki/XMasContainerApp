FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY /deploy/servicebus .
WORKDIR .
ENTRYPOINT ["dotnet", "ServiceBus.dll"]
