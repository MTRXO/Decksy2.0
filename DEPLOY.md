# Wdrożenie Decksy na serwer (Hetzner + Docker)

Aplikacja, baza SQL Server Express i Caddy (HTTPS) działają w Dockerze na jednym serwerze.
Baza nie jest wystawiona do internetu – widzi ją tylko aplikacja.

## 0. Co przygotować

- Serwer **Hetzner Cloud CX22** (lub większy), system **Ubuntu 24.04**.
- **Domena** z rekordem DNS `A` wskazującym na IP serwera (np. `decksy.twojadomena.pl`).
- Klucz SSH na swoim komputerze. Jeśli go nie masz, w PowerShellu:

  ```powershell
  ssh-keygen -t ed25519
  ```

  Zawartość pliku `C:\Users\<ty>\.ssh\id_ed25519.pub` wklej w Hetznerze przy tworzeniu serwera.

## 1. Zaloguj się na serwer

```powershell
ssh root@IP_SERWERA
```

## 2. Zainstaluj Dockera i włącz zaporę

```bash
curl -fsSL https://get.docker.com | sh
```

```bash
ufw allow OpenSSH && ufw allow 80/tcp && ufw allow 443 && ufw --force enable
```

## 3. Pobierz aplikację

Jeśli repozytorium na GitHubie jest publiczne:

```bash
git clone https://github.com/MTRXO/Decksy2.0.git /opt/decksy
```

Jeśli jest prywatne, użyj tokenu GitHuba (Settings → Developer settings → Personal access tokens) jako hasła przy `git clone`,
albo skopiuj projekt ze swojego komputera (PowerShell, w folderze repozytorium):

```powershell
scp -r Dockerfile docker-compose.yml Caddyfile .env.example .dockerignore Decksy2.0 root@IP_SERWERA:/opt/decksy/
```

## 4. Ustaw domenę i hasło do bazy

```bash
cd /opt/decksy && cp .env.example .env && nano .env
```

- `DOMAIN` – Twoja domena (bez `https://`).
- `DB_PASSWORD` – **nowe, mocne** hasło, używane tylko tutaj. Bez znaków `;` `'` `"` `$` i spacji.

Zapisz: `Ctrl+O`, `Enter`, `Ctrl+X`. Potem zablokuj dostęp do pliku:

```bash
chmod 600 /opt/decksy/.env
```

## 5. Uruchom

```bash
cd /opt/decksy && docker compose up -d --build
```

Pierwsze uruchomienie trwa kilka minut (pobieranie obrazów i budowanie aplikacji).
Aplikacja sama utworzy tabele w bazie, a Caddy pobierze certyfikat HTTPS.

Sprawdź, czy wszystko działa:

```bash
docker compose ps
```

```bash
docker compose logs -f app
```

(`Ctrl+C` kończy podgląd logów). Otwórz `https://TWOJA_DOMENA` w przeglądarce.

## Aktualizacja aplikacji

Po wypchnięciu zmian na GitHuba:

```bash
cd /opt/decksy && git pull && docker compose up -d --build
```

Konta i talie zostają (są w wolumenie bazy), użytkownicy nie zostaną wylogowani.

## Kopia zapasowa bazy

```bash
cd /opt/decksy && set -a && . ./.env && set +a
```

```bash
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_PASSWORD" -C -Q "BACKUP DATABASE DekcsyDatabase TO DISK = N'/var/opt/mssql/backup.bak' WITH INIT"
```

```bash
docker compose cp db:/var/opt/mssql/backup.bak ./backup-$(date +%F).bak
```

Plik `backup-RRRR-MM-DD.bak` pojawi się w `/opt/decksy`. Pobierz go na swój komputer (PowerShell):

```powershell
scp root@IP_SERWERA:/opt/decksy/backup-*.bak .
```

Warto też włączyć automatyczne kopie serwera w panelu Hetznera (Backups, +20% ceny serwera).

## Gdy coś nie działa

| Objaw | Co sprawdzić |
|---|---|
| Strona się nie otwiera | `docker compose ps` – czy wszystkie 3 usługi są `running`/`healthy`; czy rekord DNS wskazuje na IP serwera. |
| Błąd certyfikatu | `docker compose logs caddy` – domena musi już wskazywać na serwer, porty 80 i 443 muszą być otwarte. |
| `db` jest `unhealthy` | `docker compose logs db` – najczęściej za słabe hasło `DB_PASSWORD` (SQL Server wymaga min. 8 znaków, wielkie i małe litery, cyfry, symbol). |
| Błąd 500 | `docker compose logs app` |

## Maile

Aplikacja na razie nie wysyła maili, dlatego linki „Forgot password?” i „Resend email confirmation”
na stronie logowania są ukryte (`Areas/Identity/Pages/Account/Login.cshtml`).
Jeśli ktoś zapomni hasła, trzeba je zresetować ręcznie.
