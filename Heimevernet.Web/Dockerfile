FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build #Steg 1: bygg appen med hele .NET SDK.
WORKDIR /src

COPY global.json ./
COPY Heimevernet.Web/Heimevernet.Web.csproj Heimevernet.Web/ #Kopierer .csproj først, så restore blir cachet når pakkene ikke endres.
RUN dotnet restore Heimevernet.Web/Heimevernet.Web.csproj

COPY Heimevernet.Web/ Heimevernet.Web/ #Kopierer resten av koden og publiserer appen til /app/publish.
RUN dotnet publish Heimevernet.Web/Heimevernet.Web.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime #Steg 2: et mindre image som bare kjører appen, uten SDK.
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Heimevernet.Web.dll"]
