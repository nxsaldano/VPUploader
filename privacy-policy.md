# Privacy Policy — VPUploader

**Last updated:** October 2026

VPUploader is a personal, non-commercial desktop application developed by nxchos (Ignacio Agustín) that lets a user upload images to their own Pinterest account. Project website: [nxchos.dev.ar](https://nxchos.dev.ar) · Source code: [github.com/nxsaldano/VPUploader](https://github.com/nxsaldano/VPUploader)

## What data this app accesses

VPUploader connects to your Pinterest account via Pinterest's own official OAuth login. When you authorize the app, it requests the following permissions:

- **boards:read** — to show the list of your Pinterest boards so you can choose where to upload
- **pins:write** — to create new Pins on your behalf, using images and details (title, description, link, alt text) you choose to submit
- **pins:read** — to read back Pins created through the app
- **boards:write** — required by Pinterest for the Pin-creation endpoint; VPUploader does not edit or delete your existing boards
  VPUploader does not access, read, or modify any other data in your Pinterest account.

## What data is stored, and where

- Your Pinterest refresh token is stored **locally on your own computer only**, encrypted using Windows' built-in Data Protection API (DPAPI), tied to your Windows user account. Short-lived access tokens are kept in memory only while the app is running.
- No data is ever sent to, stored on, or processed by any server operated by the developer. VPUploader has no backend of its own — it communicates directly and only with Pinterest's official API.
- Images you choose to upload are sent directly to Pinterest's servers as part of creating a Pin, and are not retained by this app beyond that upload.
## Data sharing

This app does not share, sell, or transmit your data to any third party other than Pinterest itself, as a direct and necessary part of its core function.

## Your control over this data

You can revoke VPUploader's access to your Pinterest account at any time via Pinterest's own account settings. Deleting the locally stored token file (`%LOCALAPPDATA%\VPUploader\refresh_token.bin`) or uninstalling the app removes all data the app holds.

## Contact

Questions about this policy can be directed to nxchos (Ignacio Agustín) via GitHub: [github.com/nxsaldano](https://github.com/nxsaldano)