// Types TypeScript miroir des DTOs C# de Formations.Application.
// IMPORTANT : l'API sérialise les enums en ENTIERS (pas de JsonStringEnumConverter).

// ── Enums (valeurs numériques renvoyées par l'API) ──────────────────────────
export enum NiveauFormation {
  Debutant = 0,
  Intermediaire = 1,
  Avance = 2,
  Expert = 3,
}

export enum StatutFormation {
  Brouillon = 0,
  EnRevision = 1,
  Publie = 2,
  Archive = 3,
}

export enum StatutInscription {
  EnCours = 0,
  Termine = 1,
  Abandonne = 2,
  Certifie = 3,
}

export const NIVEAU_LABEL: Record<number, string> = {
  0: 'Débutant',
  1: 'Intermédiaire',
  2: 'Avancé',
  3: 'Expert',
};

export const STATUT_INSCRIPTION_LABEL: Record<number, string> = {
  0: '📖 En cours',
  1: '✅ Terminé',
  2: '⏸️ Abandonné',
  3: '🏆 Certifié',
};

// ── Config publique (GET /api/config) ───────────────────────────────────────
export interface AppConfig {
  environment: string;
  isDevMode: boolean;
  llmProvider: string;
  llmModel?: string | null;
  llmMock: boolean;
  avatarProvider: string;
  avatarMock: boolean;
}

// ── Auth ────────────────────────────────────────────────────────────────────
export interface AuthResponse {
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
}

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  avatarUrl?: string | null;
  bio?: string | null;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

// ── Catégories ──────────────────────────────────────────────────────────────
export interface Categorie {
  id: number;
  nom: string;
  description?: string | null;
  icone?: string | null;
  nbFormations: number;
}

// ── Formations ──────────────────────────────────────────────────────────────
export interface FormateurSummary {
  id: number;
  fullName: string;
  photoUrl?: string | null;
}

export interface FormationSummary {
  id: number;
  titre: string;
  imageUrl?: string | null;
  categorieNom: string;
  niveauLabel: string;
  dureeHeures: number;
  prix: number;
  nbInscrits: number;
  statut: StatutFormation;
}

export interface Formation {
  id: number;
  titre: string;
  description?: string | null;
  categorieId: number;
  categorieNom: string;
  niveau: NiveauFormation;
  niveauLabel: string;
  dureeHeures: number;
  statut: StatutFormation;
  imageUrl?: string | null;
  objectifs?: string | null;
  prerequis?: string | null;
  prix: number;
  nbModules: number;
  nbInscrits: number;
  formateurs: FormateurSummary[];
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ── Modules ─────────────────────────────────────────────────────────────────
export interface ModuleDto {
  id: number;
  formationId: number;
  formationTitre: string;
  titre: string;
  ordre: number;
  description?: string | null;
  isPublie: boolean;
  hasCours: boolean;
  nbExercices: number;
  nbExamens: number;
}

export interface CoursDto {
  id: number;
  moduleId: number;
  contenu?: string | null;
  videoUrl?: string | null;
  pdfUrl?: string | null;
  textePourAvatar?: string | null;
  dureeEstimeeMinutes: number;
}

export interface ExerciceDto {
  id: number;
  moduleId: number;
  enonce: string;
  typeReponse: string;
  correctionAuto: boolean;
  instructions?: string | null;
  points: number;
}

export interface ExamenDto {
  id: number;
  moduleId: number;
  titre: string;
  description?: string | null;
  dureeMinutes: number;
  noteMinPassage: number;
  tentativesMax: number;
  nbQuestions: number;
  isActif: boolean;
}

// Réponse de GET /api/modules/{id}
export interface ModuleContent {
  module: ModuleDto;
  cours?: CoursDto | null;
  exercices: ExerciceDto[];
  examens: ExamenDto[];
}

// ── Inscriptions ────────────────────────────────────────────────────────────
export interface Inscription {
  id: number;
  userId: string;
  userFullName: string;
  formationId: number;
  formationTitre: string;
  formationImage?: string | null;
  statut: StatutInscription;
  progressionPourcent: number;
  modulesCompletes: number;
  totalModules: number;
  dateInscription: string;
  dateTerminee?: string | null;
  numeroCertificat?: string | null;
}

// ── Avatar IA ───────────────────────────────────────────────────────────────
export interface StartSessionResponse {
  sessionId: string;
  streamUrl?: string | null;
  embedToken?: string | null;
  isMockMode: boolean;
  dbSessionId: number;
}

export interface SendMessageResponse {
  avatarResponse: string;
  audioUrl?: string | null;
  videoStreamUrl?: string | null;
}

export interface ChatMessage {
  role: string;
  content: string;
  timestamp: string;
}

export interface SessionHistorique {
  id: number;
  moduleId: number;
  moduleTitre: string;
  dateSession: string;
  dateFin?: string | null;
  dureeSecondes: number;
  messages: ChatMessage[];
}

// ── Admin ───────────────────────────────────────────────────────────────────
export interface FormationStat {
  id: number;
  titre: string;
  nbInscrits: number;
  tauxCompletion: number;
}

export interface InscriptionRecente {
  userFullName: string;
  formationTitre: string;
  date: string;
}

export interface DashboardStats {
  totalFormations: number;
  totalApprenants: number;
  totalFormateurs: number;
  totalInscriptions: number;
  tauxCompletionMoyen: number;
  formationsPubliees: number;
  topFormations: FormationStat[];
  recentesInscriptions: InscriptionRecente[];
}

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  nbInscriptions: number;
}
