const WEBSITE_URL = (process.env.NEXT_PUBLIC_WEBSITE_URL ?? "https://coach-os.be").replace(/\/$/, "");

/** Publieke pagina's op de website waar de juridische documenten staan. */
export const TERMS_URL = `${WEBSITE_URL}/voorwaarden`;
export const DATA_PROCESSING_AGREEMENT_URL = `${WEBSITE_URL}/verwerkersovereenkomst`;
