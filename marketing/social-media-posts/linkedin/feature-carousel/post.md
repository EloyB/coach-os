# FEATURE CAROUSEL · "Wat kan CoachOS?" — LinkedIn

**Platform:** LinkedIn — CoachOS company page (Eloy repost handmatig naar persoonlijk profiel)
**Time:** Volgende Maandag / Woensdag / Vrijdag · 08:30 CET
**Status:** PLAN — assets nog te bundelen (zie [Format](#format) hieronder)

---

## Format

LinkedIn doet géén gemengde media-carousels (video + image in één post werkt
slecht). Drie reële paden, met afnemende nativiteit:

| Optie                         | Wat                                                                                       | Effort                                                             | Effect                                                                                          | Aanbevolen?                       |
| ----------------------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------- | --------------------------------- |
| **A — Single concat video**   | 4 MP4's + 2s intro + 2s outro samen één ~43s clip                                         | Hoge — vereist nieuwe Remotion-comp óf handmatige concat in iMovie | Hoog — single video autoplayt in feed, alle motion blijft behouden, LinkedIn-vriendelijk format | ✅ Primary                        |
| **B — PDF document carousel** | 5 statische slides (hook + 4 keyframes) als één PDF, geüpload via LinkedIn "Add document" | Middel — keyframes exporteren + PDF samenstellen                   | Middel — performant in LinkedIn-algoritme, swipebaar, maar motion verloren                      | Backup als concat te veel werk is |
| **C — Multi-video post**      | 4 MP4's als losse slides (LinkedIn ondersteunt multi-video sinds 2024)                    | Laag — bestanden zijn er al                                        | Onzeker — feature is nieuwer, niet alle accounts zien de carousel-UI correct                    | Alleen als A/B niet lukken        |

**Aanbeveling:** ga voor **Optie A — single concat video**. Het hergebruikt de
bestaande MP4's, behoudt alle animatie, en past in LinkedIn's sterkste
nativeformaat (single video met long-form caption).

### Wat moet er nog gebouwd worden voor Optie A

Een nieuwe Remotion-composition `FeatureCarouselFull` die in volgorde
afspeelt:

- 0–2.5s — intro card "Wat kan CoachOS?" (statisch, zelfde stijl als
  `1-hook.png` maar zonder `01/05` counter — er zijn geen slides meer)
- 2.5–11.5s — `CreateLesreeks` (9s)
- 11.5–20.5s — `EnrollmentForm` (9s)
- 20.5–30.5s — `StudentEnrollment` (10s)
- 30.5–40.5s — `AutoPlanner` (10s)
- 40.5–43s — outro card "5 pilot-plekken · coach-os.be"

Totaal ≈ 43 sec — past binnen LinkedIn's video limits, lang genoeg om
serieus te zijn, kort genoeg voor feed-scrollers.

Output: `animations/out/feature-carousel-full.mp4`, gekopieerd hierheen als
`feature-carousel-full.mp4`.

> **Vraag aan Eloy:** mag ik die nieuwe composition bouwen + renderen?
> Het is grotendeels glue (de scenes bestaan al) — schatting 15–20 min werk
> aan compositie, plus ~3 min render.

---

## Caption (LinkedIn-fit, long-form)

Drie regels langer dan de IG-versie. Eerste twee zinnen zijn de hook (vóór
de "...see more" knop op mobile).

```
Drie weekenden Excel om één lessenreeks te plannen.

80 leerlingen. 12 banen. 4 trainers. 6 niveaugroepen. En dan, op zondagavond, ontdek je dat 2 spelers allebei alleen op donderdag kunnen — maar niet samen.

Zo beginnen de meeste hoofdcoaches hun seizoen.

Wij denken dat het beter kan.

CoachOS pakt de hele flow in vier stappen:

1. Trainer maakt een lesreeks aan — naam, periode, banen, trainers. 30 seconden.
2. Trainer deelt het inschrijvingsformulier — één publieke link, geen accounts nodig.
3. Spelers schrijven zich in via een link in hun mail.
4. De auto-planner berekent het volledige weekrooster in 0,8 seconden. Conflicten? Automatisch opgelost.

In de video hieronder zie je alle vier in actie.

We bouwen dit publiek met 5 pilot-clubs. Eén maand gratis. Levenslang 25% korting nadien. Directe lijn met ons voor wat wel en niet werkt.

Past dat bij jouw club? Stuur een bericht, of kijk eerst rond op coach-os.be.
```

**Hashtags (apart, eronder of in eerste comment):**

```
#lesplanning #padelbelgie #tennisnederland #saas
```

**Eerste comment (door Eloy direct na posten, drive een impressie-boost):**

```
→ coach-os.be
→ Vragen? Stuur me een DM, dan plannen we een 15 min call.
```

---

## Waarom anders dan Instagram

| Element    | IG-versie                              | LI-versie                                                    | Reden                                                                                                               |
| ---------- | -------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| Hook       | "Wat kan CoachOS?" + slide-counter     | "Drie weekenden Excel om één lessenseizoen rond te krijgen." | LI hook = relateerbare pain, geen vraag. Beslissers identificeren zich met het probleem, niet met een feature-demo. |
| Lengte     | 7 regels                               | 17 regels                                                    | LI rewards long-form. IG breekt na 3 regels.                                                                        |
| Verhalend  | Bullet-tour van features               | Probleem → scenario → oplossing → features                   | Manager scrollt voor verhalen, niet voor demo's.                                                                    |
| Punctuatie | `→`-pijlen, emoji-gevoel               | Genummerde lijst (`1.` `2.`)                                 | Cleaner, professioneler, scant beter.                                                                               |
| URL        | "DM ons of mail" (geen link inline)    | `coach-os.be` inline klikbaar                                | LI laat klikbare links toe; gebruik ze.                                                                             |
| Hashtags   | 12 (breed bereik)                      | 4 (intentioneel)                                             | LI's algoritme straft hashtag-spam.                                                                                 |
| Format     | 5-slide carousel (PNG + 4 MP4)         | Single concat video                                          | LI mixt geen video + image in één carousel.                                                                         |
| CTA        | "DM ons of mail naar info@coach-os.be" | "Stuur een bericht, of kijk eerst rond op coach-os.be"       | Multi-path CTA: lurkers krijgen een passieve optie (site), warm leads een actieve (DM).                             |

---

## Posting checklist (LinkedIn web — desktop)

LinkedIn werkt het beste vanaf desktop voor company pages.

1. Open `linkedin.com` → CoachOS company page → **Create a post**.
2. Selecteer **Add a video** → upload `feature-carousel-full.mp4` (zodra
   die in deze map ligt).
3. Wacht tot LinkedIn de thumbnail genereert. Optioneel: upload een eigen
   cover-image (de intro-frame van de video werkt ook).
4. Plak de caption hierboven.
5. Tag eventueel mensen of pagina's die relevant zijn (bv. partner-clubs,
   maar niet nodig voor deze post).
6. **Post**.
7. **Direct daarna:** plaats de eerste comment (zie boven). LinkedIn ziet
   snelle eerste comments als engagement-signaal.
8. Eloy: repost vanaf je persoonlijke profiel binnen 1–2u voor extra reach.

---

## Quality gates voor posten

- [ ] `feature-carousel-full.mp4` opent en speelt zonder zwart frame / glitch
- [ ] Audio = stilte (Remotion-renders zijn mute). Check dat LinkedIn dit niet
      als "no sound" bericht toont — meestal niet, maar dubbelcheck.
- [ ] Eerste 2 zinnen van de caption passen zichtbaar boven de "see more"
      fade-out (test op je telefoon vóór posten — LinkedIn truncate-grens
      varieert)
- [ ] `coach-os.be` link is klikbaar in de gepubliceerde post
- [ ] Eerste comment staat klaar in je klembord vóór posten
- [ ] Personal-profile repost ingepland (timer / herinnering)

---

## Notes

- **LinkedIn engagement-window is langer dan IG.** Een goeie LI-post haalt
  reach in de eerste 24u, niet de eerste 60 min. Plan dat in: beantwoord
  comments verspreid over de dag.
- **Dwell time > likes.** LinkedIn meet hoe lang mensen op je post stoppen.
  Long-form captions + autoplay video = hoge dwell time. Vermijd te korte
  versies voor LI.
- **Repost via personal profile** kan 2–5x meer reach geven dan alleen company
  page. Maar niet binnen het eerste uur — wacht tot de company-post momentum
  heeft.
- **Geen "link in comments" trucs nodig** — LinkedIn straft die niet meer
  zoals 2 jaar geleden. Plaats de URL gewoon inline in de caption.
- **Wat NIET te doen op LinkedIn:** emoji-bullets (👉, ✅, 🚀), shouty caps,
  "↓ comment below ↓"-spam, "Agree?" filler-zinnen aan het eind. Dit
  performt slecht en doet je geloofwaardigheid kwaad bij beslissers.
- **Volgende variant:** als deze post werkt, draai een tweede LI-post met
  alleen `AutoPlanner.mp4` als focus + caption die alleen die feature
  uitdiept. LinkedIn rewards diepte > breedte voor SaaS.

---

## Bronbestanden

- IG-versie (voor referentie): `../../instagram/feature-carousel/post.md`
- Bron-MP4's: `../../animations/out/{create-lessenreeks,enrollment-form,student-enrollment,auto-planner}.mp4`
- Hook PNG (mocht je toch voor Optie B/C gaan): `../../drafts/exports/feature-carousel-slide-1-hook.png`
- Remotion-bronnen: `../../animations/src/compositions/`
