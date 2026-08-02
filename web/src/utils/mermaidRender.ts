const DARK_THEME_VARIABLES = {
  background: "#211f2c",
  primaryColor: "#4b4470",
  primaryTextColor: "#ecebf5",
  primaryBorderColor: "#8f83e8",
  lineColor: "#8f83e8",
  secondaryColor: "#332f47",
  tertiaryColor: "#2a2738",
  fontFamily: "Inter, sans-serif",
};

const LIGHT_THEME_VARIABLES = {
  background: "#ffffff",
  primaryColor: "#e4defa",
  primaryTextColor: "#2c2650",
  primaryBorderColor: "#6b5bd6",
  lineColor: "#6b5bd6",
  secondaryColor: "#f1eefc",
  tertiaryColor: "#f7f5fd",
  fontFamily: "Inter, sans-serif",
};

export function isDarkTheme() {
  return document.documentElement.dataset.theme === "dark";
}

// El modelo a veces deja lineas en blanco en medio del mindmap o espacios de
// mas al final de linea, algo que mermaid no tolera aunque el resto del
// diagrama sea valido. Sacarlos no cambia un diagrama ya correcto, pero
// salva a varios que fallarian solo por eso.
function sanitizeMindmapCode(code: string): string {
  return code
    .split("\n")
    .map((line) => line.trimEnd())
    .filter((line) => line.trim().length > 0)
    .join("\n");
}

export async function renderMermaidSvg(code: string, isDark: boolean): Promise<string> {
  const mermaid = (await import("mermaid")).default;
  mermaid.initialize({
    startOnLoad: false,
    theme: "base",
    themeVariables: isDark ? DARK_THEME_VARIABLES : LIGHT_THEME_VARIABLES,
    // Sin esto, ante un diagrama invalido mermaid dibuja el error (icono de
    // bomba) directo en el DOM ademas de rechazar la promesa — quedaba visible
    // aunque ConceptMapSection atrapa el error y no renderiza nada por su lado.
    suppressErrorRendering: true,
  });
  const id = `mermaid-${Math.random().toString(36).slice(2)}`;
  const { svg } = await mermaid.render(id, sanitizeMindmapCode(code));
  return svg;
}

export interface RasterizedDiagram {
  dataUrl: string;
  width: number;
  height: number;
}

// Convierte el SVG del diagrama a PNG (via canvas) para poder insertarlo en el PDF,
// que no soporta SVG nativamente. scale mas alto = mas nitidez.
export function svgToPng(svgMarkup: string, scale = 2): Promise<RasterizedDiagram> {
  return new Promise((resolve, reject) => {
    // Un data URI (en vez de blob URL) evita que el canvas quede "tainted" por
    // los <foreignObject> que usa Mermaid para el texto de los nodos.
    const dataUri = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svgMarkup)}`;
    const img = new Image();

    img.onload = () => {
      const width = img.width || img.naturalWidth;
      const height = img.height || img.naturalHeight;

      const canvas = document.createElement("canvas");
      canvas.width = width * scale;
      canvas.height = height * scale;

      const ctx = canvas.getContext("2d");
      if (!ctx) {
        reject(new Error("No se pudo crear el contexto de canvas."));
        return;
      }

      ctx.scale(scale, scale);
      ctx.drawImage(img, 0, 0, width, height);

      resolve({ dataUrl: canvas.toDataURL("image/png"), width, height });
    };

    img.onerror = () => {
      reject(new Error("No se pudo rasterizar el diagrama."));
    };

    img.src = dataUri;
  });
}
