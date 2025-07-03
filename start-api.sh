#!/bin/bash

# Script de démarrage pour l'API Institut Froebel dans GitHub Codespaces
# Usage: ./start-api.sh

echo "🚀 Démarrage de l'API Institut Froebel..."
echo "=========================================="

# Vérifier que nous sommes dans le bon répertoire
if [ ! -f "InstitutFroebel.sln" ]; then
    echo "❌ Erreur: Ce script doit être exécuté depuis la racine du projet"
    exit 1
fi

# Vérifier que .NET est installé
if ! command -v dotnet &> /dev/null; then
    echo "❌ Erreur: .NET n'est pas installé"
    exit 1
fi

echo "✅ .NET détecté: $(dotnet --version)"

# Restaurer les dépendances
echo "📦 Restauration des dépendances..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ Erreur lors de la restauration des dépendances"
    exit 1
fi

# Compiler le projet
echo "🔨 Compilation du projet..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Erreur lors de la compilation"
    exit 1
fi

# Afficher les informations de configuration
echo ""
echo "📋 Configuration:"
echo "================="
echo "• Framework: .NET 9.0"
echo "• Base de données: PostgreSQL (Aiven Cloud)"
echo "• Ports configurés: 5000 (HTTP), 5001 (HTTPS), 8080 (Swagger)"
echo ""

# Vérifier la connexion à la base de données
echo "🔍 Test de connexion à la base de données..."
cd InstitutFroebel.API
echo "⚠️  Note: Configuration de base de données requise pour Codespaces"
echo "   Copiez appsettings.Codespaces.Example.json vers appsettings.Codespaces.json"
echo "   et configurez votre chaîne de connexion PostgreSQL"

echo ""
echo "🌐 URLs d'accès (après démarrage):"
echo "=================================="
echo "• API HTTP:     https://[codespace-id]-5000.preview.app.github.dev"
echo "• API HTTPS:    https://[codespace-id]-5001.preview.app.github.dev"
echo "• Swagger UI:   https://[codespace-id]-8080.preview.app.github.dev/swagger"
echo ""

echo "🚀 Lancement de l'API..."
echo "Appuyez sur Ctrl+C pour arrêter"
echo ""

# Lancer l'API
dotnet run --configuration Release 