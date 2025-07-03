# Institut Froebel API

API Multi-Tenant pour la gestion de l'Institut Froebel - Développée avec ASP.NET Core 9.0

## 🚀 Déploiement avec GitHub Codespaces

### Qu'est-ce que GitHub Codespaces ?

GitHub Codespaces est un environnement de développement cloud qui vous permet d'exécuter votre code directement dans le navigateur. Avec le port forwarding, vous pouvez exposer votre API temporairement sur internet.

### 🛠️ Configuration requise

- Un compte GitHub
- Un repository GitHub contenant ce projet
- Accès à GitHub Codespaces (inclus dans GitHub Pro, Team, ou Enterprise)

### 📋 Étapes pour déployer avec GitHub Codespaces

#### 1. Préparation du repository

Le projet est déjà configuré avec :
- `.devcontainer/devcontainer.json` - Configuration de l'environnement
- Structure de projet optimisée pour Codespaces
- Configuration des ports (5000, 5001, 8080)

#### 2. Lancement de GitHub Codespaces

1. **Ouvrir le repository** sur GitHub
2. **Cliquer sur le bouton "Code"** (vert)
3. **Sélectionner l'onglet "Codespaces"**
4. **Cliquer sur "Create codespace on main"**

#### 3. Configuration automatique

Une fois le Codespace lancé :
- L'environnement .NET 9.0 sera automatiquement installé
- Les dépendances seront restaurées (`dotnet restore`)
- Le projet sera compilé (`dotnet build`)
- Les ports seront automatiquement forwardés

#### 4. Accès à l'API

Après le démarrage, vous aurez accès à :

- **API HTTP** : `https://[codespace-id]-5000.preview.app.github.dev`
- **API HTTPS** : `https://[codespace-id]-5001.preview.app.github.dev`
- **Swagger UI** : `https://[codespace-id]-8080.preview.app.github.dev`

### 🔧 Configuration des ports

Les ports suivants sont configurés dans `.devcontainer/devcontainer.json` :

| Port | Service | Description |
|------|---------|-------------|
| 5000 | API HTTP | Endpoint principal de l'API |
| 5001 | API HTTPS | Endpoint sécurisé de l'API |
| 8080 | Swagger UI | Documentation interactive de l'API |

### 🚀 Lancement de l'application

Dans le terminal du Codespace :

```bash
# Naviguer vers le projet API
cd InstitutFroebel.API

# Lancer l'application
dotnet run
```

### 📚 Utilisation de l'API

#### 1. Documentation Swagger

Accédez à la documentation interactive via Swagger UI :
```
https://[codespace-id]-8080.preview.app.github.dev/swagger
```

#### 2. Endpoints principaux

- **Authentification** : `POST /api/auth/login`
- **Inscription** : `POST /api/auth/register`
- **Écoles** : `GET /api/ecole`
- **Élèves** : `GET /api/enfant`

#### 3. Headers requis

Pour les requêtes multi-tenant :
```
X-School-Code: FROEBEL_ABJ
```

Pour l'authentification :
```
Authorization: Bearer [jwt-token]
```

### 🔒 Sécurité

⚠️ **Important** : Cette configuration est pour le développement uniquement.

- Les clés JWT sont en clair dans `appsettings.json`
- La base de données PostgreSQL est accessible publiquement
- CORS est configuré pour accepter toutes les origines en développement

### 🛠️ Développement local

Pour développer localement :

```bash
# Cloner le repository
git clone [url-du-repo]

# Restaurer les dépendances
dotnet restore

# Lancer l'application
cd InstitutFroebel.API
dotnet run
```

### 📦 Structure du projet

```
InstitutFroebel/
├── .devcontainer/
│   └── devcontainer.json          # Configuration Codespaces
├── InstitutFroebel.API/           # Projet principal
│   ├── Controllers/               # Contrôleurs API
│   ├── Services/                  # Services métier
│   ├── Data/                      # Contexte de base de données
│   ├── DTOs/                      # Objets de transfert
│   └── Program.cs                 # Point d'entrée
├── InstitutFroebel.Core/          # Entités et interfaces
├── InstitutFroebel.Infrastructure/ # Implémentations
└── README.md                      # Ce fichier
```

### 🔧 Configuration de la base de données

L'API utilise PostgreSQL avec la configuration suivante :
- **Host** : pg-31cff761-groupechanel2022-ef82.h.aivencloud.com
- **Port** : 12060
- **Database** : defaultdb
- **SSL** : Requis

### 📝 Notes importantes

1. **Temps de démarrage** : Le premier lancement peut prendre 2-3 minutes
2. **Ports** : Les URLs des ports forwardés sont générées automatiquement
3. **Persistance** : Les données sont persistées dans la base de données cloud
4. **Limitations** : Codespaces a des limites de temps d'exécution selon votre plan GitHub

### 🆘 Dépannage

#### Problème de connexion à la base de données
```bash
# Vérifier la connexion
dotnet run --project InstitutFroebel.API
```

#### Problème de ports
- Vérifiez que les ports 5000, 5001, 8080 sont bien forwardés
- Redémarrez le Codespace si nécessaire

#### Problème de compilation
```bash
# Nettoyer et reconstruire
dotnet clean
dotnet restore
dotnet build
```

### 📞 Support

Pour toute question ou problème :
1. Vérifiez les logs dans le terminal
2. Consultez la documentation Swagger
3. Ouvrez une issue sur GitHub

---

**Développé avec ❤️ pour l'Institut Froebel** 