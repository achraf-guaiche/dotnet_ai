# Frontend React — Plateforme Formations

SPA React (Vite + TypeScript + Tailwind CSS v4) qui consomme l'API ASP.NET Core (`Formations.Api`).

> ⚠️ **Aucune installation Node.js requise sur votre machine** — tout passe par Docker.

## Lancement (Docker)

Depuis la racine du dépôt :

```bash
# Production (build Vite servi par nginx) sur http://localhost:5002
docker compose up --build web

# Développement avec hot-reload Vite sur http://localhost:3000
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build web
```

## Configuration

| Variable | Rôle | Défaut |
|---|---|---|
| `VITE_API_URL` | URL de l'API appelée **par le navigateur** (figée au build) | `http://localhost:5000` |

En production (Railway…), passez l'URL publique de l'API via l'argument de build `VITE_API_URL`.

## Structure

```
src/
├── main.tsx                # Point d'entrée (Router + AuthProvider)
├── App.tsx                 # Déclaration des routes
├── index.css               # Tailwind v4 + thème + utilitaires
├── types/api.ts            # Types TS miroir des DTOs C# (enums = entiers !)
├── lib/apiClient.ts        # Client HTTP (JWT Bearer, erreurs, download PDF)
├── context/AuthContext.tsx # Auth (login/register/logout, rôles, localStorage)
├── components/             # Layout, NavBar, FormationCard, ProtectedRoute, Spinner
└── pages/                  # Home, FormationsList, FormationDetail, MesFormations,
                            # Login, Register, AvatarSession, AdminDashboard, AdminUsers
```

## Routes

| Route | Accès | Description |
|---|---|---|
| `/` | Public | Accueil (catégories + formations populaires) |
| `/formations` | Public | Liste paginée + recherche + filtre catégorie |
| `/formations/:id` | Public | Détail formation + inscription |
| `/connexion` | Public | Connexion |
| `/inscription` | Public | Création de compte |
| `/mes-formations` | Authentifié | Progression + téléchargement certificat PDF |
| `/modules/:moduleId/avatar` | Authentifié | Session chat avec le formateur IA |
| `/admin/dashboard` | Admin | Statistiques + graphiques |
| `/admin/utilisateurs` | Admin | Gestion des rôles / activation |

## Authentification

Le token JWT est stocké dans `localStorage` (`access_token`) et envoyé en header
`Authorization: Bearer <token>` par `apiClient`. Les rôles proviennent de la réponse
de login et sont conservés dans `AuthContext`.
```
