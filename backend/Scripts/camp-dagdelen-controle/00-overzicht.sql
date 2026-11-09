-- 00-overzicht.sql: alleen SELECTs, schrijft niets. Draait op het OUDE schema (vóór AddCampDayParts).

-- 1. Kampen met aantallen en bedragen zoals ze nu zijn
SELECT c."Id", c."Name", c."IsActive", c."StartDate", c."EndDate",
       c."Price" AS hoofdprijs,
       (SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = c."Id") AS dagen,
       (SELECT COUNT(*) FROM "CampPrices" p WHERE p."CampId" = c."Id") AS tarieven,
       c."MaxParticipants",
       COUNT(e."Id") FILTER (WHERE e."Status" <> 3) AS actieve_inschrijvingen,
       COALESCE(SUM(COALESCE(p."Amount", c."Price")) FILTER (WHERE e."Status" <> 3), 0) AS som_bedragen
FROM "Camps" c
LEFT JOIN "CampEnrollments" e ON e."CampId" = c."Id"
LEFT JOIN "CampPrices" p ON p."Id" = e."CampPriceId"
GROUP BY c."Id"
ORDER BY c."StartDate" DESC;

-- 2. Tarieven per kamp, en hoeveel inschrijvingen ze gebruiken (labels verdwijnen bij de migratie)
SELECT c."Name" AS kamp, p."Label", p."Amount", COUNT(e."Id") AS inschrijvingen
FROM "CampPrices" p
JOIN "Camps" c ON c."Id" = p."CampId"
LEFT JOIN "CampEnrollments" e ON e."CampPriceId" = p."Id"
GROUP BY c."Name", p."Label", p."Amount", p."SortOrder"
ORDER BY c."Name", p."SortOrder";

-- 3. Kampen waar hoofdprijs / aantal dagen niet netjes deelbaar is op 2 decimalen
--    (nieuwe "heel kamp"-inschrijvingen wijken daar een paar cent af)
SELECT c."Id", c."Name", c."Price" AS hoofdprijs, n.dagen,
       ROUND(c."Price" / n.dagen, 2) AS nieuwe_dagprijs,
       ROUND(c."Price" / n.dagen, 2) * n.dagen AS nieuw_heel_kamp,
       c."Price" - ROUND(c."Price" / n.dagen, 2) * n.dagen AS verschil
FROM "Camps" c
CROSS JOIN LATERAL (SELECT GREATEST(COUNT(*), 1) AS dagen FROM "CampDays" d WHERE d."CampId" = c."Id") n
WHERE c."Price" - ROUND(c."Price" / n.dagen, 2) * n.dagen <> 0
ORDER BY c."Name";

-- 4. Openstaande betalingen op kampinschrijvingen
SELECT pay."Id", c."Name" AS kamp, e."ParticipantName", pay."Amount", pay."Status", pay."Method"
FROM "Payments" pay
JOIN "CampEnrollments" e ON e."Id" = pay."CampEnrollmentId"
JOIN "Camps" c ON c."Id" = e."CampId"
WHERE pay."Status" = 1
ORDER BY c."Name";

-- 5. Kampen zonder dagen of inschrijvingen op kampen zonder dagen (randgeval)
SELECT c."Id", c."Name",
       (SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = c."Id") AS dagen,
       (SELECT COUNT(*) FROM "CampEnrollments" e WHERE e."CampId" = c."Id") AS inschrijvingen
FROM "Camps" c
WHERE NOT EXISTS (SELECT 1 FROM "CampDays" d WHERE d."CampId" = c."Id");
