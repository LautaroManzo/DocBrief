import chromium from "@sparticuz/chromium";
import puppeteer from "puppeteer-core";

export const config = { maxDuration: 60 };

// Funcion serverless de Vercel: obtiene la transcripcion de un video de YouTube.
// Existe porque YouTube bloquea las requests HTTP simples (sin motor JS) con
// "Sign in to confirm you're not a bot" — confirmado que pasa incluso con cookies
// de una cuenta autenticada y desde distintos proveedores cloud (Render y Vercel),
// asi que no es un tema de reputacion de IP sino de un desafio en JavaScript que
// solo un navegador real puede resolver. Por eso se usa un Chromium headless real
// (@sparticuz/chromium, pensado para funciones serverless) en vez de un fetch.
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
