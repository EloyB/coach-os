export function getInitial(name?: string | null): string {
  if (!name) return "";
  const match = name.match(/[A-Za-zÀ-ÖØ-öø-ÿ]/);
  return match ? match[0].toUpperCase() : "";
}

export function getInitials(firstName?: string | null, lastName?: string | null): string {
  const first = getInitial(firstName);
  const last = getInitial(lastName);
  return `${first}${last}` || "?";
}
