# YouTube Playlist Monitor

When a an video in a YouTube playlist is deleted or set to private, it shows as such leaving you to wonder what that video was.
YouTube Playlist Monitor will monitor changes in your playlist and sends an email about deleted playlist items.

## Deploy

```bash

dotnet install -g LambdaSharp.Tools
lash deploy --tier devel --bootstrap --profile profilename
```

## TODO
- caching
- exponental backoff of youtube api request 
- step functions for long running playlists
- with caching then i can maybe do a fanout, but I don't know if other lambda functions are making calls at the same time 