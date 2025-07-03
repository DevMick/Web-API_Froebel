# 🚀 Guide GitHub Codespaces - Institut Froebel API

## 📋 Vue d'ensemble

Ce guide vous accompagne dans le déploiement temporaire de l'API Institut Froebel via GitHub Codespaces.

## 🎯 Avantages de GitHub Codespaces

- ✅ **Environnement pré-configuré** : .NET 9.0, extensions VS Code, etc.
- ✅ **Accès depuis n'importe où** : Navigateur web, pas d'installation locale
- ✅ **Port forwarding automatique** : API accessible publiquement
- ✅ **Collaboration facilitée** : Partage d'environnement de développement
- ✅ **Gratuit pour usage personnel** : 60h/mois incluses

## 🛠️ Prérequis

1. **Compte GitHub** avec accès à Codespaces
2. **Repository GitHub** contenant ce projet
3. **Plan GitHub** : 
   - Gratuit : 60h/mois
   - Pro : 120h/mois
   - Team/Enterprise : Illimité

## 🚀 Démarrage rapide

### Étape 1 : Ouvrir le repository
1. Allez sur votre repository GitHub
2. Cliquez sur le bouton vert **"Code"**
3. Sélectionnez l'onglet **"Codespaces"**
4. Cliquez sur **"Create codespace on main"**

### Étape 2 : Attendre l'initialisation
- ⏱️ **Temps estimé** : 2-3 minutes
- 🔄 **Processus automatique** :
  - Installation de .NET 9.0
  - Restauration des dépendances
  - Configuration des ports

### Étape 3 : Lancer l'API
```bash
# Dans le terminal du Codespace
./start-api.sh
```

## 🌐 Accès à l'API

### URLs générées automatiquement
Une fois l'API lancée, vous aurez accès à :

| Service | URL | Description |
|---------|-----|-------------|
| **API HTTP** | `https://[codespace-id]-5000.preview.app.github.dev` | Endpoint principal |
| **API HTTPS** | `https://[codespace-id]-5001.preview.app.github.dev` | Endpoint sécurisé |
| **Swagger UI** | `https://[codespace-id]-8080.preview.app.github.dev/swagger` | Documentation |

### Exemple d'URLs
```
https://mick-dev-1234567890abc-5000.preview.app.github.dev
https://mick-dev-1234567890abc-8080.preview.app.github.dev/swagger
```

## 🔧 Configuration détaillée

### Fichiers de configuration

#### `.devcontainer/devcontainer.json`
```json
{
  "name": "Institut Froebel API - ASP.NET Core",
  "image": "mcr.microsoft.com/devcontainers/dotnet:9.0",
  "portsAttributes": {
    "5000": { "label": "API HTTP" },
    "5001": { "label": "API HTTPS" },
    "8080": { "label": "Swagger UI" }
  }
}
```

#### `appsettings.Codespaces.json`
Configuration optimisée pour Codespaces :
- Bind sur `0.0.0.0` pour tous les ports
- CORS configuré pour les domaines GitHub
- Logging adapté

### Extensions VS Code incluses
- `ms-dotnettools.csharp` - Support C#
- `ms-dotnettools.vscode-dotnet-runtime` - Runtime .NET
- `ms-vscode.vscode-docker` - Support Docker
- `esbenp.prettier-vscode` - Formatage de code

## 📚 Utilisation de l'API

### 1. Documentation Swagger
Accédez à la documentation interactive :
```
https://[codespace-id]-8080.preview.app.github.dev/swagger
```

### 2. Test d'authentification
```bash
# Login
curl -X POST "https://[codespace-id]-5000.preview.app.github.dev/api/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-School-Code: FROEBEL_ABJ" \
  -d '{
    "email": "admin@froebel.com",
    "password": "Admin123!"
  }'
```

### 3. Test des endpoints
```bash
# Liste des écoles
curl -X GET "https://[codespace-id]-5000.preview.app.github.dev/api/ecole" \
  -H "X-School-Code: FROEBEL_ABJ"

# Liste des élèves
curl -X GET "https://[codespace-id]-5000.preview.app.github.dev/api/enfant" \
  -H "X-School-Code: FROEBEL_ABJ" \
  -H "Authorization: Bearer [votre-token]"
```

## 🔒 Sécurité et bonnes pratiques

### ⚠️ Limitations de sécurité
- **Développement uniquement** : Ne pas utiliser en production
- **Base de données publique** : PostgreSQL accessible depuis internet
- **Clés JWT en clair** : Stockées dans appsettings.json

### 🛡️ Recommandations
1. **Temps limité** : Arrêtez le Codespace après utilisation
2. **Données sensibles** : Évitez de stocker des données réelles
3. **Monitoring** : Surveillez l'utilisation des ressources

## 🆘 Dépannage

### Problème : API ne démarre pas
```bash
# Vérifier .NET
dotnet --version

# Nettoyer et reconstruire
dotnet clean
dotnet restore
dotnet build

# Vérifier la connexion DB
cd InstitutFroebel.API
dotnet run --no-build
```

### Problème : Ports non accessibles
1. Vérifiez que l'API est lancée
2. Attendez 30 secondes après le démarrage
3. Vérifiez les URLs dans l'onglet "Ports" de VS Code

### Problème : Erreur de base de données
```bash
# Test de connexion
dotnet run --project InstitutFroebel.API --verbosity normal
```

## 📊 Monitoring et ressources

### Vérifier l'utilisation
- **Onglet "Ports"** : URLs et statut des ports
- **Terminal** : Logs de l'application
- **Explorateur** : Fichiers et structure

### Gestion des ressources
- **CPU** : Limité selon votre plan GitHub
- **RAM** : 4GB par défaut
- **Stockage** : 20GB par défaut
- **Temps** : Limite selon votre plan

## 🔄 Workflow de développement

### 1. Développement
```bash
# Modifier le code
# Tester localement dans Codespaces
# Commiter les changements
```

### 2. Test
```bash
# Lancer l'API
./start-api.sh

# Tester via Swagger UI
# Tester via curl/Postman
```

### 3. Déploiement
```bash
# Push vers GitHub
git add .
git commit -m "Update API"
git push origin main
```

## 📞 Support et ressources

### Documentation officielle
- [GitHub Codespaces](https://docs.github.com/en/codespaces)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework](https://docs.microsoft.com/en-us/ef/)

### Liens utiles
- [Port forwarding](https://docs.github.com/en/codespaces/developing-in-codespaces/forwarding-ports-in-your-codespace)
- [Customization](https://docs.github.com/en/codespaces/customizing-your-codespace)
- [Billing](https://docs.github.com/en/billing/managing-billing-for-github-codespaces/about-billing-for-github-codespaces)

### En cas de problème
1. Vérifiez les logs dans le terminal
2. Consultez la documentation Swagger
3. Redémarrez le Codespace
4. Ouvrez une issue sur GitHub

---

**🎉 Votre API est maintenant accessible publiquement via GitHub Codespaces !** 