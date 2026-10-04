# Privacy Policy — VPUploader

**Last updated:** October 2026

VPUploader is a personal, non-commercial desktop application built by Ignacio Agustín ([github.com/nxsaldano](https://github.com/nxsaldano)) that lets a user upload images to their own Pinterest account.

## What data this app accesses

VPUploader connects to your Pinterest account via Pinterest's own official OAuth login. When you authorize the app, it can:
- Read the list of your Pinterest boards
- Create new Pins on your behalf, using images and details (title, description, link, alt text) you choose to submit

VPUploader does not access, read, or modify any other data in your Pinterest account.

## What data is stored, and where

- Your Pinterest access and refresh tokens are stored **locally on your own computer only**, encrypted using Windows' built-in Data Protection API (DPAPI), tied to your Windows user account.
- No data is ever sent to, stored on, or processed by any server operated by the developer. VPUploader has no backend of its own — it communicates directly and only with Pinterest's official API.
- Images you choose to upload are sent directly to Pinterest's servers as part of creating a Pin, and are not retained by this app beyond that upload.

## Data sharing

This app does not share, sell, or transmit your data to any third party other than Pinterest itself, as a direct and necessary part of its core function.

## Your control over this data

You can revoke VPUploader's access to your Pinterest account at any time via Pinterest's own account settings. Deleting the locally stored token file (or uninstalling the app) removes all data the app holds.

## Contact

Questions about this policy can be directed via GitHub: [github.com/nxsaldano](https://github.com/nxsaldano)
