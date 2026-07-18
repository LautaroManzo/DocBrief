import chromium from "@sparticuz/chromium";
import puppeteer from "puppeteer-core";

export const config = { maxDuration: 60 };

// Funcion serverless de Vercel: obtiene la transcripcion de un video de YouTube.
// Existe porque YouTube bloquea las requests HTTP simples (sin motor JS) con
// "Sign in to confirm you're not a bot". Ni cookies de una cuenta autenticada
// (sin JS) ni un Chromium real sin sesion (sin cookies) alcanzan por separado —
// asi que se combinan las dos cosas: Chromium real, cargando la pagina ya
// logueado con la cookie de una cuenta de YouTube (variable de entorno
// YOUTUBE_COOKIES_FILE, formato Netscape).
export default async function handler(req, res) {
  const videoId = req.method === "GET" ? req.query.videoId : req.body?.videoId;

  if (!videoId || typeof videoId !== "string") {
    res.status(400).json({ error: "Falta el parametro videoId." });
    return;
  }

  let browser;

  try {
    browser = await puppeteer.launch({
      args: chromium.args,
      executablePath: await chromium.executablePath(),
      headless: true,
    });

    const page = await browser.newPage();
    await page.setUserAgent(
      "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    );

    const cookiesFile = process.env.YOUTUBE_COOKIES_FILE;
    if (cookiesFile) {
      const cookies = parseNetscapeCookies(cookiesFile);
      if (cookies.length > 0)
        await page.setCookie(...cookies);
    }

    await page.goto(`https://www.youtube.com/watch?v=${videoId}`, {
      waitUntil: "domcontentloaded",
      timeout: 30000,
    });

    const playerResponse = await page.evaluate(() => window.ytInitialPlayerResponse ?? null);

    const status = playerResponse?.playabilityStatus?.status;
    if (status !== "OK") {
      res.status(422).json({
        error: playerResponse?.playabilityStatus?.reason ?? `Video no disponible (status=${status}).`,
      });
      return;
    }

    const tracks = playerResponse?.captions?.playerCaptionsTracklistRenderer?.captionTracks;
    if (!tracks || tracks.length === 0) {
      res.status(422).json({ error: "Ese video no tiene subtitulos disponibles." });
      return;
    }

    const track = tracks.find((t) => t.languageCode?.startsWith("en")) ?? tracks[0];

    // Se pide dentro del contexto de la pagina para que viaje con la sesion/cookies
    // que genero el propio navegador al cargar youtube.com, no un fetch aislado.
    const captionsXml = await page.evaluate(async (url) => {
      const response = await fetch(url);
      return response.text();
    }, track.baseUrl);

    const text = captionsXml
      .replace(/<[^>]+>/g, " ")
      .replace(/&#39;/g, "'")
      .replace(/&quot;/g, '"')
      .replace(/&amp;/g, "&")
      .replace(/\s+/g, " ")
      .trim();

    if (!text) {
      res.status(422).json({ error: "No pudimos extraer texto de la transcripcion." });
      return;
    }

    res.status(200).json({ text });
  } catch (err) {
    res.status(502).json({ error: "No pudimos comunicarnos con YouTube.", detail: String(err?.message ?? err) });
  } finally {
    await browser?.close();
  }
}

function parseNetscapeCookies(content) {
  const cookies = [];

  for (const rawLine of content.split("\n")) {
    let line = rawLine.replace(/\r$/, "");
    if (line.startsWith("#HttpOnly_")) line = line.slice("#HttpOnly_".length);
    if (!line.trim() || line.startsWith("#")) continue;

    const fields = line.split("\t");
    if (fields.length !== 7) continue;

    const [domain, , path, secure, expiry, name, value] = fields;
    if (!name) continue;

    cookies.push({
      name,
      value,
      domain,
      path,
      secure: secure === "TRUE",
      expires: Number(expiry) || undefined,
    });
  }

  return cookies;
}
