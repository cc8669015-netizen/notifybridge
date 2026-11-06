// NotifyBridge relay: the Android app posts a notification with a pairing code; this server
// looks up which Windows Phone that code belongs to and pushes a toast to it via WNS.
//
// State is kept in memory only — it resets on restart, so devices must call /register again
// after a redeploy. That's fine for a personal relay; swap in a real database for anything more.
const express = require("express");
const { sendToast, isConfigured } = require("./wns");

const app = express();
app.set("trust proxy", 1); // behind Render/Railway/Fly: use the real client IP for rate limiting
app.use(express.json({ limit: "16kb" }));

// Tiny in-memory rate limiter (per IP, per route). The 6-digit pairing code is the only secret
// protecting a phone from spam, so guessing it must be slow.
function rateLimit(maxPerMinute) {
  const hits = new Map();
  setInterval(() => hits.clear(), 60_000).unref();
  return (req, res, next) => {
    const n = (hits.get(req.ip) || 0) + 1;
    hits.set(req.ip, n);
    if (n > maxPerMinute) return res.status(429).json({ error: "Too many requests." });
    next();
  };
}

// The relay attaches its WNS access token to whatever URL it is given, so only ever accept
// genuine WNS channel URLs; otherwise /register could be used to leak the token elsewhere.
function isWnsChannel(value) {
  try {
    const u = new URL(String(value));
    return u.protocol === "https:" && u.hostname.endsWith(".notify.windows.com");
  } catch {
    return false;
  }
}

// code (string) -> { channelUri, updatedAt }
const registrations = new Map();

app.get("/", (req, res) => {
  res.json({ ok: true, wnsConfigured: isConfigured(), registeredCodes: registrations.size });
});

// Called by the Windows Phone app once it has a push channel URI.
app.post("/register", rateLimit(20), (req, res) => {
  const { code, channelUri } = req.body || {};
  if (!code || !channelUri) {
    return res.status(400).json({ error: "code and channelUri are required" });
  }

  if (!/^\d{6}$/.test(String(code))) {
    return res.status(400).json({ error: "code must be 6 digits" });
  }
  if (!isWnsChannel(channelUri)) {
    return res.status(400).json({ error: "channelUri must be a https://*.notify.windows.com URL" });
  }

  registrations.set(String(code), { channelUri, updatedAt: Date.now() });
  console.log(`Registered code ${code}`);
  res.json({ ok: true });
});

// Called by the Android app for every notification it forwards.
app.post("/notify", rateLimit(60), async (req, res) => {
  const { code, app: appName, title, text, category } = req.body || {};
  if (!code) return res.status(400).json({ error: "code is required" });

  const registration = registrations.get(String(code));
  if (!registration) {
    return res.status(404).json({ error: "No Windows Phone registered for this code yet." });
  }
  if (!isConfigured()) {
    return res.status(500).json({ error: "Server is missing WNS_PACKAGE_SID / WNS_CLIENT_SECRET." });
  }

  try {
    await sendToast(registration.channelUri, { app: appName, title, text, category });
    res.json({ ok: true });
  } catch (err) {
    console.error(err.message);
    // A stale channel URI is the most common cause: the phone reinstalled the app or the
    // channel expired. Drop it so /register has to run again before we retry.
    if (String(err.message).includes("HTTP 404") || String(err.message).includes("HTTP 410")) {
      registrations.delete(String(code));
    }
    res.status(502).json({ error: err.message });
  }
});

const port = process.env.PORT || 3000;
app.listen(port, () => console.log(`NotifyBridge relay listening on ${port}`));
