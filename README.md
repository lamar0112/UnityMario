# Chaos Quest 3D

Et selvstendig 3D-plattformspill i Unity, inspirert av klassiske collect-a-thon-plattformere (à la Mario 64). Laget på egen hånd for å øve på Unity- og C#-grunnlaget fra **PG2202 Spillprogrammering** — koden er kommentert med hvilket pensum-tema (forelesning/kapittel) hver del øver på.

Spilleren styrer en karakter gjennom et 3D-nivå, samler orber, unngår/beseirer fiender med enkel AI, og navigerer bevegelige og fallende plattformer, hoppeputer og sjekkpunkter mot et mål.

## Funksjonalitet

- **Spillerkontroll:** bevegelse, sprint og hopp med `CharacterController` og fysikk-basert tyngdekraft.
- **Fiende-AI:** enkel tilstandsmaskin (Patrol → Chase → Stunned → Dead), med "seek"-styringsatferd for å jage spilleren når den oppdages.
- **Spillmekanikk:** orb-samling, poeng, sjekkpunkter, bevegelige/fallende plattformer, hoppeputer, lava, portal og mål.
- **GameManager:** singleton som holder styr på poengsum, tilstand og valgt karakter på tvers av scener.
- **Egne editor-verktøy:** små editor-scripts (`MasterSetup`, `AnimatorSetup`) for raskere oppsett under utvikling.
- Eksperimentering med AI-generert skybox (Blockade Labs SDK) for nivåbakgrunn.

## Teknologier

- Unity (URP)
- C# — egne scripts for spillerkontroll, fiende-FSM og spilltilstand
- Unity `CharacterController` og `Rigidbody`-fysikk
- Gratis CC0-assets fra Kenney (plattform-, natur- og bilpakker) for nivåbygging, se `Assets/_Project/Documentation/AssetCredits.md`

## Hva jeg lærte

- Bygge en enkel, men komplett tilstandsmaskin for fiende-AI fra bunnen av, i stedet for å bruke et ferdig rammeverk.
- Strukturere spilltilstand rundt en singleton `GameManager` som overlever scenebytte.
- Koble sammen `CharacterController`-bevegelse med input, hopp og tyngdekraft uten en ferdig spillerkontroller-pakke.
- Iterere på nivådesign med gratis assets — plattformlogikk (bevegelig, fallende), hazards og sjekkpunkter.

## Kjøre prosjektet lokalt

1. Åpne prosjektet i Unity Hub (URP-prosjekt).
2. Åpne scenen `Assets/mario.unity`.
3. Trykk Play.

## Merk

Prosjektet inneholder flere scener fra ulike stadier av læringsprosessen (bl.a. en tidligere `_Project`-mappe med et separat oppsett som ikke er ferdig bygget ut). Dette er et øvingsprosjekt, ikke en eksamensinnlevering.
