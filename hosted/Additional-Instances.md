# Adding an additional instance of Ban-Ad
This guide assumes you are adding a new instance called **example**.

## First Time
Run the following only the first time:

```
mkdir ~/banad/hosted
```

## Directories
Run the following:

```
mkdir ~/banad/hosted/example/ad-slots ~/banad/hosted/example/ads ~/banad/hosted/example/banano-tracking
```

## NGINX Configuration
Create a file at `~/banad/nginx/example.conf` with the following contents:

```
server {
    # proxy /example/ to banad
    location ^~ /example/ {
        rewrite /example(.*)$ $1 break;
        proxy_pass   http://example:2022/;
    }
}
```

## Docker Compose
### Ban-Ad Instance
Add a service between the `banad` service and the `EXTERNAL DOCKER ITEMS`:

```
  example:
    image: 'thearistotle/ban-ad:1.0.1'
    ports: ['2022:2022']
    secrets: [email.pwd]
    environment:
      - BANANO_WATCH_TYPE=Creeper
      - EMAIL_PASSWORD_FILE=/run/secrets/email.pwd
      - EMAIL_ADDRESS=bananoadv@gmail.com
      - EMAIL_DISPLAY_NAME=BananoAD
      ## CHANGE EVERYTHING BELOW HERE
      - SITE_ID=Example
      - AD_APPROVER_EMAIL=approver@example.com
      - BANANO_PAYMENT_ADDRESS=ban_
      ## STOP
    volumes:
      - '/home/azureuser/banad/hosted/example/ad-slots:/run/ad-slots'
      - '/home/azureuser/banad/hosted/example/ads:/run/ads'
      - '/home/azureuser/banad/hosted/example/banano-tracking:/run/banano-tracking'
    networks: [banad]
    deploy:
      mode: replicated
      replicas: 1
```

You'll need to change the environment variables under the `CHANGE EVERYTHING BELOW HERE` comment to match the instance owner's information.

### NGINX Instance
Add the following line under `volumes`:

```
      - '/home/azureuser/banad/nginx/example.conf:/etc/nginx/conf.d/example.conf:ro'
```

### Update the Docker stack
Run the following:

```
cd ~/banad && docker stack deploy -c docker-compose.yaml ${PWD##*/}
```