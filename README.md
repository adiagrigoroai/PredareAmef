# Predare AMEF

Utilitar **C#/.NET 4.7.2** pentru predare memorie fiscala AMEF Datecs (FP-700/FP-800/FP-950) prin DUDE COM cu CMD 116.

## Caracteristici
- Multi-aparat (scanner COM + concurenta configurabila)
- Retry inline ANAF + buton "Reia ANAF" dupa reboot fizic
- Login parola la pornire (0841)
- **Auto-update** din GitHub Releases (verificare la pornire)
- Light Flat UI modern + icon procedural

## Versiune curenta
`1.3.0` — vezi [CHANGELOG.md](CHANGELOG.md) pentru istoric

## Build
```bash
"C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" \
  PredareAmef.sln /p:Configuration=Release /v:minimal
```

**Inchide app inainte de build** (lock pe `Interop.dude.dll` in bin/Release).

## Auto-update
Aplicatia verifica la pornire `https://api.github.com/repos/adiagrigoroai/PredareAmef/releases/latest`. Daca exista versiune mai noua, propune update + descarca + restart automat.

Repo-ul fiind privat, **GitHubToken** trebuie setat in App.config:
```xml
<add key="GitHubToken" value="ghp_xxxxxxxxxxxxxxxxxx" />
```

Token-ul are nevoie doar de permisiunea `contents:read` pe acest repo.

Pentru a dezactiva check-ul: `AutoCheckUpdate = false` in App.config.

## Cum se face un release nou
1. Bump `AssemblyVersion` + `AssemblyFileVersion` in `Properties/AssemblyInfo.cs`
2. Build Release
3. Push commit + tag: `git tag v1.4.0 && git push origin v1.4.0`
4. GitHub release nou (din UI sau cu `gh release create v1.4.0 PredareAmef.exe -t "v1.4.0" -n "Note"`)
5. Asset-ul `PredareAmef.exe` se atașează la release

## Setup aplicatie pe PC client
1. Copiaza pe PC client: `PredareAmef.exe`, `PredareAmef.exe.config`, `Interop.dude.dll`
2. Editeaza `PredareAmef.exe.config` → setează `GitHubToken` cu PAT valid
3. Lansează aplicatia → login `0841`

## Structura proiect
- `Forms/` — UI (LoginForm, MainForm, MultiSessionForm, UpdateDialog, Theme)
- `Services/` — logica (DudeClient, HandoverOrchestrator, FiscalMemoryReader, MfReader, MfPrinter, XmlExporter, HeaderReader, DeviceScannerService, EmailService, MultiSessionRunner, UpdateService)
- `Properties/` — AssemblyInfo
- `lib/` — Interop.dude.dll (wrapper COM Datecs)
- `appicon.ico` — icon aplicație (256/128/64/48/32/16 PNG embedded)
