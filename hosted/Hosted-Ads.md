# Hosted Banano-Powered Advertisements
Once your instance has been set up for you, *thearistotle#1599* (discord) will provide you with your base url.  This guide assumes your instance is called **example**.

## Ad System URL
Add the base path for the BanAd container as `data-banad-url` to your `body` tag:

```html
<body data-banad-url="http://banad.net/example">
```

### Ad System Script

Add the script as served up by the ad system to your pages:

```html
  <script src="http://banad.net/example/js/banad.js"></script>
</body>
```

## Ads

You'll need to provide the information [here](https://github.com/thearistotle2/ban-ad#ads-setup) to *thearistotle#1599* (discord) for each ad slot you'd like to use.  *thearistotle#1599* (discord) will provide you with an ID for each ad slot.

Add the `data-banad-id` attribute to each of your ad slots:

```html
<div data-banad-id="provided-id"></div>
```

## That's it!

The ad system script will take care of the rest.
