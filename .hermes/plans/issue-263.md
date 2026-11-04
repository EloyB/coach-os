# Issue #263 — waarschuwing formulierbouwer

- Basis: laatste origin/main (ab03c68), eigen worktree/branch; geen bestaande PR voor #263.
- Scope: één gedeelde, permanent zichtbare waarschuwing boven aangepaste velden in de lessenreeks- en kampformulierbouwer. Medische vragen zijn niet toegelaten, behalve allergieën/aandachtspunten voor veiligheid; club verantwoordelijk. Alle nieuwe UI-tekst via messages/nl.json.
- Geen trefwoordherkenning (optioneel), blokkering, opslag/API/backendwijzigingen of juridische documentwijzigingen.
- TDD: echte lokale Playwright-routes met onderschepte API-verzoeken: waarschuwing vóór eerste veld, ook na veld toevoegen, opslaan blijft werken. Eerst ontbrekende waarschuwing RED, dan minimale implementatie GREEN.
- Verificatie: regressies desktop/mobiel, unit runner uit package.json, build, TypeScript, gerichte ESLint, diff-check. Screenshots van werkelijke lokale UI met expliciet gemockte API-responses.
- Geen database/reset/seed, productie, secrets, SSH, infrastructuur of workflowwijzigingen. PR openen na lokale groene relevante checks; niet mergen.
