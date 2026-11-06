// Talks to Microsoft's push service (WNS). Needs the three values from Partner Center / Azure —
// see README.md "Get your WNS credentials".
const RAW_SID = process.env.WNS_PACKAGE_SID || "";
const RAW_SECRET = process.env.WNS_CLIENT_SECRET || "";
const PACKAGE_SID = RAW_SID.trim();     // "ms-app://..." — your client_id
const CLIENT_SECRET = RAW_SECRET.trim(); // your client_secret

// Shows enough of the SID to compare with Partner Center without revealing anything secret.
const maskedSid = PACKAGE_SID.length > 24
  ? PACKAGE_SID.slice(0, 20) + "..." + PACKAGE_SID.slice(-6)
  : PACKAGE_SID || "(empty)";
console.log(`WNS_PACKAGE_SID in use: ${maskedSid} (length ${PACKAGE_SID.length})`);
console.log(`WNS_CLIENT_SECRET length: ${CLIENT_SECRET.length}`);
if (RAW_SID !== PACKAGE_SID || RAW_SECRET !== CLIENT_SECRET) {
  console.warn("Warning: a WNS variable had spaces or line breaks around it; they were removed.");
}
if (PACKAGE_SID && !PACKAGE_SID.toLowerCase().startsWith("ms-app://")) {
  console.warn("Warning: WNS_PACKAGE_SID should start with ms-app://");
}

let cachedToken = null;
let cachedTokenExpiresAt = 0;

// WNS access tokens last about 24 hours; cache and only re-fetch once it's close to expiring.
async function getAccessToken() {
  if (cachedToken && Date.now() < cachedTokenExpiresAt - 60_000) return cachedToken;

  const body = new URLSearchParams({
    grant_type: "client_credentials",
    client_id: PACKAGE_SID,
    client_secret: CLIENT_SECRET,
    scope: "notify.windows.com",
  });

  const response = await fetch("https://login.live.com/accesstoken.srf", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
  });
  const data = await response.json();
  if (!response.ok || !data.access_token) {
    throw new Error("WNS token request failed: " + JSON.stringify(data));
  }

  cachedToken = data.access_token;
  cachedTokenExpiresAt = Date.now() + Number(data.expires_in || 3600) * 1000;
  return cachedToken;
}

function escapeXml(text) {
  return String(text ?? "").replace(/[<>&'"]/g, (c) => (
    { "<": "&lt;", ">": "&gt;", "&": "&amp;", "'": "&apos;", '"': "&quot;" }[c]
  ));
}

// Sends a toast: it appears on the phone (Action Center / lock screen) even if the app is closed.
async function sendToast(channelUri, notification) {
  const xml =
    "<toast><visual><binding template='ToastGeneric'>" +
    `<text>${escapeXml(notification.app || "Notification")}</text>` +
    `<text>${escapeXml(notification.title || "")}</text>` +
    `<text>${escapeXml(notification.text || "")}</text>` +
    "</binding></visual></toast>";

  const post = async () =>
    fetch(channelUri, {
      method: "POST",
      headers: {
        "Content-Type": "text/xml",
        Authorization: "Bearer " + (await getAccessToken()),
        "X-WNS-Type": "wns/toast",
      },
      body: xml,
    });

  let response = await post();
  if (response.status === 401) {
    // Cached token was rejected (expired early or revoked): fetch a fresh one and retry once.
    cachedToken = null;
    response = await post();
  }

  if (!response.ok) {
    const body = await response.text().catch(() => "");
    const h = (name) => response.headers.get(name) || "-";
    const hint = {
      401: "token rejected",
      403: "token's app does not match the app that created this channel",
      404: "channel not valid",
      410: "channel expired",
    }[response.status] || "";
    throw new Error(
      `WNS rejected the push (HTTP ${response.status}${hint ? ": " + hint : ""}). ` +
      `error-description=${h("x-wns-error-description")} debug-trace=${h("x-wns-debug-trace")} ` +
      `msg-id=${h("x-wns-msg-id")} body=${body || "-"} using SID ${maskedSid}`
    );
  }
}

module.exports = { sendToast, isConfigured: () => Boolean(PACKAGE_SID && CLIENT_SECRET) };
