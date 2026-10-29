#Steg 1: bygg appen med hele .NET SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
#Kopierer .csproj først, så restore blir cachet når pakkene ikke endres.
COPY Heimevernet.Web/Heimevernet.Web.csproj Heimevernet.Web/
RUN dotnet restore Heimevernet.Web/Heimevernet.Web.csproj

#Kopierer resten av koden og publiserer appen til /app/publish.
COPY Heimevernet.Web/ Heimevernet.Web/
RUN dotnet publish Heimevernet.Web/Heimevernet.Web.csproj --configuration Release --no-restore --output /app/publish

#Steg 2: et mindre image som bare kjører appen, uten SDK.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Heimevernet.Web.dll"]
