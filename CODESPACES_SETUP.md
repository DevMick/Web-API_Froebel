# 🚀 Configuration GitHub Codespaces - Institut Froebel API

## 📋 Prérequis

1. **Repository GitHub** : https://github.com/DevMick/Web-API_Froebel
2. **Base de données PostgreSQL** : Aiven Cloud ou autre fournisseur
3. **Accès GitHub Codespaces** : Plan GitHub Pro ou Enterprise

## 🔧 Configuration étape par étape

### Étape 1 : Créer un Codespace

1. Allez sur votre repository GitHub
2. Cliquez sur le bouton vert **"Code"**
3. Sélectionnez l'onglet **"Codespaces"**
4. Cliquez sur **"Create codespace on main"**

### Étape 2 : Configuration de la base de données

#### Option A : Utiliser Aiven Cloud (recommandé)
1. Créez un compte sur [Aiven Cloud](https://aiven.io/)
2. Créez une instance PostgreSQL
3. Récupérez les informations de connexion

#### Option B : Utiliser une autre base de données
- PostgreSQL hébergé (AWS RDS, Azure Database, etc.)
- Base de données locale avec port forwarding

### Étape 3 : Configuration des fichiers

Dans le terminal du Codespace :

```bash
# 1. Copier le fichier de configuration d'exemple
cp InstitutFroebel.API/appsettings.Codespaces.Example.json InstitutFroebel.API/appsettings.Codespaces.json

# 2. Éditer le fichier avec vos informations de connexion
code InstitutFroebel.API/appsettings.Codespaces.json
```

#### Configuration de la chaîne de connexion :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-postgresql-host;Port=5432;Database=your-database;Username=your-username;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

**Remplacez :**
- `your-postgresql-host` : L'hôte de votre base de données
- `your-database` : Le nom de votre base de données
- `your-username` : Votre nom d'utilisateur
- `your-password` : Votre mot de passe

### Étape 4 : Configuration JWT

Dans le même fichier, configurez votre clé JWT :

```json
{
  "JwtSettings": {
    "SecretKey": "votre-clé-jwt-très-sécurisée-et-très-longue-pour-2024",
    "Issuer": "InstitutFroebel.API",
    "Audience": "InstitutFroebel.Client",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  }
}
```

### Étape 5 : Lancement de l'API

#### Méthode 1 : Script automatisé
```bash
./start-api.sh
```

#### Méthode 2 : Commandes manuelles
```bash
# Restaurer les dépendances
dotnet restore

# Compiler le projet
dotnet build

# Lancer l'API
cd InstitutFroebel.API
dotnet run --environment Codespaces
```

#### Méthode 3 : Tâches VS Code
1. Ouvrez la palette de commandes (`Ctrl+Shift+P`)
2. Tapez "Tasks: Run Task"
3. Sélectionnez "Setup Codespaces Config"
4. Puis "Run API in Codespaces"

## 🌐 Accès à l'API

Une fois l'API lancée, vous aurez accès à :

| Service | URL | Description |
|---------|-----|-------------|
| **API HTTP** | `https://[codespace-id]-5000.preview.app.github.dev` | Endpoint principal |
| **API HTTPS** | `https://[codespace-id]-5001.preview.app.github.dev` | Endpoint sécurisé |
| **Swagger UI** | `https://[codespace-id]-8080.preview.app.github.dev/swagger` | Documentation |

## 📚 Utilisation de l'API

### 1. Test de santé
```bash
curl https://[codespace-id]-5000.preview.app.github.dev/health
```

### 2. Documentation Swagger
Ouvrez votre navigateur et allez sur :
```
https://[codespace-id]-8080.preview.app.github.dev/swagger
```

### 3. Test d'authentification
```bash
curl -X POST "https://[codespace-id]-5000.preview.app.github.dev/api/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-School-Code: FROEBEL_ABJ" \
  -d '{
    "email": "admin@froebel.com",
    "password": "Admin123!"
  }'
```

## 🔒 Sécurité

### ⚠️ Points importants
- **Développement uniquement** : Cette configuration est pour les tests
- **Base de données** : Utilisez une base de données de test
- **JWT** : Changez la clé secrète pour la production
- **CORS** : Configuré pour accepter les domaines GitHub

### 🛡️ Bonnes pratiques
1. **Ne jamais commiter** les fichiers de configuration avec des secrets
2. **Utiliser des variables d'environnement** en production
3. **Changer les mots de passe** par défaut
4. **Surveiller les logs** pour détecter les problèmes

## 🆘 Dépannage

### Problème : API ne démarre pas
```bash
# Vérifier la configuration
dotnet run --project InstitutFroebel.API --verbosity normal

# Vérifier les logs
tail -f InstitutFroebel.API/logs/app.log
```

### Problème : Erreur de base de données
```bash
# Tester la connexion
dotnet run --project InstitutFroebel.API --no-build --verbosity normal
```

### Problème : Ports non accessibles
1. Vérifiez que l'API est lancée
2. Attendez 30 secondes après le démarrage
3. Vérifiez les URLs dans l'onglet "Ports" de VS Code

## 📊 Monitoring

### Logs de l'application
```bash
# Suivre les logs en temps réel
tail -f InstitutFroebel.API/logs/app.log
```

### Métriques de performance
- **CPU** : Surveillé via l'onglet "Ports" de VS Code
- **Mémoire** : Limite selon votre plan GitHub
- **Stockage** : 20GB par défaut

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

## 📞 Support

### Documentation
- [GitHub Codespaces](https://docs.github.com/en/codespaces)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework](https://docs.microsoft.com/en-us/ef/)

### En cas de problème
1. Vérifiez les logs dans le terminal
2. Consultez la documentation Swagger
3. Redémarrez le Codespace
4. Ouvrez une issue sur GitHub

---

**🎉 Votre API Institut Froebel est maintenant configurée pour GitHub Codespaces !** 