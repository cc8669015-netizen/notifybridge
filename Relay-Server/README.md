# NotifyBridge Relay

A tiny server that lets the Android app reach your Windows Phone from anywhere, by forwarding
notifications through Microsoft's push service (WNS). Without this server, NotifyBridge only
works over local Wi-Fi (the "Local" tab in the Windows Phone app needs nothing extra).

## What it does

1. The Windows Phone app asks Windows for a push channel, then calls `POST /register` here with
   its 6-digit pairing code and that channel's address.
2. The Android app calls `POST /notify` with the same code plus the notification's app name,
   title and text.
3. This server looks up the channel for that code and asks WNS to push a toast to it. The toast
   appears on the phone even if NotifyBridge isn't open there.

## 1. Get your WNS credentials

This is the part that needs a Microsoft account and, currently, some patience — Partner Center's
WNS setup has had reported provisioning bugs recently. If you get stuck for more than about 20
minutes on any one step, the Local tab in the Windows Phone app still works with none of this.

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard) and reserve a new app
   name (Apps and games → Create a new app). You don't need to submit it to the Store.
2. Open the app → **Product management → Product identity**. Note the **Package SID**,
   **Package/Identity/Name**, **Package/Identity/Publisher**, and **Publisher Display Name**.
3. Open **Product management → WNS/MPNS → App Registration portal**. It opens an Azure AD app
   registration already linked to this Store app — use this one, not a separately created one.
4. In that Azure app, go to **Certificates & secrets → New client secret**. Copy the secret's
   *value* immediately; you can't see it again later.
5. In Visual Studio, open the NotifyBridge project → right-click the project → **Store → Associate
   App with the Store**, sign in, and pick the app you just reserved. This updates
   `Package.appxmanifest` with the matching identity — required for the push channel to work.

## 2. Deploy this server

Any Node.js host works. A free option:

1. Push this `Relay-Server` folder to its own GitHub repo.
2. Create a free web service on [Render](https://render.com) (or Railway, Fly.io) from that repo.
   Build command: `npm install`. Start command: `npm start`.
3. Add two environment variables from step 1: `WNS_PACKAGE_SID` (the Package SID, formatted as
   `ms-app://<value>`) and `WNS_CLIENT_SECRET` (the client secret).
4. Note the server's public URL, e.g. `https://notifybridge-yourname.onrender.com`.

Free tiers on most hosts sleep after inactivity and take a few seconds to wake on the next
request — the first push after a quiet period can arrive up to about a minute late.

## 3. Point the apps at it

- **Windows Phone app:** open the **Push (anywhere)** tab, type your server's address into
  **Your relay server address** (it must start with `https://`), then tap
  **Set up push notifications**. No rebuild needed, and anyone can use their own server this way.
- **Android app:** in Settings, set **Relay URL** to the same address and **Relay code** to the
  6-digit code shown on the Windows Phone app's Push tab.

## Notes

- `/register` only accepts real WNS channel URLs (`https://*.notify.windows.com`) and `/notify`
  and `/register` are rate-limited per IP, because the 6-digit code is the only thing stopping a
  stranger from sending toasts to your phone.
- WNS channels expire after about 30 days. The Windows Phone app registers again every time it is
  opened (once Push has been set up), which also covers the relay restarting and forgetting codes.
  If pushes stop arriving, open the app once.

## Testing without a phone

```
curl -X POST https://your-relay-url/notify \
  -H "Content-Type: application/json" \
  -d '{"code":"123456","app":"Test","title":"Hello","text":"It works"}'
```

Replace `123456` with the code shown in the Windows Phone app, after you've completed step 1
above (`/register` must have succeeded first).
