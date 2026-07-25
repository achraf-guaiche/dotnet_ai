# BACKLOG — Plateforme de Gestion des Formations

> Méthodologie Scrum | Suivi sprint par sprint

---

## Légende

- `[ ]` À faire
- `[x]` Terminé
- `[~]` En cours

---

## EPIC 1 — Authentification & Gestion des Comptes

### Sprint 1 — Auth & Identity

**User Stories :**

- [x] **US-01** En tant qu'apprenant, je veux m'inscrire avec email/mot de passe afin d'accéder à la plateforme.
  - Critères : formulaire d'inscription, validation email unique, hashage MDP, retour JWT
- [x] **US-02** En tant qu'utilisateur, je veux me connecter afin d'obtenir un token JWT valide.
  - Critères : endpoint POST /api/auth/login, retour access_token + refresh_token
- [x] **US-03** En tant qu'utilisateur, je veux rafraîchir mon token afin de rester connecté sans re-saisir mon MDP.
  - Critères : endpoint POST /api/auth/refresh, token rotation
- [x] **US-04** En tant qu'utilisateur, je veux réinitialiser mon mot de passe par email.
  - Critères : endpoint POST /api/auth/forgot-password + POST /api/auth/reset-password
- [x] **US-05** En tant qu'admin, je veux assigner des rôles (Admin/Formateur/Apprenant) à un utilisateur.
  - Critères : endpoint PUT /api/admin/users/{id}/role, protégé Admin
- [x] **US-06** En tant qu'utilisateur, je veux consulter et modifier mon profil (photo, bio).
  - Critères : GET/PUT /api/auth/profile, upload avatar optionnel

---

## EPIC 2 — Interface d'Administration

### Sprint 7 — Dashboard & Stats

**User Stories :**

- [x] **US-07** En tant qu'admin, je veux voir un dashboard avec statistiques afin d'avoir une vue globale.
  - Critères : nb formations, nb apprenants, taux de complétion moyen, graphiques
  - Réalisé : `GET /api/admin/dashboard` + page React `/admin/dashboard` (widgets + graphique barres CSS)
- [x] **US-08** En tant qu'admin, je veux gérer les utilisateurs (activer/désactiver).
  - Critères : liste paginée, bouton activer/désactiver, changer rôle
  - Réalisé : page React `/admin/utilisateurs` (rôle via select, activer/désactiver, pagination)
- [x] **US-09** En tant qu'admin, je veux consulter les logs d'activité.
  - Critères : liste des actions horodatées, filtre par utilisateur/action
  - Réalisé : `GET /api/admin/logs` paginé, filtres `userId` **et** `action`, tri par date décroissante ;
    page React `/admin/logs` (tableau horodaté, filtre par action, pagination) + accès depuis le dashboard

---

## EPIC 3 — Gestion des Catégories

### Sprint 2 — Catégories & Formateurs

**User Stories :**

- [x] **US-10** En tant qu'admin, je veux créer/modifier/supprimer des catégories afin d'organiser les formations.
  - Critères : CRUD complet, validation nom unique, champ icône (classe CSS ou emoji)
- [x] **US-11** En tant qu'apprenant, je veux filtrer les formations par catégorie.
  - Critères : GET /api/categories, formations filtrables par categorieId

---

## EPIC 4 — Gestion des Formateurs

**User Stories :**

- [x] **US-12** En tant qu'admin, je veux créer/modifier un profil formateur lié à un compte utilisateur.
  - Critères : CRUD formateur, association userId, bio, spécialités
- [x] **US-13** En tant qu'admin, je veux associer un formateur à une ou plusieurs formations.
  - Critères : relation many-to-many FormationFormateur

---

## EPIC 5 — Gestion des Formations

### Sprint 3 — Formations & Modules

**User Stories :**

- [x] **US-14** En tant qu'admin/formateur, je veux créer/modifier/supprimer une formation.
  - Critères : titre, description, catégorie, image, niveau, durée, statut publié/brouillon
- [x] **US-15** En tant qu'apprenant, je veux voir la liste des formations publiées.
  - Critères : GET /api/formations?status=Published, paginé, filtres catégorie/niveau
- [x] **US-16** En tant qu'apprenant, je veux voir le détail d'une formation avec ses modules.
  - Critères : GET /api/formations/{id}, liste des modules ordonnés

---

## EPIC 6 — Gestion des Modules

**User Stories :**

- [x] **US-17** En tant qu'admin/formateur, je veux créer/modifier/supprimer un module dans une formation.
  - Critères : CRUD module, champ ordre (pour tri), description
- [x] **US-18** En tant qu'apprenant, je veux consulter les modalités d'un module (cours, exercices, examens).
  - Critères : GET /api/modules/{id}/content

---

## EPIC 7 — Modalités Pédagogiques

### Sprint 4 — Cours / Exercices / Examens

**User Stories :**

- [x] **US-19** En tant que formateur, je veux créer un cours avec du contenu riche (texte, vidéo, PDF).
  - Critères : CRUD Cours, champs Contenu (HTML), VideoUrl, PdfUrl
- [x] **US-20** En tant que formateur, je veux créer des exercices avec différents types de réponse.
  - Critères : CRUD Exercice, types : TexteLibre / FichierUpload / ChoixMultiple
- [x] **US-21** En tant que formateur, je veux créer un examen/quiz QCM avec score et temps limité.
  - Critères : CRUD Examen + QuestionQuiz, DureeMinutes, NoteMinPassage, TentativesMax
- [x] **US-22** En tant qu'apprenant, je veux passer un quiz et voir mon score immédiatement.
  - Critères : POST /api/examens/{id}/attempt, calcul score, stockage ResultatExamen

---

## EPIC 8 — Inscription & Progression

### Sprint 5 — Inscriptions & Certificats

**User Stories :**

- [x] **US-23** En tant qu'apprenant, je veux m'inscrire à une formation.
  - Critères : POST /api/inscriptions, vérif doublon, statut = EnCours
- [x] **US-24** En tant qu'apprenant, je veux suivre ma progression module par module.
  - Critères : PUT /api/inscriptions/{id}/progress, % calculé automatiquement
- [x] **US-25** En tant qu'apprenant, je veux télécharger mon certificat de fin de formation (PDF).
  - Critères : GET /api/inscriptions/{id}/certificate, généré en PDF avec QuestPDF

---

## EPIC 9 — Formateur IA (Avatar)

### Sprint 6 — Avatar IA

**User Stories :**

- [x] **US-26** En tant qu'apprenant, je veux voir une présentation animée par l'avatar IA pour chaque module.
  - Critères : page Session IA, vidéo streamée depuis Anam.ai / HeyGen
- [x] **US-27** En tant qu'apprenant, je veux poser des questions orales ou textuelles à l'avatar IA.
  - Critères : interface chat/vocal, envoi question → Claude API → réponse texte → avatar restitue en audio/vidéo
- [x] **US-28** En tant que système, je veux stocker l'historique des sessions IA par utilisateur/module.
  - Critères : modèle SessionAvatarIA, GET /api/avatar/sessions

---

## EPIC 10 — Migration Frontend React & Studio d'authoring

### Sprint 8 — React + Gestion de contenu + Choix du LLM

**User Stories :**

- [x] **US-29** En tant qu'équipe technique, je veux migrer le frontend de Blazor Server vers React afin de disposer d'une SPA découplée.
  - Critères : Vite + TypeScript + Tailwind, parité fonctionnelle, Blazor retiré de la solution
- [x] **US-30** En tant que formateur, je veux créer/modifier une formation depuis l'interface afin de ne plus passer par Swagger.
  - Critères : page `/gestion/formations`, formulaire complet, accès Admin + Formateur
- [x] **US-31** En tant que formateur, je veux gérer les modules et leurs modalités (cours, exercices, examens, questions).
  - Critères : page `/gestion/formations/:id/modules`, éditeurs cours/exercice/examen + QCM
- [x] **US-32** En tant qu'admin/formateur, je ne veux pas voir le bouton « S'inscrire » sur une formation.
  - Critères : bouton masqué pour les rôles Admin/Formateur, remplacé par « Gérer le contenu »
- [x] **US-33** En tant qu'équipe technique, je veux choisir le fournisseur LLM via docker-compose et le voir affiché en mode dev.
  - Critères : `LLM__Provider` = claude|openai|google|groq|llm, `GET /api/config`, badge « Mode dev » sur l'accueil

---

## EPIC 11 — Parcours apprenant & Rôles à l'inscription

### Sprint 9 — Apprentissage, Avatar temps réel, Choix du rôle

**User Stories :**

- [x] **US-34** En tant qu'apprenant, je veux consulter le cours, faire les exercices et passer le quiz d'un module.
  - Critères : page `/modules/:id/apprendre` avec onglets Cours (vidéo/PDF/HTML), Exercices
    (correction auto) et Quiz (timer, QCM, résultats détaillés avec explication)
  - Réalisé : `ModuleLearn.tsx`, `GET /api/modules/examens/{id}/questions` (sans les bonnes réponses)
- [x] **US-35** En tant qu'apprenant, je veux voir et entendre l'avatar IA pendant la session.
  - Critères : flux vidéo/audio Anam dans le navigateur, pas d'échec si aucun micro n'est branché
  - Réalisé : `@anam-ai/js-sdk` + `streamToVideoAndAudioElements`, `sessionToken` fourni par
    l'API, flux audio silencieux (`AudioContext`) passé en `userProvidedMediaStream` pour
    contourner `getUserMedia` (chat 100 % texte)
- [x] **US-36** En tant que visiteur, je veux choisir de m'inscrire comme **Apprenant** ou **Formateur**.
  - Critères :
    - le formulaire `/inscription` propose deux cartes de rôle (Apprenant par défaut)
    - le rôle est envoyé à `POST /api/auth/register` et appliqué au compte créé
    - **seuls** `Apprenant` et `Formateur` sont acceptés côté serveur — `Admin` reste attribuable
      uniquement par un administrateur via `/admin/utilisateurs`
    - une inscription « Formateur » crée aussi la fiche `Formateur` liée (rattachement aux formations)
    - après inscription, redirection vers `/gestion/formations` (formateur) ou `/formations` (apprenant)
  - Réalisé : `RegisterRequest.Role` + liste blanche `RolesAutorises`, validation FluentValidation,
    `AuthService.RegisterAsync`, `Register.tsx`, `AuthContext.register(..., role)`
  - Tests : `Register_AssignsOnlyWhitelistedRole` (4 cas, dont tentative d'escalade « Admin »)

---

## EPIC 12 — Génération de contenu par IA

### Sprint 11 — Studio de génération IA

**User Stories :**

- [x] **US-37** En tant que formateur, je veux générer le contenu d'un module à partir d'un prompt,
  afin de ne pas tout rédiger à la main.
  - Critères :
    - un prompt par module (bouton « Générer / régénérer ce module ») **et** un prompt global au
      niveau formation (« Générer tous les modules »)
    - l'IA produit le **cours** (HTML), le **script du prof IA vidéo** (`TextePourAvatar`) et un
      **quiz** (3–5 QCM avec bonne réponse + explication)
    - contenu persisté : upsert du `Cours`, examen créé si absent, questions régénérées (non cumulées)
    - mode dev : sans clé LLM, un contenu d'exemple déterministe est produit (aucun appel externe)
  - Réalisé : `ICourseGenerationService` (impl. `AvatarIAService`, dispatch LLM partagé avec le chat),
    `POST /api/modules/{id}/generate`, `POST /api/formations/{id}/generate`, panneaux React dans
    `GestionModules`. Testé (`CourseGenerationTests`, 3 cas).
- [x] **US-38** En tant qu'apprenant, je veux que le prof IA vidéo de chaque module présente le
  contenu généré et réponde dans le chat.
  - Réalisé : `AvatarIAController.StartSession` lit `module.Cours.TextePourAvatar` → l'avatar
    interactif existant (`/modules/:id/avatar`) récite le script généré, chat inclus. Aucun rendu
    de fichier vidéo (avatar temps réel Anam, déjà en place).

---

## ⚠️ Dette technique / À corriger au prochain sprint

- [x] ~~**BUG-01** `POST /api/avatar/sessions/message` renvoie 500 (Anam `POST /v1/sessions/speak` → 405).~~
  - Corrigé (Sprint 9) : `TextToAvatarSpeechAsync` utilise la bonne base URL, vérifie le statut
    explicitement et est entouré d'un try/catch — un 4xx/5xx du fournisseur ne casse plus le chat.
    La parole est déclenchée côté navigateur par le SDK.
- [x] ~~**BUG-02** Vidéo de l'avatar non affichée.~~
  - Corrigé (Sprint 9) : intégration `@anam-ai/js-sdk` dans `AvatarSession.tsx`.
- [x] ~~**BUG-03** Appels providers LLM (OpenAI / Google / Groq / Claude) non vérifiés.~~
  - Couvert par tests : `AvatarIAServiceLlmTests` valide, via un `HttpMessageHandler` simulé,
    la construction de requête (endpoint, en-têtes d'auth, corps) **et** le parsing de réponse
    pour chaque fournisseur, plus le mode mock (aucun appel réseau sans clé). Un smoke test
    « live » reste possible en fournissant une vraie clé (`LLM__ApiKey`), mais n'est plus bloquant.
- [x] ~~**DETTE-01** Tests unitaires des services métier.~~ — **35 tests** désormais.
  - Couvert : scoring d'examen (pondération par points, casse/espaces, `TentativesMax`, 404),
    progression d'inscription (%, complétion + certificat, plancher/plafond, isolation par
    utilisateur), attribution de rôle, **et les 5 fournisseurs LLM** (voir BUG-03).
- [~] **DETTE-02** Déploiement Railway — configuration prête et vérifiée (`railway.toml` :
  `Dockerfile`, `startCommand`, `healthcheckPath=/health` qui existe bien, `PORT=8080`).
  Le déploiement réel sur le compte Railway reste à exécuter (nécessite les identifiants).
- [x] ~~**DETTE-03** L'attribution du rôle `Formateur` par un admin ne crée pas la fiche `Formateur`.~~
  - Corrigé : `AdminController.ChangeRole` crée la fiche `Formateur` (si absente) lors d'une
    promotion en Formateur, comme à l'inscription (US-36). Testé (`ChangeRoleTests`, 3 cas).
- [x] ~~**DETTE-04** Modération des inscriptions formateur.~~
  - Corrigé : code d'invitation optionnel `Auth:FormateurInviteCode`. S'il est configuré, le rôle
    Formateur l'exige à l'inscription (champ « code d'invitation » côté front) ; un code manquant
    ou invalide crée un compte Apprenant. Non configuré = inscription formateur ouverte
    (rétrocompatible). Testé (`AuthServiceTests`, 3 cas). Env : `.env.example` + `docker-compose.yml`.

---

## Sprint 0 — Setup Infrastructure ✅

**Tâches techniques :**

- [x] Initialiser repo Git avec branches main/develop
- [x] Créer .gitignore .NET
- [x] Créer structure solution Clean Architecture (Domain / Application / Infrastructure / Api / Web)
- [x] Configurer docker-compose (app .NET + MySQL 8)
- [x] Créer Dockerfile multi-stage pour .NET 8
- [x] Créer .env.example
- [x] Créer GitHub Actions CI (build + tests)
- [x] Créer README.md squelette
- [x] Configurer Serilog (logging structuré)
- [x] Configurer Swagger/OpenAPI
- [x] Configurer middleware d'exception centralisé

---

## Sprint Review

### Sprint 0 ✅
**Livré :** Structure complète Clean Architecture, Docker, CI/CD, README
**Fichiers créés :** formations.sln, 5 projets .csproj, docker-compose.yml, Dockerfile, .env.example, .github/workflows/ci.yml, README.md
**Comment tester :** `docker compose up --build` → app sur http://localhost:5000

### Sprint 1 ✅
**Livré :** Authentification complète (register, login, refresh, forgot/reset password, profil, rôles)
**Fichiers créés :** ApplicationUser, Identity config, JWT service, AuthController, Auth pages Blazor
**Comment tester :** POST /api/auth/register → POST /api/auth/login → bearer token dans Swagger

### Sprint 2 ✅
**Livré :** CRUD Catégories, CRUD Formateurs, admin UI Blazor
**Fichiers créés :** Categorie/Formateur entities, services, controllers, pages Blazor

### Sprint 3 ✅
**Livré :** CRUD Formations, CRUD Modules, relations, pages listing Blazor
**Fichiers créés :** Formation/Module entities, FormationFormateur, services, controllers

### Sprint 4 ✅
**Livré :** Cours (rich text/video/PDF), Exercices, Examens QCM avec score
**Fichiers créés :** Cours/Exercice/Examen/QuestionQuiz entities, attempt controller

### Sprint 5 ✅
**Livré :** Inscriptions, suivi progression, certificat PDF (QuestPDF)
**Fichiers créés :** Inscription/ResultatExamen, CertificateService, pages progression

### Sprint 6 ✅
**Livré :** Intégration Anam.ai (mock mode), dialogue via Claude API, stockage sessions
**Fichiers créés :** AvatarIAService, AvatarIAController, SessionAvatarIA, page Session Blazor

### Sprint 7 ✅
**Livré :** Dashboard admin (US-07), gestion des utilisateurs (US-08), **remise en état complète du backend**
(le projet initial ne compilait pas et n'avait aucune migration EF).

**Correctifs backend :**
- Packages : `Microsoft.AspNetCore.Identity` 2.2.0 (legacy) → `Microsoft.Extensions.Identity.Stores/Core` 8.0.11 ;
  ajout `Microsoft.EntityFrameworkCore` (Application), `Microsoft.Extensions.Http` (Infrastructure),
  `Microsoft.EntityFrameworkCore.Design` (Api)
- `CertificateService` : `.Rectangle().FillColor()` → `.Background()` (API QuestPDF)
- `AdminController` : `Average` en `decimal` (erreur de conversion double→decimal)
- `AvatarIAController.GetSessions` : matérialisation avant `JsonSerializer.Deserialize`
  (argument optionnel interdit dans un arbre d'expression EF)
- `CategoriesController` : `OrderBy` avant `Select` (LINQ non traduisible)
- `FormationsController` : `Niveau.ToString()` déplacé en mémoire (enum non traduisible en SQL)
- `JwtService` : `MapInboundClaims = false` — `sub` était remappé, le refresh token était cassé
- **Migration `InitialCreate` créée** (aucune migration n'existait → base sans tables)
- `Dockerfile` API : installation de `curl` (le HEALTHCHECK en dépendait, `web` ne démarrait jamais)
- `docker-compose` : port interne MySQL corrigé (`3306`, pas le port hôte)

**Comment tester :** `docker compose up --build` → API http://localhost:5000/swagger, front http://localhost:5002
**Tests :** 9/9 tests unitaires au vert

### Sprint 8 ✅
**Livré :** Migration React, studio d'authoring, sélection du fournisseur LLM

**Fichiers créés :** `src/formations-web-react/` (Vite + TS + Tailwind v4) — `apiClient.ts`, `AuthContext.tsx`,
`ProtectedRoute`, `NavBar`, `Layout`, `FormationCard`, pages `Home`, `FormationsList`, `FormationDetail`,
`MesFormations`, `Login`, `Register`, `AvatarSession`, `AdminDashboard`, `AdminUsers`,
`GestionFormations`, `GestionModules` ; `Dockerfile` + `Dockerfile.dev` + `nginx.conf` ; `docker-compose.dev.yml`

**Fichiers supprimés :** `src/Formations.Web/` (Blazor) et `Dockerfile.Web` — retirés de `formations.sln`

**Backend :** endpoint public `GET /api/config` ; `AvatarIAService` rendu multi-fournisseurs
(Anthropic / OpenAI / Gemini / Groq / passerelle générique) ; correction de l'endpoint Anam
(`/v1/auth/session-token` au lieu de `/v1/sessions/start` inexistant) + repli gracieux en mode simulé

**Comment tester :**
1. `docker compose up --build`
2. Admin : `admin@formations.local` / `Admin@1234!` → menu **Admin**
3. Formateur : promouvoir un compte via `/admin/utilisateurs` → menu **Gestion** → créer formation/modules/cours
4. Choix du LLM : `LLM__Provider=groq docker compose up -d api` → badge « Mode dev » sur l'accueil

**Reste à faire :** voir la section « Dette technique » (BUG-01 à BUG-03, DETTE-01/02)

### Sprint 9 ✅
**Livré :** parcours d'apprentissage complet, avatar IA en temps réel, choix du rôle à l'inscription

**Apprentissage (US-34)** — page `ModuleLearn.tsx` (`/modules/:id/apprendre`) avec onglets cours
(vidéo / PDF / HTML), exercices à correction automatique et quiz chronométré avec correction
détaillée ; `GET /api/modules/examens/{id}/questions` renvoie les questions sans les bonnes réponses ;
`AttemptExamen` renvoie la `review` complète (bonne réponse + explication).

**Avatar (US-35, BUG-01/BUG-02)** — intégration `@anam-ai/js-sdk` avec
`streamToVideoAndAudioElements` ; flux audio silencieux (`AudioContext`) passé en
`userProvidedMediaStream` pour éviter l'erreur `NotFoundError` de `getUserMedia` sans micro ;
`TextToAvatarSpeechAsync` ne fait plus planter le chat en cas d'erreur du fournisseur ;
le message d'accueil est désormais persisté dans l'historique de session.

**Inscription par rôle (US-36)**
- `RegisterRequest` : nouveau champ `Role` (défaut `Apprenant`) + `RolesAutorises` (liste blanche)
- `RegisterRequestValidator` : refuse tout rôle hors `Apprenant` / `Formateur`
- `AuthService.RegisterAsync` : double contrôle serveur (ignore silencieusement un rôle non autorisé
  et retombe sur `Apprenant`) — **impossible de s'auto-attribuer `Admin`**
- Inscription formateur → création automatique de la fiche `Formateur` (rattachement aux formations,
  comptage dans le dashboard admin)
- Journal d'activité : `Register` précise le rôle choisi
- Front : sélecteur de rôle en deux cartes sur `/inscription`, redirection vers `/gestion/formations`
  pour un formateur

**Comment tester :**
1. `docker compose up --build` → http://localhost:5002/inscription
2. Choisir **Formateur** → après création du compte, arrivée directe sur **Gestion des formations**
3. Choisir **Apprenant** → arrivée sur le catalogue, bouton « S'inscrire » disponible
4. Tentative d'escalade : `curl -X POST .../api/auth/register -d '{"role":"Admin",...}'` → compte créé
   en **Apprenant** (jamais Admin)

**Tests :** 13/13 tests unitaires au vert (dont 4 cas sur la liste blanche des rôles)

### Sprint 10 ✅
**Livré :** apurement de la dette technique — journal d'activité, couverture de tests, modération formateur

**US-09 (journal d'activité)** — filtre `action` ajouté à `GET /api/admin/logs` + page React
`/admin/logs` (tableau horodaté, filtre, pagination) accessible depuis le dashboard.

**DETTE-03** — `AdminController.ChangeRole` crée la fiche `Formateur` à la promotion (idempotent).

**DETTE-01 / BUG-03 (tests 13 → 35)** — harnais EF Core InMemory + `HttpMessageHandler` simulé :
- scoring d'examen (pondération, casse/espaces, `TentativesMax`, 404)
- progression (%, complétion → certificat, plancher/plafond, isolation par utilisateur)
- 5 fournisseurs LLM (endpoint, en-têtes d'auth, corps, parsing, mode mock)

**DETTE-04 (modération formateur)** — code d'invitation optionnel `Auth:FormateurInviteCode` :
si configuré, exigé pour le rôle Formateur ; sinon inscription ouverte (rétrocompatible).
Champ « code d'invitation » côté front, redirection selon le rôle réellement accordé.

**Reste (hors périmètre local) :** DETTE-02 déploiement réel sur Railway (identifiants requis).

**Tests :** 35/35 au vert. Build .NET + build front OK.

### Sprint 11 ✅
**Livré :** génération du contenu pédagogique par IA (US-37, US-38)

**Backend**
- `ICourseGenerationService` implémenté par `AvatarIAService` (le dispatch LLM claude/openai/
  google/groq est factorisé et partagé entre le chat et la génération).
- Le LLM renvoie un JSON structuré `{ contenu, textePourAvatar, dureeEstimeeMinutes, quiz[] }`,
  parsé de façon tolérante (blocs ```json, casse). Mode mock déterministe sans clé.
- `POST /api/modules/{id}/generate` : upsert du cours + examen + questions (remplacées).
- `POST /api/formations/{id}/generate` : itère les modules et applique le prompt global.

**Frontend** — panneaux de génération dans `GestionModules` : un global (« Générer tous les
modules ») et un par module (« Générer / régénérer »). Les éditeurs se remontent pour afficher le
contenu généré.

**Vidéo** — aucun nouveau rendu : l'avatar interactif temps réel existant présente
`Cours.TextePourAvatar` du module (chat inclus).

**Comment tester :** `docker compose up --build` → connexion formateur → une formation avec modules →
« Gérer le contenu » → saisir un prompt → « Générer ». Puis côté apprenant, ouvrir le module → 🤖 IA.

**Tests :** 38/38 au vert (dont 3 sur la génération). Build .NET + build front OK.
