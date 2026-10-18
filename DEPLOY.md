# Wdrożenie Decksy na serwer (Hetzner + Docker)

Aplikacja i Caddy (HTTPS) działają w Dockerze. Baza to plik SQLite trzymany w wolumenie Dockera,
więc nie potrzeba osobnego serwera bazy – wystarczy serwer z 1–2 GB RAM.

## 0. Co przygotować

- Serwer w **Hetzner Cloud** z systemem **Ubuntu 24.04** (wystarczy 1–2 GB RAM).
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

Jeśli jest prywatne, użyj tokenu GitHuba (Settings → Developer settings → Personal access tokens) jako hasła przy `git clone`.

## 4. Ustaw domenę

```bash
cd /opt/decksy && cp .env.example .env && nano .env
```

Wpisz swoją domenę w `DOMAIN` (bez `https://`). Zapisz: `Ctrl+O`, `Enter`, `Ctrl+X`.

## 5. Uruchom

```bash
cd /opt/decksy && docker compose up -d --build
```

Pierwsze uruchomienie trwa kilka minut (pobieranie obrazów i budowanie aplikacji).
Aplikacja sama utworzy plik bazy, a Caddy pobierze certyfikat HTTPS.

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

Konta i talie zostają (plik bazy jest w wolumenie), użytkownicy nie zostaną wylogowani.

## Kopia zapasowa bazy

Na chwilę zatrzymaj aplikację, skopiuj plik bazy i uruchom ją ponownie (trwa kilka sekund):

```bash
cd /opt/decksy && docker compose stop app && docker compose cp app:/app/data/decksy.db ./backup-$(date +%F).db && docker compose start app
```

Plik `backup-RRRR-MM-DD.db` pojawi się w `/opt/decksy`. Pobierz go na swój komputer (PowerShell):

```powershell
scp root@IP_SERWERA:/opt/decksy/backup-*.db .
```

Warto też włączyć automatyczne kopie serwera w panelu Hetznera (Backups, +20% ceny serwera).

## Gdy coś nie działa

| Objaw | Co sprawdzić |
|---|---|
| Strona się nie otwiera | `docker compose ps` – czy obie usługi są `running`; czy rekord DNS wskazuje na IP serwera. |
| Błąd certyfikatu | `docker compose logs caddy` – domena musi już wskazywać na serwer, porty 80 i 443 muszą być otwarte. |
| Błąd 500 | `docker compose logs app` |

## Maile

Aplikacja na razie nie wysyła maili, dlatego linki „Forgot password?” i „Resend email confirmation”
na stronie logowania są ukryte (`Areas/Identity/Pages/Account/Login.cshtml`).
Jeśli ktoś zapomni hasła, trzeba je zresetować ręcznie.
