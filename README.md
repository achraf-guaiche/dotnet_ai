# Plateforme de Gestion des Formations

Application web complète de gestion des formations avec formateur IA intégré (Anam.ai + Claude API).

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
│   └── Formations.Web/             # Frontend Blazor Server
├── tests/
│   └── Formations.Tests.Unit/      # Tests unitaires (xUnit + Moq + FluentAssertions)
├── docker-compose.yml
├── Dockerfile                       # API (multi-stage, .NET 8)
├── Dockerfile.Web                   # Frontend Blazor
├── railway.toml                     # Config déploiement Railway
└── .github/workflows/ci.yml        # GitHub Actions CI
```

### Choix techniques justifiés

| Décision | Choix | Justification |
|---|---|---|
| Frontend | Blazor Server | Mono-solution, partage des types C# avec le backend, SSR pour le SEO |
| Avatar IA | Anam.ai | SDK officiel, mode mock intégré, meilleure intégration web |
| LLM dialogue | Claude API (Anthropic) | Meilleure qualité de réponse pédagogique, API simple |
| ORM | EF Core + Pomelo | Support natif MySQL 8, migrations versionnées |
| Auth | Identity + JWT | Industriel, rôles granulaires, refresh tokens |
| PDF | QuestPDF | Licence Community, code-first, multi-plateforme |
| Email | MailKit | Standard SMTP, supporte OAuth2 |

---

## Fonctionnalités

### ✅ Implémentées (Sprint 0 → 6)

- **Auth** : Inscription, connexion JWT, refresh token, réinitialisation MDP, profil
- **Rôles** : Admin / Formateur / Apprenant
- **Catégories** : CRUD avec protection suppression
- **Formateurs** : Profil, spécialités, association aux formations
- **Formations** : CRUD, statuts (brouillon/publié), filtres, pagination
- **Modules** : CRUD ordonnés, cours (rich text/vidéo/PDF)
- **Exercices** : Texte libre, choix multiple, upload fichier
- **Examens QCM** : Questions, score automatique, tentatives limitées, temps limité
- **Inscriptions** : Auto-inscription, suivi progression, certificat PDF
- **Avatar IA** : Intégration Anam.ai (+ mode mock), dialogue via Claude API, historique sessions
- **Admin** : Dashboard stats, gestion utilisateurs, logs d'activité

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

7. **Lancer le frontend Blazor :**
   ```bash
   dotnet run --project src/Formations.Web
   ```
   - Web : `https://localhost:5003`

8. **(Optionnel) Lancer via Docker Compose (app + MySQL en un coup) :**
   ```bash
   docker compose up --build
   ```
   - API : `http://localhost:5000/swagger`
   - Web : `http://localhost:5002`

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

## Déploiement Railway

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

- [ ] Sprint 7 : Dashboard admin avec graphiques (Chart.js via JS Interop)
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
