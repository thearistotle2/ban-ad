FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

WORKDIR /app
COPY ./ ./
RUN dotnet restore
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app
COPY --from=build-env /app/out .

RUN apt-get update && apt-get install -y curl

ENV ASPNETCORE_URLS=http://+:2022

HEALTHCHECK CMD curl --fail http://localhost:2022/banad/newid || exit 1

ENTRYPOINT [ "dotnet", "BanAd.dll" ]
