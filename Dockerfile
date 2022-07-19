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
ENV SITE_ID=BanAd
ENV AD_SLOT_DECLARATIONS=/run/ad-slots
ENV ADS_LOCATION=/run/ads
ENV EXTENSIONS=.png;.jpg;.jpeg;.gif
ENV MAX_SIZE_KB=150
ENV BOT_HONEYPOT_NAME=name
ENV BOT_MIN_SECONDS=5

HEALTHCHECK CMD curl --fail http://localhost:2022/banad/newid || exit 1

ENTRYPOINT [ "dotnet", "BanAd.dll" ]
