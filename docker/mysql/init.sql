-- Script d'initialisation MySQL pour Formations Platform
-- Ce script s'exécute à la première création du container
-- Les tables sont créées par EF Core Migrations au démarrage de l'app

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- Vérifier/créer la base de données
CREATE DATABASE IF NOT EXISTS formations_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- S'assurer que l'utilisateur a tous les droits
GRANT ALL PRIVILEGES ON formations_db.* TO 'formations_user'@'%';
FLUSH PRIVILEGES;
