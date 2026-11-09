-- 02-vergelijking.sql: draait NA de migratie, tegen de snapshot uit 01-snapshot.sql.
-- Controles 1-9 geven alleen rijen bij een probleem. Geen rijen = in orde.

-- 1. Kampen verdwenen of erbij gekomen
SELECT 1 AS "check", s.camp_id, s."Name", 'kamp ontbreekt na migratie' AS probleem
FROM camp_snapshot.camps s LEFT JOIN "Camps" c ON c."Id" = s.camp_id WHERE c."Id" IS NULL
UNION ALL
SELECT 1, c."Id", c."Name", 'kamp bestond niet in snapshot'
FROM "Camps" c LEFT JOIN camp_snapshot.camps s ON s.camp_id = c."Id" WHERE s.camp_id IS NULL;

-- 2. Inschrijvingen verdwenen of erbij gekomen, of status gewijzigd
SELECT 2 AS "check", s.enrollment_id, 'inschrijving ontbreekt of status gewijzigd' AS probleem
FROM camp_snapshot.enrollments s
LEFT JOIN "CampEnrollments" e ON e."Id" = s.enrollment_id
WHERE e."Id" IS NULL OR e."Status" <> s."Status" OR e."CampId" <> s.camp_id OR e."CampEnrollmentGroupId" IS DISTINCT FROM s.group_id
UNION ALL
SELECT 2, e."Id", 'inschrijving bestond niet in snapshot'
FROM "CampEnrollments" e LEFT JOIN camp_snapshot.enrollments s ON s.enrollment_id = e."Id" WHERE s.enrollment_id IS NULL;

-- 3. Vastgelegd bedrag per inschrijving moet exact het oude bedrag (tarief of hoofdprijs) zijn
SELECT 3 AS "check", e."Id" AS enrollment_id, s.verwacht_bedrag, e."TotalAmount", 'TotalAmount wijkt af van oud bedrag' AS probleem
FROM "CampEnrollments" e JOIN camp_snapshot.enrollments s ON s.enrollment_id = e."Id"
WHERE e."TotalAmount" <> s.verwacht_bedrag;

-- 4. Elke inschrijving: precies één dagrij per kampdag, alleen voormiddag
SELECT 4 AS "check", e."Id" AS enrollment_id,
       (SELECT COUNT(*) FROM "CampEnrollmentDays" x WHERE x."CampEnrollmentId" = e."Id") AS dagrijen,
       (SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = e."CampId") AS kampdagen,
       'aantal dagrijen klopt niet met kampdagen' AS probleem
FROM "CampEnrollments" e
WHERE (SELECT COUNT(*) FROM "CampEnrollmentDays" x WHERE x."CampEnrollmentId" = e."Id")
   <> (SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = e."CampId")
UNION ALL
SELECT 4, x."CampEnrollmentId", NULL, NULL, 'dagrij is niet "alleen voormiddag"'
FROM "CampEnrollmentDays" x WHERE NOT x."Morning" OR x."Afternoon";

-- 5. Kampdagen: voormiddag = oude start/eind, geen namiddag, datum en trainers ongewijzigd
SELECT 5 AS "check", d."Id" AS day_id, 'uren, datum of trainers wijken af van snapshot' AS probleem
FROM "CampDays" d JOIN camp_snapshot.days s ON s.day_id = d."Id"
WHERE d."MorningStart" IS DISTINCT FROM s."StartTime" OR d."MorningEnd" IS DISTINCT FROM s."EndTime"
   OR d."AfternoonStart" IS NOT NULL OR d."AfternoonEnd" IS NOT NULL
   OR d."Date" <> s."Date"
   OR (SELECT COUNT(*) FROM "CampDayTrainers" t WHERE t."CampDayId" = d."Id") <> s.trainers
UNION ALL
SELECT 5, s.day_id, 'kampdag ontbreekt na migratie'
FROM camp_snapshot.days s LEFT JOIN "CampDays" d ON d."Id" = s.day_id WHERE d."Id" IS NULL;

-- 6. Kampinstellingen: halve dagen uit, dagprijs = halve-dagprijs, capaciteit en actief-vlag ongewijzigd
SELECT 6 AS "check", c."Id" AS camp_id, c."Name", 'instelling wijkt af' AS probleem
FROM "Camps" c JOIN camp_snapshot.camps s ON s.camp_id = c."Id"
WHERE c."AllowHalfDays" OR c."DayPrice" <> c."HalfDayPrice"
   OR c."MaxParticipants" IS DISTINCT FROM s."MaxParticipants" OR c."IsActive" <> s."IsActive";

-- 7. Betalingen ongewijzigd (bedrag, status, methode, betaaldatum)
SELECT 7 AS "check", s.payment_id, 'betaling gewijzigd of verdwenen' AS probleem
FROM camp_snapshot.payments s LEFT JOIN "Payments" p ON p."Id" = s.payment_id
WHERE p."Id" IS NULL OR p."Amount" <> s."Amount" OR p."Status" <> s."Status"
   OR p."Method" IS DISTINCT FROM s."Method" OR p."PaidAt" IS DISTINCT FROM s."PaidAt";

-- 8. Openstaande betaling moet overeenkomen met het vastgelegde bedrag
--    (solo: eigen TotalAmount; groepsleider: som van de actieve leden)
SELECT 8 AS "check", pay."Id" AS payment_id, pay."Amount" AS betaling,
       COALESCE(g.som, e."TotalAmount") AS vastgelegd, 'openstaande betaling wijkt af van TotalAmount' AS probleem
FROM "Payments" pay
JOIN "CampEnrollments" e ON e."Id" = pay."CampEnrollmentId"
LEFT JOIN LATERAL (
    SELECT SUM(m."TotalAmount") AS som FROM "CampEnrollments" m
    WHERE m."CampEnrollmentGroupId" = e."CampEnrollmentGroupId" AND m."Status" <> 3
      AND e."CampEnrollmentGroupId" IS NOT NULL) g ON true
WHERE pay."Status" = 1 AND pay."Amount" <> COALESCE(g.som, e."TotalAmount");

-- 9. Totaal per kamp: som van TotalAmount (actieve inschrijvingen) = som van de oude bedragen
SELECT 9 AS "check", s.camp_id, s."Name", oud.som AS oud, nieuw.som AS nieuw, 'som per kamp wijkt af' AS probleem
FROM camp_snapshot.camps s
CROSS JOIN LATERAL (SELECT COALESCE(SUM(verwacht_bedrag), 0) AS som FROM camp_snapshot.enrollments x WHERE x.camp_id = s.camp_id AND x."Status" <> 3) oud
CROSS JOIN LATERAL (SELECT COALESCE(SUM("TotalAmount"), 0) AS som FROM "CampEnrollments" e WHERE e."CampId" = s.camp_id AND e."Status" <> 3) nieuw
WHERE oud.som <> nieuw.som;

-- INFORMATIEF: wat kost een nieuwe "heel kamp"-inschrijving nu t.o.v. de oude hoofdprijs?
SELECT 'ROUND-VERSCHIL' AS info, s."Name", s.hoofdprijs AS oude_hoofdprijs,
       c."DayPrice" * s.dagen AS nieuw_heel_kamp,
       s.hoofdprijs - c."DayPrice" * s.dagen AS verschil
FROM camp_snapshot.camps s JOIN "Camps" c ON c."Id" = s.camp_id
WHERE s.hoofdprijs <> c."DayPrice" * s.dagen
ORDER BY s."Name";

-- INFORMATIEF: tarieflabels die verdwenen zijn (alleen in snapshot terug te vinden)
SELECT 'TARIEF-VERLOOP' AS info, c."Name", s.tarief_label, COUNT(*) AS inschrijvingen
FROM camp_snapshot.enrollments s JOIN camp_snapshot.camps c ON c.camp_id = s.camp_id
WHERE s.tarief_label IS NOT NULL
GROUP BY c."Name", s.tarief_label ORDER BY c."Name";
