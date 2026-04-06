export const THEME_STORAGE_KEY = "halbot-theme";
export const GARMIN_CONNECT_LOGO_PATH = "/icons/garmin-connect-logo.png";
export const LONG_MAX = 9223372036854775807n;

export const LOG_SEVERITY_LEVEL = Object.freeze({
  0: "Info",
  1: "Warning",
  2: "Error"
});

export function getTodayDateInput(): string {
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  return now.toISOString().slice(0, 10);
}

export function formatDate(value: string | Date): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  return new Intl.DateTimeFormat("en-US", {
    weekday: "long",
    month: "long",
    day: "numeric",
    year: "numeric"
  }).format(date);
}

export function formatDateCompact(value: string | Date): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "2-digit"
  }).format(date);
}

export function formatDateTime(value: string | Date): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  const hour = String(date.getHours()).padStart(2, "0");
  const minute = String(date.getMinutes()).padStart(2, "0");
  return `${year}-${month}-${day} ${hour}:${minute}`;
}

export function formatLogSeverity(value: number | string): string {
  if (typeof value === "number") {
    return LOG_SEVERITY_LEVEL[value as keyof typeof LOG_SEVERITY_LEVEL] ?? `Unknown (${value})`;
  }

  if (typeof value === "string") {
    const trimmed = value.trim();
    return trimmed || "Unknown";
  }

  return "Unknown";
}

export function formatDistance(value: number): string {
  if (typeof value !== "number") {
    return "";
  }

  const kilometers = value / 1000;
  return kilometers.toFixed(2).replace(".", ",");
}

export function formatClimb(value: number): string {
  if (typeof value !== "number" || value <= 0) {
    return "-";
  }

  return `${Math.round(value)}`;
}

export function formatPace(value: string): string {
  const raw = String(value ?? "").trim();
  if (!raw || raw === "-") {
    return "-";
  }

  return `${raw} m/km`;
}

export function formatDuration(value: number): string {
  if (typeof value !== "number" || value <= 0) {
    return "-";
  }

  const totalSeconds = Math.round(value);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  }

  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

export function formatDurationInput(value: number): string {
  if (typeof value !== "number" || value <= 0) {
    return "0:00";
  }

  const totalSeconds = Math.round(value);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  }

  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

export function parseDurationInputToSeconds(value: string): number | null {
  const raw = String(value ?? "").trim();
  if (!raw) {
    return null;
  }

  const parts = raw.split(":");
  if (parts.length !== 2 && parts.length !== 3) {
    return null;
  }

  const numericParts = parts.map(part => Number.parseInt(part, 10));
  if (numericParts.some(part => Number.isNaN(part) || part < 0)) {
    return null;
  }

  let hours = 0;
  let minutes = 0;
  let seconds = 0;

  if (numericParts.length === 2) {
    [minutes, seconds] = numericParts;
  } else {
    [hours, minutes, seconds] = numericParts;
  }

  if (minutes > 59 || seconds > 59) {
    return null;
  }

  return (hours * 3600) + (minutes * 60) + seconds;
}

export function normalizeNumberInput(value: unknown): number {
  return Number.parseFloat(String(value ?? "").trim().replace(",", "."));
}

export function formatPaceInput(value: string): string {
  const raw = String(value ?? "").trim();
  if (!raw || raw === "-") {
    return "";
  }

  return raw.replace(/\s*m\/km$/i, "").trim();
}

export function formatActivityType(value: number | string): string {
  if (typeof value === "number") {
    if (value === 0) return "Classic";
    if (value === 1) return "TomTom";
    if (value === 2) return "Garmin";
    return "Unknown";
  }

  if (typeof value === "string") {
    const normalized = value.trim().toLowerCase();
    if (normalized === "classic") return "Classic";
    if (normalized === "tomtom") return "TomTom";
    if (normalized === "garmin") return "Garmin";
  }

  return "Unknown";
}

export function isGarminActivity(value: number | string): boolean {
  return formatActivityType(value) === "Garmin";
}

export function getRunBand(distanceMeters: number): string {
  const km = (distanceMeters ?? 0) / 1000;
  if (km < 5)  return "run-xs";
  if (km < 10) return "run-s";
  if (km < 20) return "run-m";
  if (km < 35) return "run-l";
  if (km < 60) return "run-xl";
  return "run-xxl";
}

export function getRunningIdWarning(value: string): string {
  const raw = String(value).trim();

  if (!raw) {
    return "";
  }

  if (!/^\d+$/.test(raw)) {
    return "Running ID must be numeric only.";
  }

  try {
    const numeric = BigInt(raw);
    if (numeric < 1n || numeric > LONG_MAX) {
      return "Running ID must be between 1 and 9223372036854775807.";
    }
  } catch {
    return "Running ID is invalid.";
  }

  return "";
}

export function toDateInput(value: unknown): string {
  const raw = String(value ?? "");
  const matched = raw.match(/^\d{4}-\d{2}-\d{2}/);
  if (matched) {
    return matched[0];
  }

  const date = new Date(value as string | Date);
  if (Number.isNaN(date.getTime())) {
    return getTodayDateInput();
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function readStoredTheme(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  const storedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);
  if (storedTheme === "light" || storedTheme === "dark") {
    return storedTheme;
  }

  return null;
}

export function saveStoredTheme(theme: string): void {
  if (typeof window === "undefined") {
    return;
  }

  if (theme === "light" || theme === "dark") {
    window.localStorage.setItem(THEME_STORAGE_KEY, theme);
  }
}
