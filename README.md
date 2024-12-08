<p align="center">
   <img src="logo.png" alt="Icon" />
</p>

**Spoofy** is  an asynchronous Spotify extended streaming history parser intended to be as fast as possible while providing detailed statistics, built using [CliFx](https://github.com/Tyrrrz/CliFx) for the frontend. 

## Usage
Spoofy supports analyzing data regarding tracks, albums, and artists, showing both streams and time listened.

    Spoofy <track|album|artist> [options]

### Options

`--num, -n` - Number of top tracks the user wants to show.

`--time, -t` - Shows time listened instead of streams. Does not take an input.

`--path, -p` - Provides an alternate path for Spoofy to look for Spotify streaming history from. Not recommended unless you need to have it somewhere else. Default path is the `data` folder next to the program.

## Setup

Spoofy **requires** a Spotify App to be created in order to access the API. To create one, head towards https://developer.spotify.com/dashboard and create a new app. When prompted to select which API/SDKs you're planning to use, select the Web API.

Once you've created your bot, you can go into the Basic Information page, where you'll find your client ID and client secret. Go into the `data` folder next to the program and create a new .ini file, named `client.ini`. Here you will put your 2 values.

The `client.ini` file should be set-up like this:

```
[keys]
client_id = <CLIENT ID>
client_secret = <CLIENT SECRET>
```

Once that is all complete, you can place your extended streaming history **(MUSIC ONLY)** in the same folder and it'll be setup!
