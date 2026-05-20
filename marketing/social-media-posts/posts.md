# Social Media Posts

Tracker voor alle CoachOS social posts op Instagram en LinkedIn. Open dit
bestand bij plannen, schedulen of evalueren van een post.

## Mapindeling

```
social-media-posts/
├── posts.md             ← dit bestand — tracker
├── brief.md             ← wat is CoachOS
├── voice.md             ← tone-of-voice
├── schedule.md          ← cadence + uren
├── POST-THIS-WEEK.md    ← actieve week
├── instagram/           ← IG-captions (één .md per post)
├── linkedin/            ← LI-captions (één .md per post)
├── drafts/              ← gedeelde visual assets (card SVG-bronnen)
│   └── exports/         ← gerenderde PNG's, klaar voor upload
├── animations/          ← Remotion-bronnen voor MP4 clips
│   └── out/             ← gerenderde MP4's, klaar voor upload
└── scripts/             ← automation (Buffer webhooks etc.)
```

Eén topic op beide platforms → twee caption-bestanden (`instagram/<slug>.md`
en `linkedin/<slug>.md`) + één gedeeld kaartbestand in `drafts/`.

## Hoe te gebruiken

1. **Draft** → voeg een rij toe aan **Pipeline** met slug, platform(s) en target-datum.
2. **Posted** → verplaats de rij naar **Posted** met de daadwerkelijke publish-datum/tijd.
3. **Evaluatie** → vul engagement in 24u na publicatie (likes, comments, saves).
4. Status-flow: `draft` → `scheduled` → `posted` → `evaluated`.

---

## Pipeline

Posts in voorbereiding of ingepland. Status: `draft` of `scheduled`.

| Slug | Platform | Topic | Caption | Visual | Status | Target |
|---|---|---|---|---|---|---|
| `feature-carousel` | IG | "Wat kan CoachOS?" — 5-slide carousel | [post bundle](instagram/feature-carousel/post.md) | bestanden in [`instagram/feature-carousel/`](instagram/feature-carousel/) | draft | volgende Ma/Wo/Vr · 07:30 |
| `feature-carousel` | LI | "Drie weekenden Excel" — single concat video | [post bundle](linkedin/feature-carousel/post.md) | TBD — concat video moet nog gerenderd worden | plan | volgende Ma/Wo/Vr · 08:30 |

---

## Posted

Live posts. Engagement invullen 24u na publicatie.

| Datum | Tijd | Platform | Slug | Topic | Caption | Visual | Likes | Comments | Saves | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| 2026-05-11 | 07:30 | IG | `launch` | Coach-os.be is live — pilot-call (5 testers) | [caption](instagram/launch-instagram.md) | [card](drafts/exports/launch-card.png) | _TBD_ | _TBD_ | _TBD_ | Eerste post |
