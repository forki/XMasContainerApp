FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY /deploy/http .
WORKDIR .
EXPOSE 80
ENTRYPOINT ["dotnet", "HTTP.dll"]
