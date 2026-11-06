# NotifyBridge

Forward notifications from an **Android** phone to a **Windows 10 Mobile** phone: messages, banking apps, or any app you choose. Notifications appear on Windows Phone just like on a smartwatch.

> Windows 10 Mobile has reached end of support since January 14, 2020. This project is intended for those who are still using such devices and want to receive notifications from their primary phone.

[🇻🇳 BẠN LÀ NGƯỜI VIỆT NAM HẢ?, NHẤN VÀO ĐÂY NÈ 🇻🇳](README_VN.md)
## Table of Contents

- [Two modes of operation](#two-modes-of-operation)
- [Project structure](#project-structure)
- [Quick start with Local mode](#quick-start-with-local-mode)
- [Quick start with Push mode](#quick-start-with-push-mode)
- [Building Windows Phone app and Setting up your own relay server (Push mode)](#building-windows-phone-app-and-setting-up-your-own-relay-server-push-mode)
- [Troubleshooting](#troubleshooting)
- [Privacy and security](#privacy-and-security)
- [Known limitations](#known-limitations)

## Two modes of operation

| | Local (same Wi-Fi) | Push (anywhere) |
| --- | --- | --- |
| Additional setup required | No | Yes: Microsoft Partner Center account and a relay server |
| When it works | Both devices on the same Wi-Fi, Windows Phone app must be open and screen on | Even when the app is closed, screen is off, or outside Wi-Fi |
| Latency | Instant | Instant, except for the first time after the relay "sleeps" (see limitations section) |

You can use both. The Android app sends to all destinations you have configured.

> Windows suspends apps when you switch to another app or lock the screen, so Local mode stops receiving then. Use Push mode if you want to receive notifications with the screen off.

## Project structure

| Directory | Content |
| --- | --- |
| `Receiver-WindowsPhone/` | UWP app for Windows Phone. Has Local tab and Push tab |
| `Relay-Server/` | Relay server written in Node.js, only required for Push mode |

## Quick start with Local mode

This mode requires no account or server.

1. Install the Windows Phone app.
2. Open the app, go to the **Local (same Wi-Fi)** tab, and turn on the **Listening** switch. The app will display an address like `192.168.1.23:48651`.
3. Install the Android app, grant notification access permissions, and select the apps to forward.
4. In the Android app, enter that address into the **Local address** field, then enable **Forward selected notifications**.
5. Send a test message and check your Windows Phone.

## Quick start with Push mode

**On Windows Phone** (*Push (anywhere)* tab):

1. Type the relay address into the **Your relay server address** field. Default server address is `https://notifybridge-relay.onrender.com` (Used for the default app, anyone hosting their own server must build their own app)
2. Tap **Set up push notifications**. Upon success, it will show "Registered" along with a **6-digit code**.

**On Android:**

1. Tap **Grant notification access** and enable NotifyBridge. On Android 13 or higher, if it reports "Restricted setting", go to *Settings → Apps → NotifyBridge*, tap the three dots menu, choose **Allow restricted settings**, and then enable it again.
2. Tap **Choose apps** and select the apps you want to forward.
3. Fill in the **Relay URL** (relay address, without `/notify` at the end) and **Relay code** (6-digit code).
4. Enable **Forward selected notifications**.
5. Go to the app's battery settings and select **Unrestricted** so Android does not kill the app in the background.
---

## Building Windows Phone app and Setting up your own relay server (Push mode)

**Setting up relay server**

This step is done **only once**. For those who want high-speed notifications. Instructions use Render (free tier available), but the relay can run on any server with Node.js 18 or higher.

### Step 1. Register a Microsoft Developer Account

1. Go to [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com) and register for an **Individual** account using your personal Microsoft account.
2. Microsoft currently does not charge registration fees for individual accounts in many countries. Check the amount displayed on the registration page before paying. You will need an ID card/passport and a selfie for verification.
3. Log in to [Partner Center](https://partner.microsoft.com/dashboard). Wait a few minutes until you see the **Apps and games** section.

### Step 2. Obtain WNS Keys

1. In **Apps and games**, click *New product → MSIX or PWA app*, set a name (e.g. `notifybridge`) and click *Reserve product name*. No need to submit the app.
2. Open that app, go to *Product management → Product Identity*, and copy the **Package SID**. When using it, include the full `ms-app://` prefix (e.g. `ms-app://S-1-15-2-...`).
3. Go to *Product management → WNS/MPNS*, click the **App Registration portal** link. On the Azure page that opens, select *Client credentials*, tab **Client secrets**, click **+ New client secret**, select the longest expiration period, then click **Add**.
4. Copy the **Value** column of the secret immediately, as it is only displayed once.

These two values will be `WNS_PACKAGE_SID` and `WNS_CLIENT_SECRET`. **Do not upload them to GitHub or share them with anyone.**

### Step 3. Deploy the relay code online

**Using Render:**

1. Create a GitHub repository (preferably **Private**) containing the code. There are two options:
   - Repository contains only the contents of the `Relay-Server` directory (`server.js`, `wns.js`, `package.json`, etc.) at the root level, or
   - Repository contains the whole project, and when creating the service on Render, set **Root Directory** to `Relay-Server`.
2. Go to [render.com](https://render.com), log in with GitHub, click **New → Web Service**, and select the repository.
3. Fill in:
   - **Build Command:** `npm install`
   - **Start Command:** `npm start`
   - **Instance Type:** Free (or a paid tier if you want to avoid "sleeping")
4. Open the **Environment** section and add two variables:

   | Key | Value |
   | --- | --- |
   | `WNS_PACKAGE_SID` | Package SID, with `ms-app://` prefix |
   | `WNS_CLIENT_SECRET` | The secret value from Azure |

5. Click **Create Web Service** (or *Save changes* if already created) and wait a few minutes.
6. Copy the service address, formatted like `https://your-name.onrender.com`.

**Other servers (VPS, Railway, Fly.io, ...):** run `npm install` then `npm start`, set the two environment variables above. The server listens on the port defined in the `PORT` variable (default is 3000). You need to run it behind an **https** address.

### Step 4. Verification

Open the relay address in a browser. Seeing the following result means the server is running correctly and has received the keys:

```json
{"ok":true,"wnsConfigured":true,"registeredCodes":0}
```

If `wnsConfigured` is `false`, re-check the two environment variables (correct names, no extra spaces or quotes).

**Build Windows phone app**

### Step 1. You need a Windows PC with **Visual Studio** (2019 recommended, 2022 works too), install **Universal Windows Platform development** workload and Windows 10 SDK (e.g., 10.0.15063). App requires Windows 10 build 1607 (10.0.14393) or higher.

1. Open `Receiver-WindowsPhone/NotifyBridge.sln`.
2. **Push mode only:** perform [Step 5 in relay setup section](#step-2-associate-windows-phone-app-identity-with-partner-center) to associate app identity. Local mode does not require this step.
3. On your phone, turn on *Settings → Update & security → For developers → Developer mode*.
4. Connect via USB, select **ARM** configuration and **Device** target, then press F5. (Or build the app package and sideload it onto the device)

### Step 2. Associate Windows Phone app identity with Partner Center

This is the easiest step to get wrong. WNS only delivers notifications when **the identity of the app on the phone matches the app whose name you reserved** in Partner Center. If it doesn't match, the relay will be rejected by Microsoft with an `HTTP 403` error, even if everything else is correct.

**2.1. Get values from Partner Center.** Open your app, go to *Product management → Product Identity* and note down:

| Value | Used for |
| --- | --- |
| `Package/Identity/Name` | `Name` attribute of `<Identity>` |
| `Package/Identity/Publisher` (format `CN=...`) | `Publisher` attribute of `<Identity>` |
| `Package/Properties/PublisherDisplayName` | `<PublisherDisplayName>` |
| `Package Family Name (PFN)` | For cross-checking in step 5.3 |
| `MSA app Id` (under *MSA app* section) | `PhoneProductId` attribute |

**2.2. Edit `Receiver-WindowsPhone/Package.appxmanifest`.** Right-click the file, select **View Code**, and edit:

```xml
<Identity Name="<Package/Identity/Name>"
          Publisher="<Package/Identity/Publisher>"
          Version="1.0.0.0" />
<mp:PhoneIdentity PhoneProductId="<MSA app Id>"
                  PhonePublisherId="00000000-0000-0000-0000-000000000000" />
```

Inside `<Properties>`, set `<PublisherDisplayName>` exactly as shown in Partner Center. Keep `PhonePublisherId` as all zeroes.

Instead of editing manually, you can try right-clicking the project, selecting *Publish → Associate App with the Store*. If Visual Studio reports "The store blocked this account", use the manual edit method above.

**2.3. Check in Packaging tab.** Open `Package.appxmanifest` using the visual editor. The **Package family name** row must be **identical** to the PFN on Partner Center. If *Publisher* automatically changes to a different value (e.g. `CN=Admin`), it's because the test signing certificate uses a different name:
- Click **Choose Certificate... → Create...**
- In **Publisher common name**, type the Publisher value **without the `CN=` part**
- Go back and check Publisher and Package family name

**2.4. Uninstall ALL NotifyBridge entries on the phone** (if you installed your shared app previously), in Visual Studio select *Build → Rebuild Solution*, reinstall the new app to the device

**2.5. Confirm on phone.** Open the **Push (anywhere)** tab, scroll down to the bottom and read the **App identity** line. It must match your PFN. Only tap **Set up push notifications** when it matches.

> On my test device, Push only worked after completing all the above steps, including setting `PhoneProductId`. It is unclear which specific step is mandatory, so please do all of them. Do not overwrite `Package.appxmanifest` with another file after you finish editing.
> Once completed, test it out as described in the [Quick start with Push mode](#quick-start-with-push-mode) section, but replace it with your server.

### Relay Endpoints

| Method | Path | Usage |
| --- | --- | --- |
| `GET` | `/` | Check server and WNS status |
| `POST` | `/register` | Windows Phone app registers a 6-digit code along with its push channel |
| `POST` | `/notify` | Android app sends a notification to a 6-digit code |

The relay only stores data **in memory**. Upon restarting, it forgets all codes, but the Windows Phone app will automatically re-register every time you open it.

---

## Troubleshooting

| Symptom | Solution |
| --- | --- |
| `wnsConfigured` is `false` | Check the names and values of the two environment variables, then let Render restart |
| Failed to register push | Relay address must start with `https://` and open in a browser |
| Relay log reports `HTTP 403` | App identity does not match Partner Center. Repeat [Step 5](#step-2-associate-windows-phone-app-identity-with-partner-center) and compare the **App identity** line on phone with PFN |
| **App identity** line is a random string | Phone is still running the old package. Completely uninstall all NotifyBridge entries, Rebuild and reinstall |
| `/notify` reports code not found | Open the Windows Phone app once to re-register, as the relay forgets codes on restart |
| The first notification arrives very late | Free tier relay is "sleeping", wait up to about a minute. Subsequent notifications arrive immediately |
| Push stops after about a month | WNS channel expired. Open the Windows Phone app once to re-register |
| App list on Android is empty | Use the latest version of the app (which declares app list query permissions for Android 11+) |
| Cannot grant notification permissions on Android 13+ | Use *Allow restricted settings* as shown above |
| Secret expired | Create a new secret in Azure and update `WNS_CLIENT_SECRET` |

## Privacy and security

- Everything you choose to forward, **including banking notifications and OTP codes**, will appear on the lock screen and Action Center of your Windows Phone. Only select apps you are comfortable displaying there.
- In Push mode, content passes through your self-hosted relay and Microsoft's push service.
- The 6-digit code is the only thing preventing strangers from sending fake notifications to your device. The relay includes rate-limiting by IP to slow down guessing attempts, but do not share your code.
- The relay only accepts channel addresses matching `https://*.notify.windows.com`, to prevent abuse of sending WNS tokens elsewhere.
- Never commit `WNS_CLIENT_SECRET` to GitHub. Only set it in the host's environment variables.

## Known limitations

- Windows 10 Mobile is no longer updated, and there is no guarantee that WNS will continue to deliver pushes reliably to Mobile devices. Local mode serves as a fallback option that always works.
- Render's free tier sleeps after about 15 minutes of inactivity.
- Relay stores codes in memory, so they are lost on restart. Fine for personal use, but multi-user setups will slow down.
- Anyone building a relay must use the WNS keys of **their own registered app** in Partner Center. Keys from other users cannot be shared.
