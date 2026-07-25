# Plateforme de Gestion des Formations

**Application web de formation en ligne (e-learning) avec un formateur IA vidéo interactif.**

La plateforme permet à des **formateurs** de créer des formations (modules, cours, exercices, quiz)
— y compris en **générant le contenu automatiquement par IA** à partir d'un simple prompt — et à des
**apprenants** de suivre ces formations, de dialoguer avec un **prof IA en vidéo temps réel**
(avatar Anam.ai + dialogue Claude/OpenAI/Gemini/Groq), de faire les exercices, de passer les quiz,
de suivre leur **progression** et d'obtenir un **certificat PDF**.

Stack : **ASP.NET Core 8** (API REST, Clean Architecture) · **React** (Vite + TypeScript + Tailwind) ·
**MySQL 8** (EF Core / Pomelo) · **Docker** · Auth **JWT** avec rôles.

> ▶️ **Pressé ?** Voir [Démarrage rapide (Docker)](#démarrage-rapide-docker) et
> [Rôles et permissions](#rôles-et-permissions).

[![CI](https://github.com/<votre-org>/<votre-repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<votre-org>/<votre-repo>/actions)

---

## Architecture

```
formations/
├── src/
│   ├── Formations.Domain/          # Entités métier, enums (pas de dépendances)
│   ├── Formations.Application/     # Use cases, DTOs, interfaces, validators
│   ├── Formations.Infrastructure/  # EF Core, MySQL, JWT, Email, Anam.ai, Claude API
│   ├── Formations.Api/             # Web API ASP.NET Core, controllers, middleware
│   └── formations-web-react/       # Frontend React (Vite + TypeScript + Tailwind)
├── tests/
│   └── Formations.Tests.Unit/      # Tests unitaires (xUnit + Moq + FluentAssertions)
├── docker-compose.yml
├── docker-compose.dev.yml           # Override dev : hot-reload Vite pour le frontend
├── Dockerfile                       # API (multi-stage, .NET 8)
├── railway.toml                     # Config déploiement Railway
└── .github/workflows/ci.yml        # GitHub Actions CI
```

> Le frontend a son propre Dockerfile dans `src/formations-web-react/` (build Vite → nginx).

### Choix techniques justifiés

| Décision | Choix | Justification |
|---|---|---|
| Frontend | React (Vite + TypeScript + Tailwind) | SPA découplée consommant l'API REST, écosystème riche, types TS miroir des DTOs C# |
| Avatar IA | Anam.ai | SDK officiel, mode mock intégré, meilleure intégration web |
| LLM dialogue | Claude API (Anthropic) | Meilleure qualité de réponse pédagogique, API simple |
| ORM | EF Core + Pomelo | Support natif MySQL 8, migrations versionnées |
| Auth | Identity + JWT | Industriel, rôles granulaires, refresh tokens |
| PDF | QuestPDF | Licence Community, code-first, multi-plateforme |
| Email | MailKit | Standard SMTP, supporte OAuth2 |

---

## Fonctionnalités

- **Authentification** : inscription (choix du rôle Apprenant/Formateur), connexion JWT,
  refresh token, réinitialisation de mot de passe, profil. Modération formateur optionnelle par
  code d'invitation.
- **Rôles** : Admin / Formateur / Apprenant (voir [Rôles et permissions](#rôles-et-permissions)).
- **Catégories & Formateurs** : CRUD, profils formateurs associés aux formations.
- **Formations & Modules** : CRUD, statuts (brouillon/publié), filtres, pagination, modules ordonnés.
- **Contenu pédagogique** : cours (HTML riche / vidéo / PDF), exercices (texte libre, choix multiple),
  examens QCM (score automatique, tentatives et temps limités).
- **🤖 Génération par IA** : à partir d'un prompt, le formateur génère automatiquement le cours,
  le script du prof IA vidéo, les exercices et le quiz — par module **ou** pour toute la formation.
- **🎥 Formateur IA vidéo** : avatar temps réel (Anam.ai, mode mock intégré) qui présente le module
  et répond dans un **chat** ; dialogue propulsé par le LLM configurable (Claude / OpenAI / Gemini / Groq).
- **Parcours apprenant** : inscription, suivi du cours, exercices et quiz **enregistrés**
  (consultables et recommençables), **progression** automatique (module complété = quiz réussi +
  exercices corrects), **certificat PDF** à 100 %.
- **Administration** : dashboard statistiques, gestion des utilisateurs (rôles, activation),
  journal d'activité filtrable.
- **Données de démonstration** : 3 formations complètes chargées au premier démarrage.

---

## Démarrage rapide (Docker)

Le seul prérequis est **Docker** (avec Docker Compose). Aucun autre outil à installer.

```bash
# 1. Cloner le dépôt
git clone <url-du-repo> && cd dotnet_ai

# 2. Créer le fichier d'environnement (les valeurs par défaut suffisent pour un essai local)
cp .env.example .env

# 3. Construire et lancer toute la stack (API + MySQL + frontend React)
docker compose up --build
```

Une fois les conteneurs démarrés :

| Service | URL |
|---|---|
| **Application (frontend React)** | http://localhost:5002 |
| **API + Swagger** | http://localhost:5000/swagger |
| **Santé de l'API** | http://localhost:5000/health |
| Base MySQL (hôte) | `localhost:33006` |

**Compte administrateur** créé automatiquement : `admin@formations.local` / `Admin@1234!`
(les **3 formations de démonstration** sont chargées au premier démarrage sur base vide).

> Développement avec rechargement à chaud du frontend (Vite) :
> `docker compose -f docker-compose.yml -f docker-compose.dev.yml up`
>
> Recharger les données de démo (⚠️ réinitialise la base) : `docker compose down -v && docker compose up --build`

Sans clé d'API configurée, l'app tourne en **mode démo** : le prof IA et la génération de contenu
renvoient des réponses simulées (aucun appel externe). Voir [Variables d'environnement](#variables-denvironnement)
pour brancher les vraies clés (Anam.ai, Claude/OpenAI/Gemini/Groq).

---

## Rôles et permissions

L'application distingue trois rôles. Le rôle est choisi à l'inscription (Apprenant ou Formateur) ;
le rôle **Admin** n'est attribué que par un administrateur.

### 👨‍🎓 Apprenant (par défaut à l'inscription)
- Parcourir le catalogue des formations publiées (filtres par catégorie, niveau, recherche).
- S'inscrire à une formation et accéder à son contenu.
- Suivre les **cours** (texte, vidéo, PDF).
- Faire les **exercices** et passer les **quiz** : les tentatives sont **enregistrées**,
  consultables et **recommençables**.
- Dialoguer avec le **formateur IA vidéo** (chat) sur chaque module.
- Suivre sa **progression** (mise à jour automatiquement) et télécharger son **certificat PDF** à 100 %.
- Gérer son profil.

### 👨‍🏫 Formateur
- Tout ce qu'un utilisateur connecté peut faire, **plus la création de contenu** :
- Créer / modifier / **supprimer** des formations, modules, cours, exercices, examens et questions.
- **Générer le contenu par IA** à partir d'un prompt (cours + script du prof IA + exercices + quiz),
  par module ou pour toute la formation.
- **Prévisualiser** un module comme un apprenant (bouton « 👁️ Aperçu »).
- Être associé à des formations. *(Un formateur ne s'inscrit pas aux formations : il les gère.)*

### 🛠️ Administrateur
- Tout ce qu'un formateur peut faire, **plus la gestion de la plateforme** :
- **Dashboard** : statistiques globales (formations, apprenants, formateurs, taux de complétion).
- **Gestion des utilisateurs** : changer les rôles (y compris promouvoir Formateur/Admin), activer/désactiver un compte.
- **Journal d'activité** : consulter les actions horodatées (filtrable par utilisateur/action).
- Gérer les catégories et l'ensemble du contenu.

Compte admin par défaut : `admin@formations.local` / `Admin@1234!`.

---

## Installation locale

### Prérequis communs
- .NET SDK 8.0
- MySQL Server 8.x
- Git
- (Optionnel) Docker + Docker Compose

### 🐧 Sous Ubuntu

1. **Installer .NET SDK 8.0 :**
   ```bash
   wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt update
   sudo apt install -y dotnet-sdk-8.0
   dotnet --version
   ```

2. **Installer MySQL Server :**
   ```bash
   sudo apt update
   sudo apt install -y mysql-server
   sudo systemctl enable --now mysql
   sudo mysql_secure_installation
   ```
   Créer la base et l'utilisateur :
   ```bash
   sudo mysql -u root -p
   ```
   ```sql
   CREATE DATABASE formations_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   CREATE USER 'formations_user'@'localhost' IDENTIFIED BY 'UserPasswordFort!';
   GRANT ALL PRIVILEGES ON formations_db.* TO 'formations_user'@'localhost';
   FLUSH PRIVILEGES;
   EXIT;
   ```

3. **Cloner le projet :**
   ```bash
   git clone https://github.com/<votre-org>/<votre-repo>.git
   cd <votre-repo>
   ```

4. **Configurer les variables d'environnement :**
   ```bash
   cp .env.example .env
   # Éditer .env avec vos vraies valeurs
   nano .env
   ```
   Ou avec `dotnet user-secrets` (recommandé) :
   ```bash
   cd src/Formations.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Default" \
     "server=localhost;database=formations_db;user=formations_user;password=UserPasswordFort!;AllowPublicKeyRetrieval=true;SslMode=none"
   dotnet user-secrets set "Jwt:Key" "VotreCleJWTMin32CaracteresSecrete!!!"
   dotnet user-secrets set "AvatarIA:ApiKey" "VOTRE_CLE_ANAM_AI"  # Optionnel, mock sinon
   dotnet user-secrets set "LLM:ApiKey" "VOTRE_CLE_ANTHROPIC"      # Optionnel, mock sinon
   cd ../..
   ```

5. **Installer l'outil EF Core et appliquer les migrations :**
   ```bash
   dotnet tool install --global dotnet-ef
   export PATH="$PATH:$HOME/.dotnet/tools"
   dotnet ef database update \
     --project src/Formations.Infrastructure \
     --startup-project src/Formations.Api
   ```
   > **Note :** L'application applique aussi les migrations automatiquement au démarrage en mode Development.

6. **Lancer l'API :**
   ```bash
   dotnet restore
   dotnet run --project src/Formations.Api
   ```
   - API : `https://localhost:5001`
   - Swagger : `https://localhost:5001/swagger`
   - Health : `https://localhost:5001/health`

7. **Lancer le frontend React (via Docker — rien à installer sur la machine) :**

   Le frontend est une SPA React. Il n'est **pas** nécessaire d'installer Node.js
   sur votre machine : tout passe par Docker.

   ```bash
   # Build de production (nginx) — fait partie de docker compose up (voir étape 8)
   docker compose up --build web

   # OU en mode développement avec hot-reload Vite :
   docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build web
   ```
   - Frontend (prod nginx) : `http://localhost:5002`
   - Frontend (dev Vite)   : `http://localhost:3000`

8. **(Recommandé) Lancer toute la stack via Docker Compose (API + MySQL + Frontend) :**
   ```bash
   docker compose up --build
   ```
   - API : `http://localhost:5000/swagger`
   - Frontend React : `http://localhost:5002`

   > Le navigateur appelle l'API sur `http://localhost:5000`. Cette URL est figée au
   > build du frontend via l'argument `VITE_API_URL` (voir `docker-compose.yml`).

---

### 🪟 Sous Windows

1. **Installer .NET SDK 8.0 :**
   ```powershell
   winget install Microsoft.DotNet.SDK.8
   ```
   Ou télécharger depuis https://dotnet.microsoft.com/download/dotnet/8.0
   Vérifier : `dotnet --version`

2. **Installer MySQL Server :**
   ```powershell
   winget install Oracle.MySQL
   ```
   Ou MySQL Installer depuis https://dev.mysql.com/downloads/installer/

   Créer la base (MySQL Workbench ou PowerShell) :
   ```sql
   CREATE DATABASE formations_db CHARACTER SET utf8mb4;
   CREATE USER 'formations_user'@'localhost' IDENTIFIED BY 'UserPasswordFort!';
   GRANT ALL PRIVILEGES ON formations_db.* TO 'formations_user'@'localhost';
   FLUSH PRIVILEGES;
   ```

3. **Installer Git :**
   ```powershell
   winget install Git.Git
   ```

4. **Cloner le projet :**
   ```powershell
   git clone https://github.com/<votre-org>/<votre-repo>.git
   cd <votre-repo>
   ```

5. **Configurer les secrets locaux :**
   ```powershell
   copy .env.example .env
   # Éditer .env avec Notepad ou VS Code
   ```
   Ou via `dotnet user-secrets` (depuis PowerShell ou Terminal VS Code) — mêmes commandes que Ubuntu.

6. **Installer l'outil EF Core et migrer :**
   ```powershell
   dotnet tool install --global dotnet-ef
   dotnet ef database update `
     --project src\Formations.Infrastructure `
     --startup-project src\Formations.Api
   ```

7. **Lancer l'application :**
   ```powershell
   dotnet restore
   dotnet run --project src\Formations.Api
   ```
   Ou ouvrir `formations.sln` dans **Visual Studio 2022** et lancer avec `F5`.

8. **(Optionnel) Docker Desktop :**
   ```powershell
   docker compose up --build
   ```

---

### Synchroniser le travail entre Ubuntu et Windows

Puisque le développement peut se faire sur deux machines, utilisez GitHub comme source unique de vérité :

```bash
git pull origin develop      # avant de commencer à travailler
# ... travail ...
git add .
git commit -m "feat: description claire"
git push origin develop      # avant de changer de machine
```

> **Important :** Commitez toujours `.env.example` (jamais `.env` réel) et vos migrations EF Core pour que l'autre machine puisse recréer l'environnement identique.

---

## Compte administrateur par défaut

Au premier démarrage, la base est initialisée avec :

| Email | Mot de passe |
|---|---|
| `admin@formations.local` | `Admin@1234!` |

**Changez ce mot de passe immédiatement en production !**

---

## Variables d'environnement

Voir `.env.example` pour la liste complète. Variables critiques :

| Variable | Description | Requis |
|---|---|---|
| `ConnectionStrings__Default` | Chaîne de connexion MySQL | ✅ |
| `Jwt__Key` | Clé secrète JWT (min 32 chars) | ✅ |
| `AvatarIA__Provider` | `mock` / `anam` / `heygen` | ✅ (mock par défaut) |
| `AvatarIA__ApiKey` | Clé API Anam.ai ou HeyGen | En production |
| `LLM__ApiKey` | Clé API Anthropic (Claude) | En production |
| `Email__Username` | Compte SMTP | En production |
| `Email__Password` | Mot de passe application SMTP | En production |

---

## Intégration Avatar IA

### Mode Mock (développement)
Le mode mock est activé automatiquement si `AvatarIA__Provider=mock` ou si la clé API est vide.
Il simule les sessions et les réponses IA sans appel externe.

### Brancher Anam.ai (production)
1. Créer un compte sur https://anam.ai
2. Obtenir votre `API_KEY` et `PERSONA_ID` depuis le dashboard
3. Configurer dans `.env` :
   ```
   AvatarIA__Provider=anam
   AvatarIA__ApiKey=votre_cle_anam
   AvatarIA__PersonaId=votre_persona_id
   ```

### Brancher le LLM Claude (dialogue IA)
1. Créer un compte sur https://anthropic.com
2. Obtenir une clé API
3. Configurer : `LLM__ApiKey=votre_cle_anthropic`

---

## Déploiement sur un serveur

Deux options selon l'infrastructure. Dans les deux cas, **utilisez des secrets de production**
(clé JWT ≥ 32 caractères, mots de passe MySQL forts) et **changez le mot de passe admin par défaut**.

### Option A — Serveur / VPS avec Docker Compose (recommandé, autonome)

Sur n'importe quel serveur Linux disposant de Docker :

```bash
# 1. Récupérer le code
git clone <url-du-repo> && cd dotnet_ai

# 2. Renseigner les secrets de production dans .env
cp .env.example .env
nano .env   # Jwt__Key, MYSQL_PASSWORD, clés API (AvatarIA__ApiKey, LLM__ApiKey), SMTP…

# 3. Lancer en arrière-plan
docker compose up --build -d

# 4. Vérifier
docker compose ps
curl http://localhost:5000/health
```

- Les **migrations EF s'appliquent automatiquement au démarrage** et les données de démo sont
  insérées sur base vide.
- Placez un **reverse proxy HTTPS** (Nginx, Caddy, Traefik) devant les ports `5002` (frontend) et
  `5000` (API), et pointez un nom de domaine dessus.
- Pour reconstruire le frontend avec l'URL publique de l'API :
  `VITE_API_URL=https://api.mondomaine.com docker compose up --build -d web`
  (l'URL de l'API est figée au build du frontend).

### Option B — Railway (PaaS managé)

### Prérequis
- Compte Railway (https://railway.app)
- CLI Railway : `npm install -g @railway/cli`

### Étapes

1. **Créer le projet Railway :**
   ```bash
   railway login
   railway init
   ```

2. **Ajouter le plugin MySQL :**
   - Dashboard Railway → New → Database → MySQL

3. **Configurer les variables d'environnement Railway :**
   ```bash
   railway variables set Jwt__Key="VOTRE_CLE_SECRETE_PRODUCTION"
   railway variables set AvatarIA__ApiKey="votre_cle_anam"
   railway variables set LLM__ApiKey="votre_cle_anthropic"
   # ConnectionStrings__Default est automatiquement injectée par le plugin MySQL Railway
   ```

4. **Déployer :**
   ```bash
   railway up
   ```

5. **Appliquer les migrations (une seule fois) :**
   ```bash
   railway run dotnet ef database update \
     --project src/Formations.Infrastructure \
     --startup-project src/Formations.Api
   ```
   > **Note :** L'app applique aussi les migrations au démarrage automatiquement.

### URL de déploiement
Railway fournit automatiquement une URL HTTPS : `https://formations-api-xxxx.railway.app`

---

## API Reference

La documentation Swagger est disponible en mode développement :
- `https://localhost:5001/swagger` (local)
- `https://votre-app.railway.app/swagger` (prod, désactiver en production)

### Endpoints principaux

| Méthode | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Inscription apprenant |
| POST | `/api/auth/login` | Public | Connexion → JWT |
| POST | `/api/auth/refresh` | Public | Rafraîchir token |
| GET | `/api/formations` | Public | Liste formations paginée |
| GET | `/api/formations/{id}` | Public | Détail formation |
| POST | `/api/inscriptions` | Apprenant | S'inscrire |
| PUT | `/api/inscriptions/{id}/progress` | Apprenant | Mettre à jour progression |
| GET | `/api/inscriptions/{id}/certificate` | Apprenant | Télécharger certificat PDF |
| POST | `/api/modules/examens/{id}/attempt` | Apprenant | Passer un quiz |
| POST | `/api/avatar/sessions/start` | Apprenant | Démarrer session IA |
| POST | `/api/avatar/sessions/message` | Apprenant | Envoyer message à l'IA |
| GET | `/api/admin/dashboard` | Admin | Statistiques |

---

## Tests

```bash
# Tous les tests
dotnet test

# Tests unitaires uniquement
dotnet test tests/Formations.Tests.Unit/

# Avec couverture de code
dotnet test --collect:"XPlat Code Coverage"
```

---

## Roadmap / Backlog restant

- [x] Sprint 7 : Dashboard admin avec graphiques (barres CSS, sans dépendance)
- [x] Migration du frontend Blazor Server → React (Vite + TypeScript + Tailwind)
- [ ] Graphiques avancés (Recharts / Chart.js) sur le dashboard React
- [ ] Upload fichiers (avatars, PDFs de cours) vers S3/Azure Blob
- [ ] Notifications en temps réel (SignalR)
- [ ] Mobile app (MAUI ou React Native)
- [ ] Tests d'intégration (WebApplicationFactory + TestContainers)
- [ ] Support multilangue (i18n)
- [ ] Mode hors-ligne PWA
- [ ] Analytics avancées (temps passé par module, heat maps)

---

## Contribuer

```
git checkout develop
git checkout -b feature/ma-fonctionnalite
# ... code ...
git commit -m "feat: description de la fonctionnalité"
git push origin feature/ma-fonctionnalite
# Créer une Pull Request vers develop
```

Convention de commits : `feat:` / `fix:` / `chore:` / `docs:` / `test:`

---

## Licence

Ce projet est sous licence MIT. Voir `LICENSE` pour plus d'informations.
