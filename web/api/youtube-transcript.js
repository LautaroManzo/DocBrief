// Funcion serverless de Vercel: obtiene la transcripcion de un video de YouTube.
// Existe porque desde la IP de datacenter de Render, YouTube bloquea las requests
// con "Sign in to confirm you're not a bot" (confirmado: pasa incluso con cookies
// de una cuenta autenticada, es un bloqueo a nivel de IP). Vercel corre en una red
// distinta, asi que el backend llama a este endpoint como alternativa.
export default async function handler(req, res) {
  const videoId = req.method === "GET" ? req.query.videoId : req.body?.videoId;

  if (!videoId || typeof videoId !== "string") {
    res.status(400).json({ error: "Falta el parametro videoId." });
    return;
  }

  try {
    const playerResponse = await fetch("https://www.youtube.com/youtubei/v1/player", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "User-Agent":
          "com.google.android.apps.youtube.vr.oculus/1.60.19 (Linux; U; Android 12L; Quest 3 Build/SQ3A.220605.009.A1) gzip",
      },
      body: JSON.stringify({
        videoId,
        contentCheckOk: true,
        context: {
          client: {
            clientName: "ANDROID_VR",
            clientVersion: "1.60.19",
            deviceMake: "Oculus",
            deviceModel: "Quest 3",
            osName: "Android",
            osVersion: "12L",
            platform: "MOBILE",
            hl: "en",
            gl: "US",
          },
        },
      }),
    }).then((r) => r.json());

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
    const captionsXml = await fetch(track.baseUrl).then((r) => r.text());

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
  } catch {
    res.status(502).json({ error: "No pudimos comunicarnos con YouTube." });
  }
}
