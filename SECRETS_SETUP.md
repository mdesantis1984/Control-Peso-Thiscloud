# ============================================
# SECRETS SETUP - Control Peso
# ============================================

## Primera vez
1. Copiar: `Copy-Item .secrets.local.example .secrets.local`
2. Editar: `.secrets.local` con secretos REALES
3. NUNCA commitear `.secrets.local`

## Docker Compose
```bash
# Usa secretos automáticamente
docker compose -f docker-compose.yml -f .secrets.local up -d
```

## Secretos necesarios
- SQL SA Password
- Google OAuth ClientId + ClientSecret
- Telegram BotToken + ChatId

## Rotación (HACER AHORA)
1. Google OAuth: console.cloud.google.com → Delete old → Create new
2. Telegram: @BotFather → /revoke → /token
3. SQL: ALTER LOGIN sa WITH PASSWORD = 'new'
4. Actualizar `.secrets.local` (local + producción)
