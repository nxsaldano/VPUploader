# Setup notes

## Project layout so far

```
VPUploader.sln
VPUploader.Core/            - the real logic, no test dependencies
    ITokenProvider.cs         - interface PinterestClient depends on for auth
    ITokenStore.cs             - interface AuthService depends on for persistence
    DpapiTokenStore.cs        - real ITokenStore: encrypts refresh token to disk (DPAPI)
    AuthService.cs             - implements ITokenProvider; OAuth flow + refresh
    PinterestClient.cs         - GetBoardsAsync / CreatePinAsync
    Models/Board.cs, PinRequest.cs
VPUploader.Core.Tests/       - xUnit project, zero network/filesystem/browser calls
    Testing/FakeHttpMessageHandler.cs  - queue canned responses, records requests sent
    Testing/InMemoryTokenStore.cs      - fake ITokenStore
    Testing/FakeTokenProvider.cs       - fake ITokenProvider (canned token)
    PinterestClientTests.cs
    AuthServiceTests.cs
```

## Why this shape

`PinterestClient` doesn't depend on `AuthService` directly - it depends on the
`ITokenProvider` interface. Tests hand it a `FakeTokenProvider` that returns a
canned string, so `PinterestClient` tests never touch OAuth, DPAPI, or the
network at all - they only care that the right HTTP request gets built and the
response gets parsed correctly.

`AuthService` doesn't write to disk directly either - it depends on
`ITokenStore`. Tests use `InMemoryTokenStore` instead of `DpapiTokenStore`, so
refresh-token logic can be tested without ever touching a real file.

Both classes also take an optional `HttpClient` in the constructor. Pass
nothing and you get a real one (that's what the actual app does); pass one
built from `FakeHttpMessageHandler` and no request ever leaves the machine.

**What's still NOT unit tested, on purpose:** `RunAuthorizationFlowAsync` (the
part that opens a real browser and waits on a local `HttpListener`). That's an
integration point, not a unit, and it only runs once per machine anyway - the
one time you'll actually exercise it is your real first run (and your Pinterest
review recording).

## Running the tests

```
dotnet test
```

No Pinterest account, credentials, or network connection required for any of
these tests to pass.

## Wiring up the real app (unchanged from before)

1. Register the app on Pinterest's developer portal (business account
   required), add redirect URI `http://127.0.0.1:8756/callback/` exactly.
2. Trial access is enough to develop and test against the real API - pins
   you create work but aren't publicly visible until Standard access is
   approved (a short video-demo review).

```csharp
var auth = new AuthService(clientId: "YOUR_APP_ID", clientSecret: "YOUR_APP_SECRET");
var client = new PinterestClient(auth); // AuthService implements ITokenProvider

var boards = await client.GetBoardsAsync();
var pinId = await client.CreatePinAsync(new PinRequest
{
    BoardId = boards[0].Id,
    ImagePath = @"C:\Users\you\Pictures\example.jpg",
    Title = "Optional title",
});
```

The first call to `GetValidAccessTokenAsync()` with no saved refresh token
opens your browser for the one-time consent screen; after that, the encrypted
token at `%LOCALAPPDATA%\PinUploader\refresh_token.bin` means you won't see
it again. Delete that file (or call `DpapiTokenStore.Clear()`) to force
re-authorization.

## Note on API details

Pinterest's API has moved around before (v3 -> v4 -> v5, scope names, refresh
behavior). Everything here matches the current v5 docs as of this writing, but
if you hit unexpected 400/401s, it's worth a quick check against
https://developer.pinterest.com/docs/api/v5/ before assuming the code is wrong.
