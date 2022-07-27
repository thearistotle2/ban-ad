# Banano-Powered Advertisements

## Docker-Compose Example

```yaml
version: '3.8'

services:

  banad:
    image: 'thearistotle/ban-ad:1.0.0'
    environment:
      - SITE_ID=My Cool Site
      - EMAIL_ADDRESS=my.cool.site@gmail.com
      - EMAIL_PASSWORD_FILE=/run/secrets/email.pwd
      - EMAIL_DISPLAY_NAME=My Cool Site
      - BANANO_NODE=https://api.banano.kga.earth/node/proxy
      - AD_APPROVER_EMAIL=my-email@pm.me
      - BANANO_PAYMENT_ADDRESS=ban_
    volumes:
      - '/run/banad/ad-slots:/run/ad-slots'
      - '/run/banad/ads:/run/ads'
      - '/run/banad/banano-tracking:/run/banano-tracking'
    networks:
      - traefik
      # The ad system needs internet access for emails and payment tracking.
      - banad-internet
    deploy:
      mode: replicated
      replicas: 1
      labels:
        - 'traefik.enable=true'
        - 'traefik.docker.network=traefik'
        - 'traefik.http.routers.banad.rule=Host(`example.com`) && PathPrefix(`/ads`)'
        - 'traefik.http.middlewares.banad.stripprefix.prefixes=/ads'
        - 'traefik.http.routers.banad.middlewares=banad'
        - 'traefik.http.routers.banad.entrypoints=web-secure'
        - 'traefik.http.routers.banad.tls'
        - 'traefik.http.services.banad.loadbalancer.server.port=2022'

  my-cool-site:
    image: 'nginx:1.21-alpine'
    volumes:
      - '/run/my-cool-site:/usr/share/nginx/html'
    networks: [ traefik ]
    deploy:
      mode: replicated
      replicas: 1
      labels:
        - 'traefik.enable=true'
        - 'traefik.docker.network=traefik'
        - 'traefik.http.routers.my-cool-site.rule=Host(`example.com`)'
        - 'traefik.http.routers.my-cool-site.entrypoints=web-secure'
        - 'traefik.http.routers.my-cool-site.tls'
        - 'traefik.http.services.my-cool-site.loadbalancer.server.port=80'

networks:
  traefik:
    external: true
  banad-internet:
    external: true
```

## Full ENV Variable List

The container can be configured using the following environment variables:

| Variable                      | Default                                                           | Description                                                                                                                                                                                           |
|:------------------------------|:------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ASPNETCORE_URLS               | http://+:2022                                                     | Port on which to listen                                                                                                                                                                               |
| SITE_ID                       | BanAd                                                             | ID of the site on which this is running; will appear in emails and page titles                                                                                                                        |
| SITE_BASE_URL                 | /ads                                                              | The path or URL where the ad system is hosted                                                                                                                                                         |
| AD_SLOT_DECLARATIONS          | /run/ad-slots                                                     | Location in the container to find ad slot configurations                                                                                                                                              |
| ADS_LOCATION                  | /run/ads                                                          | Location in the container to store ads                                                                                                                                                                |
| BANANO_TRACKING_LOCATION      | /run/banano-tracking                                              | Location in the container to track seen banano transaction hashes                                                                                                                                     |
| BANANO_HISTORY_COUNT          | 10                                                                | The number of recent transactions to check for payments                                                                                                                                               |
| EXTENSIONS                    | .png;.jpg;.jpeg;.gif                                              | The file extensions you want to support as ads, separated by semicolons                                                                                                                               |
| MAX_SIZE_KB                   | 150                                                               | The max file size for ads                                                                                                                                                                             |
| BOT_HONEYPOT_NAME             | name                                                              | The name of the hidden field to use to try to filter out bots                                                                                                                                         |
| BOT_MIN_SECONDS               | 3                                                                 | The minimum number of seconds between loading the page and submitting an ad, also to try to filter out bots                                                                                           |
| SMTP_SERVER                   | smtp.gmail.com                                                    | The URL of the SMTP server to use to send emails                                                                                                                                                      |
| SMTP_PORT                     | 587                                                               | The port of the SMTP server specified above                                                                                                                                                           |
| IMAP_SERVER                   | imap.gmail.com                                                    | The URL of the IMAP server to use to read emails                                                                                                                                                      |
| IMAP_PORT                     | 993                                                               | The port of the IMAP server specified above                                                                                                                                                           |
| BANANO_WATCH_TYPE             | RPC                                                               | `RPC` to use a node or proxy (**preferred**); `Creeper` to use a block explorer that works the same as https://creeper.banano.cc/                                                                     |
| BANANO_CREEPER_HISTORY_URL    | https://api.spyglass.pw/banano/v2/account/confirmed-transactions  | URL to which to POST to watch confirmed transactions through a block explorer                                                                                                                         |
| BANANO_CREEPER_RECEIVABLE_URL | https://api.spyglass.pw/banano/v1/account/receivable-transactions | URL to which to POST to watch receivable transactions through a block explorer                                                                                                                        |
| **EMAIL_ADDRESS**             |                                                                   | **REQUIRED**; Email address owned by the ad system                                                                                                                                                    |
| **EMAIL_PASSWORD**            |                                                                   | **REQUIRED IF NO EMAIL_PASSWORD_FILE PROVIDED**; Email password for the account specified above (or below)                                                                                            |
| **EMAIL_PASSWORD_FILE**       |                                                                   | **OPTIONAL**; File containing the email password for the account specified above (or below); preferred; use with Docker Secrets                                                                       |
| **EMAIL_USERNAME**            |                                                                   | **OPTIONAL; DEFAULTS TO EMAIL_ADDRESS**; Username to log in if separate from the email address specified above                                                                                        |
| **EMAIL_DISPLAY_NAME**        |                                                                   | **OPTIONAL; DEFAULTS TO EMAIL_ADDRESS**; A display name to use in place of the email address above when sending emails                                                                                |
| **AD_APPROVER_EMAIL**         |                                                                   | **REQUIRED**; Email address of the ad approver; the approver will receive email notifications of ad submissions, and only emails from the approver will be allowed to accept or reject ad submissions |
| **BANANO_PAYMENT_ADDRESS**    |                                                                   | **REQUIRED**; Default banano address to use for ad payments                                                                                                                                           |
| **BANANO_NODE**               |                                                                   | **REQUIRED IF BANANO_WATCH_TYPE=RPC**; URL to which to POST to watch transactions through a node or proxy; preferred                                                                                  |

## Ads Setup

In the `ad-slots` directory, create a file for each ad slot. Each file should be named as the ad slot id:

```
filename:
629B5C2E

contents:
{"banPerHour": 0, "width": 300, "height": 180 }
```

The example above is the minimum required to set up an ad slot, but the full list of settings is below:

```
{
  "banPerHour"  : int,
  "width"       : int,
  "height"      : int,
  "defaultImage": "string",
  "defaultLink" : "string",
  "banAddress"  : "string"
}
```

- `defaultLink` is only used `defaultImage` is also provided.
- `banAddress` can be used to keep ad payments separate for each ad slot.

**Ad slot ids can be generated for you by the app at `/banad/newid`.**

## Gmail Setup Instructions

You can use any SMTP with TLS and IMAP with SSL, but the defaults are set up for Gmail. In order to set up a new account
to support BanAd, you can follow these steps:

- Manage your Google account on https://myaccount.google.com.
    - Go to Security
    - Enable 2-Step Verification
    - Generate an App Password for BanAd.
        - This is the password to use as `EMAIL_PASSWORD`.
- Go to Gmail's All Settings > Forwarding and POP/IMAP and Enable IMAP.

## How to integrate into your site

### Ad System URL

Add the base path for the BanAd container as `data-banad-url` to your `body` tag:

```html
<body data-banad-url="/ads">
```

```html
<body data-banad-url="https://ads.your-site.com">
```

### Ad System Script

Add the script as served up by the ad system to your pages:

```html
  <script src="/ads/js/banad.js"></script>
</body>
```

```html
  <script src="https://ads.your-site.com/js/banad.js"></script>
</body>
```

### Ads

Add the `data-banad-id` attribute to each of your ad slots:

```html
<div data-banad-id="your-id"></div>
```

### Favicon

If you host the ad system at a path under your site, browsers should use the favicon at the base of your site automatically.  If you're running it under its own domain or subdomain, you can mount a favicon like so:

```yaml
    volumes:
      - '/path/to/favicon.ico:/app/wwwroot/favicon.ico'
```

### That's it!

The ad system script will take care of the rest.
