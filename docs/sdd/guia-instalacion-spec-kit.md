# Guía de instalación y seguimiento — GitHub Spec Kit

> Instalación realizada el **2026-09-11** en Windows 11 (10.0.26200) sobre la rama
> `docs/sdd-spec-kit`. Cada paso incluye el comando, la evidencia real y cómo verificarlo.
>
> **Relacionados:** [Guía de implementación](sdd-implementation.md) · [Propuesta](sdd-proposal.md)

---

## Seguimiento de la instalación

| # | Paso | Estado | Evidencia |
|---|---|---|---|
| 0 | Prerrequisitos verificados | Hecho | §0 |
| 1 | Rama de trabajo creada | Hecho | `docs/sdd-spec-kit` desde `dev` (0159a9b) |
| 2 | CLI `specify` 1.0.6 instalada | Hecho | `specify version` → 1.0.6 |
| 3 | Respaldo previo de archivos del agente | Hecho | Hash de `CLAUDE.md` igual antes y después |
| 4 | `specify init` en la raíz | Hecho | `.specify/` y 10 skills en `.claude/skills/` |
| 5 | Conflicto `.CLAUDE`/`.claude` resuelto | Hecho | Carpeta renombrada; `.gitignore` ajustado |
| 6 | Qué se versiona y qué no | Hecho | `git check-ignore` (§6) |
| 7 | Constitución 1.1.0 | Hecho | `.specify/memory/constitution.md` |
| 8 | Features piloto creadas | Hecho | `specs/001…003` |
| 9 | Verificación final | Hecho | §9 |
| 10 | Commit y PR a `dev` | **Pendiente de confirmación** | — |

---

## 0. Prerrequisitos

| Herramienta | Requerido | Encontrado |
|---|---|---|
| Python | 3.11+ | 3.13 del sistema; `specify` corre en 3.12.14 administrado por uv |
| uv | Recomendado | `…\Python313\Scripts\uv.exe` |
| Git | Sí | `C:\Program Files\Git\cmd\git.exe` |
| PowerShell | Para `--script ps` | PowerShell 7 |
| Claude Code | Agente | Extensión de VS Code |
| GitHub CLI (`gh`) | Solo para `/speckit-taskstoissues` | **No instalado** (no bloquea) |

```powershell
foreach ($t in 'uv','python','git','specify') {
  $c = Get-Command $t -ErrorAction SilentlyContinue
  '{0,-8} {1}' -f $t, ($c ? $c.Source : '-- no encontrado --')
}
```

## 1. Rama de trabajo

Las reglas prohíben trabajar sobre `main` o `dev`.

```powershell
git switch dev
git pull
git switch -c docs/sdd-spec-kit
```

> **Incidente registrado:** durante la instalación el editor cambió la rama a `dev` por su
> cuenta (lo muestra el reflog: `checkout: moving from docs/sdd-spec-kit to dev`). Los
> archivos nuevos no rastreados (`.specify/`, `specs/`) acompañan cualquier cambio de rama, así
> que no se pierde nada, pero **antes de hacer commit hay que confirmar la rama** con
> `git branch --show-current`.

## 2. Instalar la CLI `specify`

Se fija la versión para que la instalación sea reproducible:

```powershell
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v1.0.6
specify version
```

Salida obtenida (resumen):

```text
Installed 16 packages in 437ms
 + specify-cli==1.0.6 (from git+https://github.com/github/spec-kit.git@96c9bd65…)
Installed 1 executable: specify
│     CLI Version    1.0.6          │
│          Python    3.12.14        │
│        Platform    Windows        │
```

Alternativa sin fijar versión: `uv tool install specify-cli` (desde PyPI).

## 3. Respaldo previo

`specify init --force` puede sobrescribir archivos en rutas que administra. Antes de ejecutarlo
se respaldaron `CLAUDE.md` y la carpeta del agente fuera del repo y se registró el hash:

```powershell
$bk = "$env:TEMP\respaldo-previo-init"
New-Item -ItemType Directory -Force $bk | Out-Null
Copy-Item -Recurse -Force .claude "$bk\claude-dir"
Copy-Item -Force CLAUDE.md "$bk\CLAUDE.md"
(Get-FileHash CLAUDE.md).Hash
```

Resultado: el hash de `CLAUDE.md` fue **idéntico antes y después** del `init`
(`B205A772…2F53`): Spec Kit no lo modificó.

## 4. Inicializar Spec Kit en el proyecto existente

```powershell
specify init --here --force --integration claude --script ps --non-interactive
```

| Opción | Por qué |
|---|---|
| `--here` | Inicializa en el repo actual (brownfield) en lugar de crear una carpeta |
| `--force` | El directorio no está vacío; mezcla la plantilla con lo existente |
| `--integration claude` | Instala las skills para Claude Code |
| `--script ps` | Scripts PowerShell (Windows) |
| `--non-interactive` | Sin preguntas; útil en terminal no interactiva |

Salida obtenida:

```text
Selected coding agent integration: claude
Selected script type: ps
├── ● Install integration (Claude Code)
├── ● Install shared infrastructure (scripts (ps) + templates)
├── ● Constitution setup (copied from template)
├── ● Install bundled workflow (speckit installed)
└── ● Finalize (project ready)
Project ready.
```

Archivos creados:

```text
.specify/
├── init-options.json        # integración, script, numeración "sequential", versión 1.0.6
├── integration.json
├── integrations/            # manifiestos de claude y speckit
├── memory/constitution.md   # plantilla (luego completada en el paso 7)
├── scripts/powershell/      # check-prerequisites, common, create-new-feature,
│                            # resolve-template, setup-plan, setup-tasks
├── templates/               # spec, plan, tasks, checklist, constitution
├── workflows/speckit/workflow.yml   # ciclo specify → plan → tasks → implement con puertas
└── .gitignore               # ignora feature.json (estado local)
.claude/skills/
├── speckit-analyze/     speckit-checklist/  speckit-clarify/
├── speckit-constitution/ speckit-converge/  speckit-implement/
├── speckit-plan/        speckit-specify/    speckit-tasks/
└── speckit-taskstoissues/
```

El `init` **no tocó** código de la aplicación ni hizo commits. Emite un aviso de seguridad:
algunos agentes guardan credenciales en su carpeta, por eso se versiona solo `.claude/skills/`.

## 5. Problema encontrado: `.CLAUDE` y `.claude` son la misma carpeta en Windows

El repo ya tenía `.CLAUDE/` (reglas locales para el agente, ignorada por git). En Windows el
sistema de archivos no distingue mayúsculas, así que las skills se escribieron en
`.CLAUDE\skills\`. Si se versionaran así, git las guardaría como `.CLAUDE/skills/…` y en
Linux/macOS Claude Code **no las encontraría** (busca `.claude/skills/`).

```powershell
git config core.ignorecase          # true
(Get-Item -Force .CLAUDE).Name       # .CLAUDE
git check-ignore -v --no-index .claude/skills/speckit-plan/SKILL.md
# .gitignore:100:.claude/   .claude/skills/speckit-plan/SKILL.md
```

**Solución aplicada:**

1. Renombrar en dos pasos (Windows no permite cambiar solo mayúsculas en uno):

   ```powershell
   Rename-Item -LiteralPath .CLAUDE -NewName .claude-tmp
   Rename-Item -LiteralPath .claude-tmp -NewName .claude
   ```

2. Actualizar los enlaces de `CLAUDE.md` de `.CLAUDE/` a `.claude/` (archivo local, ignorado).
3. En `.gitignore`, reemplazar las reglas `.CLAUDE` y `.claude/` por:

   ```gitignore
   .claude/*
   !.claude/skills/
   ```

   Así las reglas locales (`RULES.md`, `commands.md`, `structure.md`) y cualquier configuración
   personal siguen fuera del repo, y solo las skills se comparten.

## 6. Qué se versiona y qué no

| Ruta | Se versiona | Motivo |
|---|---|---|
| `.specify/` (plantillas, scripts, constitución, manifiestos) | Sí | Cualquier integrante ejecuta las skills sin instalar nada más |
| `.specify/feature.json` | No | Puntero local a la feature activa (lo ignora `.specify/.gitignore`) |
| `.claude/skills/speckit-*` | Sí | Mismos comandos para todo el equipo |
| `.claude/RULES.md`, `commands.md`, `structure.md`, `settings*.json` | No | Guías y configuración locales |
| `specs/` | Sí | Artefactos SDD |
| `docs/sdd/` | Sí | Esta documentación |

Verificación (la salida vacía significa "no ignorado"):

```powershell
git check-ignore -v --no-index .claude/skills/speckit-plan/SKILL.md   # (vacío) → se versiona
git check-ignore -v --no-index .claude/RULES.md                        # .gitignore: .claude/*
git check-ignore -v --no-index .specify/feature.json                   # .specify/.gitignore: feature.json
git check-ignore -v --no-index specs/001-e2e-playwright-mcp/spec.md    # (vacío) → se versiona
```

## 7. Constitución

Se ejecutó `/speckit-constitution` a partir de las reglas vigentes, sin inventar reglas nuevas
(recomendación de la guía oficial para proyectos existentes). Resultado:
[`.specify/memory/constitution.md`](../../.specify/memory/constitution.md) **v1.1.0**:

- 1.0.0: principios I–VII y secciones de seguridad y flujo de trabajo, derivados de `RULES.md`
  y del CI.
- 1.1.0 (MINOR): excepción acotada para `driver.js`, solo para tours (piloto b).

## 8. Crear las features piloto

`/speckit-specify` usa internamente `create-new-feature.ps1`, que numera secuencialmente y
guarda la feature activa en `.specify/feature.json`:

```powershell
$s = '.\.specify\scripts\powershell\create-new-feature.ps1'
& $s -Json -ShortName 'e2e-playwright-mcp'          'Pruebas automaticas E2E asistidas por IA con Playwright MCP'
& $s -Json -ShortName 'guia-interactiva-tours'      'Guia interactiva de producto con tours por rol'
& $s -Json -ShortName 'infraestructura-como-codigo' 'Software como infraestructura: Codespaces, Terraform multi-nube y CI/CD'
```

```text
{"BRANCH_NAME":"001-e2e-playwright-mcp", "FEATURE_NUM":"001", …}
{"BRANCH_NAME":"002-guia-interactiva-tours", "FEATURE_NUM":"002", …}
{"BRANCH_NAME":"003-infraestructura-como-codigo", "FEATURE_NUM":"003", …}
```

> El script **no crea ramas de git** (eso lo hace una extensión opcional de git que no se
> instaló). `BRANCH_NAME` es solo el nombre sugerido; la rama real sigue la convención del
> proyecto (`feat/<issue>-<slug>`).

Para trabajar sobre una feature concreta, se fija la variable antes de invocar la skill:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY = 'specs/002-guia-interactiva-tours'
```

## 9. Verificación final

```powershell
specify version                                                        # 1.0.6
Get-ChildItem .claude\skills -Name                                     # 10 carpetas speckit-*
.\.specify\scripts\powershell\check-prerequisites.ps1 -Json -PathsOnly # rutas de la feature activa
git status --short                                                     # .gitignore, .specify/, .claude/skills/, specs/, docs/
```

En Claude Code, escribir `/speckit` debe listar las diez skills. **Reiniciar la sesión** de
Claude Code si no aparecen: las skills se descubren al iniciar.

## 10. Uso diario

| Quiero… | Skill |
|---|---|
| Crear el spec de una necesidad | `/speckit-specify Descripción en lenguaje natural` |
| Resolver ambigüedades | `/speckit-clarify` |
| Diseñar | `/speckit-plan Stack y restricciones` |
| Revisar la calidad de los requisitos de un dominio | `/speckit-checklist seguridad` |
| Partir en tareas | `/speckit-tasks` |
| Validar consistencia (solo lectura) | `/speckit-analyze` |
| Implementar (con aprobación) | `/speckit-implement` |
| Comparar código y artefactos | `/speckit-converge` |

## 11. Incorporar a otra persona del equipo

Como `.specify/` y `.claude/skills/` se versionan, **no necesita la CLI** para el trabajo diario:

1. `git pull` de la rama que contenga la integración.
2. Abrir el repo en VS Code con Claude Code y escribir `/speckit`.
3. Solo si va a actualizar Spec Kit: `uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v1.0.6`.

## 12. Actualizar Spec Kit

```powershell
specify self check                 # ¿hay versión nueva?
specify self upgrade --dry-run     # qué cambiaría
specify self upgrade --tag v1.0.7  # ejemplo de actualización fijada
specify init --here --force --integration claude --script ps --non-interactive  # refresca plantillas y skills
```

Antes del `init --force`, respaldar `.specify/memory/constitution.md` y cualquier override de
plantillas: un refresco forzado puede sobrescribir archivos compartidos.

## 13. Problemas conocidos

| Síntoma | Causa | Solución |
|---|---|---|
| Las skills no aparecen en Claude Code | La sesión se abrió antes del `init` | Reiniciar Claude Code |
| Las skills no aparecen en Linux/macOS | Se versionaron como `.CLAUDE/skills` | Ver §5 |
| La skill trabaja sobre la feature equivocada | `.specify/feature.json` apunta a otra | Fijar `SPECIFY_FEATURE_DIRECTORY` (§8) |
| Los scripts `.ps1` no se ejecutan | Política de ejecución de PowerShell | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| `/speckit-taskstoissues` falla | Falta GitHub CLI / acceso a GitHub | Instalar `gh` o crear los issues a mano; el comando saldrá del núcleo en una versión futura |
| La rama cambió sola | El editor hizo `checkout` | `git branch --show-current` antes de cada commit (§1) |
