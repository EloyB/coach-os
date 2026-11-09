-- 01-snapshot.sql: bewaart de toestand VÓÓR de migratie in schema camp_snapshot.
-- Alleen op de gerestoreerde KOPIE draaien, daarna pas migreren.
DROP SCHEMA IF EXISTS camp_snapshot CASCADE;
CREATE SCHEMA camp_snapshot;

CREATE TABLE camp_snapshot.camps AS
SELECT c."Id" AS camp_id, c."OrganizationId", c."Name", c."IsActive", c."Price" AS hoofdprijs,
       c."MaxParticipants",
       (SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = c."Id") AS dagen
FROM "Camps" c;

CREATE TABLE camp_snapshot.days AS
SELECT d."Id" AS day_id, d."CampId" AS camp_id, d."Date", d."StartTime", d."EndTime",
       (SELECT COUNT(*) FROM "CampDayTrainers" t WHERE t."CampDayId" = d."Id") AS trainers
FROM "CampDays" d;

CREATE TABLE camp_snapshot.enrollments AS
SELECT e."Id" AS enrollment_id, e."CampId" AS camp_id, e."Status", e."CampEnrollmentGroupId" AS group_id,
       p."Label" AS tarief_label,
       COALESCE(p."Amount", c."Price", 0) AS verwacht_bedrag
FROM "CampEnrollments" e
JOIN "Camps" c ON c."Id" = e."CampId"
LEFT JOIN "CampPrices" p ON p."Id" = e."CampPriceId";

CREATE TABLE camp_snapshot.payments AS
SELECT pay."Id" AS payment_id, pay."CampEnrollmentId" AS enrollment_id, pay."Amount", pay."Status", pay."Method", pay."PaidAt"
FROM "Payments" pay
WHERE pay."CampEnrollmentId" IS NOT NULL;

CREATE TABLE camp_snapshot.prices AS
SELECT p."Id" AS price_id, p."CampId" AS camp_id, p."Label", p."Amount", p."SortOrder" FROM "CampPrices" p;

SELECT 'camps' AS tabel, COUNT(*) AS rijen FROM camp_snapshot.camps
UNION ALL SELECT 'days', COUNT(*) FROM camp_snapshot.days
UNION ALL SELECT 'enrollments', COUNT(*) FROM camp_snapshot.enrollments
UNION ALL SELECT 'payments', COUNT(*) FROM camp_snapshot.payments
UNION ALL SELECT 'prices', COUNT(*) FROM camp_snapshot.prices;
