# Setup notes

## Project layout

```
VPUploader.sln
VPUploader.Core/            - the real logic, no test dependencies
    ITokenProvider.cs         - interface PinterestClient depends on for auth
    ITokenStore.cs            - interface AuthService depends on for persistence
    DpapiTokenStore.cs        - real ITokenStore: encrypts refresh token to disk (DPAPI)
    AuthService.cs            - implements ITokenProvider; OAuth flow + refresh
    PinterestClient.cs        - GetBoardsAsync / CreateBoardAsync / CreatePinAsync
    Models/Board.cs, PinRequest.cs
VPUploader.Core.Tests/      - xUnit project, zero network/filesystem/browser calls
    Testing/FakeHttpMessageHandler.cs  - queue canned responses, records requests sent
    Testing/InMemoryTokenStore.cs      - fake ITokenStore
    Testing/FakeTokenProvider.cs       - fake ITokenProvider (canned token)
    PinterestClientTests.cs
    AuthServiceTests.cs
VPUploader.App/             - WPF front end (MVVM)
    ViewModels/MainViewModel.cs
    Commands/RelayCommand.cs
    MainWindow.xaml(.cs), App.xaml(.cs)
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
integration point, not a unit, and it only runs when you click "Connect to
Pinterest".

## Running the tests

```
dotnet test
```

No Pinterest account, credentials, or network connection required for any of
these tests to pass.

## Wiring up the real app

1. Register the app on Pinterest's developer portal (business account
   may be required) and add the redirect URI `http://localhost:8756/callback/`
   exactly. Pinterest only accepts `http` redirects for `localhost`, not
   `127.0.0.1`.
2. Create `VPUploader.App/appsettings.local.json` (gitignored) with your App ID
   and App secret - see the README.
3. **Trial access is a sandbox.** Pinterest only lets Trial apps call
   `https://api-sandbox.pinterest.com`, and sandbox tokens come from
   `https://api-sandbox.pinterest.com/v5/oauth/token`. Pins and boards created
   there are only visible to the creating account. Both URLs are constants in
   `PinterestClient.cs` and `AuthService.cs`; switch them back to
   `api.pinterest.com` once Standard access is approved.
4. The sandbox starts with no boards, so `MainViewModel.LoadBoardsAsync`
   temporarily creates a "VPUploader Sandbox" board if the list is empty.
   Remove that block when moving to production.
## How login works

The app never opens the browser by surprise:

- On startup, if a refresh token is saved, boards load silently.
- Otherwise the window shows a **Connect to Pinterest** button. Clicking it
  calls `AuthService.SignInAsync()`, which opens the browser for the one-time
  consent screen.
- `GetValidAccessTokenAsync()` only refreshes from a saved token; with no
  saved login it throws instead of opening a browser.
  The encrypted refresh token lives at
  `%LOCALAPPDATA%\VPUploader\refresh_token.bin`. Delete that file (or call
  `DpapiTokenStore.Clear()`) to force re-authorization - needed, for example,
  after changing the requested scopes or switching between sandbox and
  production, since a token issued by one environment is rejected by the other.

## Note on API details

Pinterest's API has moved around before (v3 -> v4 -> v5, scope names, refresh
behavior). Everything here matches the current v5 docs as of this writing, but
if you hit unexpected 400/401/403s, it's worth a quick check against
https://developers.pinterest.com/docs/ before assuming the code is wrong.