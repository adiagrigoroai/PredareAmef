# PredareAmef — Utilitar predare memorie fiscala AMEF

**Scop**: aplicatie standalone Windows (.NET Framework 4.7.2, WinForms, x86) care face **predarea de memorie** a aparatelor fiscale Datecs (FP-700 si similare) cu un singur click.

---

## Flux operational (7 pasi)

| Pas | Actiune | Mecanism |
|-----|---------|----------|
| 0 | Conectare aparat (Serial/LAN) | `DudeClient` (COM CFD_DUDE) |
| 1+2 | **Dump raw memorie fiscala → `.bin`** | CMD 116 `service_Get_FiscalMemory`, chunk-uri 106 bytes |
| 3 | Interpretare `.txt` (sumar per Z) | CMD 95 tip 10 (`report_FM_ByZReports`) |
| 4 | FM text Z1 → ultim Z | CMD 95 tip 10 (duplicat cu 3, optional) |
| 5 | Printare sumar pe aparat | CMD 95 tip 0 |
| 6 | Export XML 3 luni (`.p7b`/XML raw) | CMD 128 (`EXPORT_XML`) pe interval date |
| 7 | Antet → `.txt` | CMD 255 `Header` (index 0-8) + `Footer` |

Output: `Desktop\Predare_<Client>_<YYYYMMDD_HHmmss>\` cu toate fisierele (`<Serial>.bin`, `<Serial>.txt`, `<Serial>_FM_Z1-Z{N}.txt`, `<Serial>_antet.txt`, `XML/<luna>/<fisiere>.xml`).

---

## Descoperirea cheie: CMD 116

Am reverse-engineered FMI.exe (Delphi + DUDE COM, UPX packed). FMI foloseste **CMD 116** nedocumentat pentru dump raw al flash-ului FM:

```
Input:  Operation=0 \t Address=<HEX string, max 6 chars> \t nBytes=<decimal, max 106>
Output: ErrorCode \t HexData  (2 hex chars per byte)
```

**PoC validat**: primii 512 bytes cititi via PredareAmef = 100% byte-identici cu `.bin`-ul produs de FMI pe acelasi aparat. Dupa fix-ul EOF (16 KB tail 0xFF), generatie completa functioneaza.

### EOF detection (important!)

FM are gap-uri interne mari de 0xFF (zone nescrise intre header records, VAT changes, etc.) — **max observat = 8556 bytes**. Nu opri la gap-uri mici. Prag folosit: **16 KB continuu de 0xFF** dupa depasirea dimensiunii estimate (7222 + nZreport * 254 bytes).

---

## Structura .bin Datecs (decriptata)

```
0x000-0x0FF  : 256 bytes 0xFF (padding header)
0x100        : Serial number ASCII (12 bytes, ex "DB4700013896")
0x10C        : BCD date serial set (7 bytes: YYYY u16 LE + MM + DD + HH + MI + SS)
0x118        : BCD date fiscalization
0x127-0x15B  : Header line 1 (48 bytes, ASCII NUL-padded)
0x15C-...    : Header lines 2-N (fiecare 48 bytes)
0x3A8+       : Header change records — 656 bytes fiecare (0x290)
0x2A34+      : VAT change records — ~30 bytes
0x3D44+      : **Z records — 176 bytes fiecare** (2162 × 176 = 380 KB pentru FM plin)
```

Z record layout (partial decodat):
- 0x00-0x06: date BCD (7 bytes)
- 0x07: pad
- 0x08-0x0B: u32 Documents
- 0x0C-0x0F: u32 Fiscal documents
- 0x10-0x13: u32 Invoice documents
- 0x14+: money fields (encoding neobisnuit — posibil Currency Delphi shiftat × 256 sau cents u32 la offset impare)
- 0xA4: u32 Currency rate × 10000 (44,20 → 0x06BE90 = 442000)

---

## Arhitectura proiectului

```
PredareAmef/
├── PredareAmef.sln
├── PROJECT.md                       ← asta
└── PredareAmef/
    ├── PredareAmef.csproj           (.NET 4.7.2, C# 7.3, x86, WinExe)
    ├── App.config
    ├── Program.cs
    ├── Forms/
    │   ├── MainForm.cs              (UI code-behind + threading worker)
    │   ├── MainForm.Designer.cs     (layout: conn + output + 6 checkbox-uri pasi)
    │   └── MainForm.resx
    ├── Services/
    │   ├── DudeClient.cs            (wrapper COM peste CFD_DUDE — simplu, fara pre-check)
    │   ├── Logger.cs                (ILogger + ActionLogger cu Progress + SubProgress)
    │   ├── HandoverOptions.cs       (config + DeviceInfo)
    │   ├── FiscalMemoryReader.cs    (pasii 1+2: CMD 116 dump → .bin, cu tail EOF)
    │   ├── MfReader.cs              (pasii 3/4: CMD 95 → .txt)
    │   ├── MfPrinter.cs             (pas 5: CMD 95 tip 0 → printer)
    │   ├── XmlExporter.cs           (pas 6: CMD 128 pe 3 luni)
    │   ├── HeaderReader.cs          (pas 7: CMD 255 Header/Footer → .txt)
    │   └── HandoverOrchestrator.cs  (runner complet, 7 pasi)
    └── lib/
        └── Interop.dude.dll         (COM wrapper Datecs, copiat din StatusAMEF)
```

---

## Build & Run

```bash
# MSBuild (NU dotnet build — .NET Framework)
"C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" \
  C:\Users\adria\source\repos\PredareAmef\PredareAmef.sln /p:Configuration=Release /v:minimal

# Executabil
C:\Users\adria\source\repos\PredareAmef\PredareAmef\bin\Release\PredareAmef.exe
```

**Inchide app inainte de build** — locks pe `Interop.dude.dll` din `bin/Release`.

---

## Probleme cunoscute + solutii

### „Aparatul nu raspunde" / `Device not connected`
USB virtual serial (Datecs) poate intra in stare blocata. **Fix software (remote)**:
```powershell
$dev = Get-PnpDevice | Where-Object { $_.FriendlyName -match 'Datecs Virtual Serial' }
$dev | ForEach-Object {
    Disable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false
}
Start-Sleep 2
$dev | ForEach-Object {
    Enable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false
}
```
Echivalent software al unplug USB.

### Port ocupat (`Access to port denied`)
- Inchide Config Tool Datecs / FMI.exe / FiscalArhive / alte apps care tin portul
- Kill `dude.exe` orfane: `Get-Process -Name dude | Stop-Process -Force`
- Daca exista `dude.exe` rulate ca SYSTEM (PID nekillable), reboot necesar

---

## TODO — continuare

### Finalizat in sesiunea 2026-04-27
- [x] **Auto port-conflict handling in DudeClient** — `PortConflictRecovery.cs` cu recovery-on-failure (kill stale `dude.exe` + reset PnP USB Datecs via PowerShell). NU pre-check (interfereaza cu DUDE).
- [x] **Export ANAF `.p7b` oficial** prin `download_ANAF_DTRange()` — `XmlExporter` rescris. Output in `ANAF_p7b/YYYY-MM/{CUI}_Z{NNNN}.p7b`.
- [x] **File logger persistent** — `<output>/predare.log` UTF-8 cu format `[HH:mm:ss] LEVEL message`. AutoFlush ON. Sub-progress NU se logheaza.
- [x] **Auto-open folder output** la finalizare (chiar si pe esec partial / anulare).
- [x] **Cancel button + cooperative cancellation** — `CancellationToken` propagat prin `HandoverOrchestrator.Run` → `FiscalMemoryReader` / `MfReader` / `XmlExporter`. Buton rosu in UI, MessageBox de confirmare.

### Ramas pentru viitor
- [ ] **Decodificare completa .bin** pentru a genera `.txt` in format tabular FMI (63 coloane) — necesita reverse engineering money encoding pe Z record
- [ ] **Service mode** optional (CMD 253 cu parola) pentru comenzi privilegiate
- [ ] **Progress total** — ETA bazat pe toti 7 pasi, nu doar pasul curent

---

## Timing tipic (FP-700 cu 2162 Z-uri)

| Pas | Durata |
|-----|--------|
| Conectare + identitate | ~1 sec |
| Dump .bin (557 KB) | **~10 min** (5260 × CMD 116) |
| Interpretare .txt (CMD 95 sumar × 2162 Z) | ~3-5 min |
| Print sumar pe aparat | ~30 sec - 1 min |
| Export XML 3 luni | ~1-2 min |
| Antet | instant |

**Total: ~15-20 min** pentru un aparat cu istoric complet.

---

## Jurnal sesiune

**Data**: 2026-04-21 → 2026-04-22

**Workflow descoperit**:
1. User a rulat FMI.exe → folder cu `<Serial>.bin` + `<Serial>.txt` pe Desktop
2. Am monitorizat procesul si am identificat output-ul
3. Am dezarhivat FMI (UPX) si identificat ca e Delphi + DUDE COM
4. Am enumerat comenzile DUDE pe aparat si am descoperit CMD 116
5. Am validat byte-perfect match cu `.bin`-ul FMI
6. Am implementat `FiscalMemoryReader` + orchestrator nativ
7. Am eliminat dependenta FMI.exe complet

**Fisiere referinta FMI**:
- `C:\Users\adria\Desktop\Cartofiserie DB4700013896\DB4700013896.bin` (557282 bytes)
- `C:\Users\adria\Desktop\Cartofiserie DB4700013896\DB4700013896.txt` (797376 bytes)
