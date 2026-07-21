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

- [ ] **US-07** En tant qu'admin, je veux voir un dashboard avec statistiques afin d'avoir une vue globale.
  - Critères : nb formations, nb apprenants, taux de complétion moyen, graphiques
- [ ] **US-08** En tant qu'admin, je veux gérer les utilisateurs (activer/désactiver).
  - Critères : liste paginée, bouton activer/désactiver, changer rôle
- [x] **US-09** En tant qu'admin, je veux consulter les logs d'activité.
  - Critères : liste des actions horodatées, filtre par utilisateur/action

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

### Sprint 7 — En cours
**À livrer :** Dashboard admin, statistiques, polish UI, tests unitaires, déploiement Railway
