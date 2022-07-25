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
ENV SITE_BASE_URL=/ads
ENV AD_SLOT_DECLARATIONS=/run/ad-slots
ENV ADS_LOCATION=/run/ads
ENV BANANO_TRACKING_LOCATION=/run/banano-tracking
ENV BANANO_HISTORY_COUNT=10
ENV EXTENSIONS=.png;.jpg;.jpeg;.gif
ENV MAX_SIZE_KB=150
ENV BOT_HONEYPOT_NAME=name
ENV BOT_MIN_SECONDS=3
ENV SMTP_SERVER=smtp.gmail.com
ENV SMTP_PORT=587
ENV IMAP_SERVER=imap.gmail.com
ENV IMAP_PORT=993
ENV BANANO_WATCH_TYPE=RPC
ENV BANANO_CREEPER_HISTORY_URL=https://api.spyglass.pw/banano/v2/account/confirmed-transactions
ENV BANANO_CREEPER_RECEIVABLE_URL=https://api.spyglass.pw/banano/v1/account/receivable-transactions
#ENV EMAIL_ADDRESS=             REQUIRED
#ENV EMAIL_PASSWORD=            REQUIRED IF NO EMAIL_PASSWORD_FILE PROVIDED
#ENV EMAIL_PASSWORD_FILE=       OPTIONAL
#ENV EMAIL_USERNAME=            OPTIONAL; DEFAULTS TO EMAIL_ADDRESS
#ENV EMAIL_DISPLAY_NAME=        OPTIONAL; DEFAULTS TO EMAIL ADDRESS
#ENV AD_APPROVER_EMAIL=         REQUIRED
#ENV BANANO_PAYMENT_ADDRESS=    REQUIRED
#ENV BANANO_NODE=               REQUIRED IF BANANO_WATCH_TYPE=RPC

HEALTHCHECK CMD curl --fail http://localhost:2022/banad/newid || exit 1

ENTRYPOINT [ "dotnet", "BanAd.dll" ]
