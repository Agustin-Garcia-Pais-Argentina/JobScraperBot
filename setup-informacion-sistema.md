# Setup de Información del Sistema: Perfil de Usuario (`UserProfile`)

> Documento complementario a `arquitectura-bot-scraper-empleos.md`. Acá se define en detalle el `UserProfile` — la configuración de negocio que alimenta el pipeline de filtros — para que sea editable sin tocar código ni recompilar.

## 1. Criterios base definidos

| Criterio | Regla |
|---|---|
| **Seniority** | Junior, Trainee o Pasantía. Se descarta solo si dice **explícitamente** Senior/SSR/Lead. Si no aclara nivel → se incluye. |
| **Áreas** | Backend, Full-stack, Data Analyst, Data Engineer, Desarrollador (Python/Java/C#/C++) |
| **Pasantías** | Sin filtro de área — cualquier pasantía pasa, **excepto** soporte técnico/mesa de ayuda/atención al cliente |
| **Ubicación** | Remoto: sin restricción. Presencial/híbrido: debe ser Santa Fe Capital. Si no aclara ciudad → se incluye (ambiguo = se incluye). Si dice explícitamente otra ciudad conocida → se descarta. |
| **Salario** | No filtra. Si el sitio lo informa, se muestra en el resumen; si no, no pasa nada. |
| **Horario** | No filtra, no se tiene en cuenta. |

---

## 2. Modelo de configuración

```csharp
public class UserProfile
{
    // Excluye SOLO si menciona explícitamente un nivel no deseado.
    // Ausencia de mención de seniority = NO se descarta (ante la duda, se incluye).
    public List<string> SeniorityExcludeTerms { get; set; } = new()
        { "senior", "ssr", "semi senior", "semi-senior", "sr.", "lead" };

    // OR entre perfiles: alcanza con que matchee UNO solo para que la oferta pase.
    public List<RoleProfile> RoleProfiles { get; set; } = new();

    public LocationProfile Location { get; set; } = new();
}

public class RoleProfile
{
    public string Name { get; set; } = "";

    // Vacío = sin exigencia de seniority propia (usa solo el filtro global).
    // Se usa en el perfil de Pasantías para no dejarlo abierto a cualquier oferta.
    public List<string> RequiredSeniorityTerms { get; set; } = new();

    // Cada sub-lista es un concepto obligatorio: AND entre grupos, OR dentro del grupo.
    // Vacío = sin filtro de área (solo el perfil de Pasantías lo deja vacío).
    public List<List<string>> RequiredKeywordGroups { get; set; } = new();

    public List<string> ExcludeKeywords { get; set; } = new();
}

public class LocationProfile
{
    public List<string> AcceptedOnsiteCities { get; set; } = new()
        { "santa fe capital", "santa fe, santa fe", "ciudad de santa fe" };

    // Ciudades/regiones que, si aparecen explícitamente, son inequívocamente
    // OTRO lugar → se descarta. Todo lo que no caiga acá ni en la lista de arriba
    // se considera "ambiguo" → se incluye (regla: ante la duda, se incluye).
    public List<string> KnownOtherCities { get; set; } = new()
    {
        "rosario", "reconquista", "rafaela", "córdoba", "cordoba",
        "mendoza",
        // AMBA / CABA — todas las variantes de escritura habituales en avisos
        "amba", "caba", "c.a.b.a", "c.a.b.a.", "ciudad autónoma de buenos aires",
        "ciudad autonoma de buenos aires", "buenos aires", "gba", "gran buenos aires"
    };
}
```

> **Nota de mantenimiento:** `KnownOtherCities` es una lista que se va a ir ampliando a mano con el tiempo, a medida que se detecten ofertas de otras localidades puntuales colándose. No tiene sentido resolverla con geolocalización automática — sería complejidad y (probablemente) costo innecesario para este caso de uso.

---

## 3. Perfiles de rol configurados

```json
[
  {
    "name": "Backend",
    "requiredKeywordGroups": [["backend", "back-end", "server-side"]]
  },
  {
    "name": "Full-stack",
    "requiredKeywordGroups": [["full stack", "fullstack", "full-stack"]]
  },
  {
    "name": "Data Analyst",
    "requiredKeywordGroups": [["data analyst", "analista de datos"]]
  },
  {
    "name": "Data Engineer",
    "requiredKeywordGroups": [["data engineer", "ingeniero de datos"]]
  },
  {
    "name": "Desarrollador (Python/Java/C#/C++)",
    "requiredKeywordGroups": [
      ["developer", "desarrollador", "programador", "engineer", "ingeniero"],
      ["python", "java", "c#", ".net", "c++"]
    ]
  },
  {
    "name": "Pasantía / Trainee general",
    "requiredSeniorityTerms": ["pasantía", "pasantia", "trainee", "intern", "internship"],
    "requiredKeywordGroups": [],
    "excludeKeywords": [
      "soporte técnico", "soporte tecnico", "mesa de ayuda",
      "help desk", "service desk", "atención al cliente", "call center"
    ]
  }
]
```

El perfil de **Pasantías** es intencionalmente distinto a los demás: no tiene `requiredKeywordGroups` (sin filtro de área), pero sí exige que la oferta mencione explícitamente "pasantía/trainee/intern" en `requiredSeniorityTerms" — de lo contrario, al no tener filtro de área, pasaría cualquier oferta del mercado. Además excluye soporte técnico y afines.

---

## 4. Orden de evaluación en el pipeline

```
1. SeniorityFilter  → descarta SOLO si dice explícitamente "Senior/SSR/Lead" (global, aplica a todos los perfiles)
2. LocationFilter   → remoto: siempre pasa
                       presencial/híbrida en Santa Fe Capital: pasa
                       presencial/híbrida en ciudad de KnownOtherCities: descarta
                       presencial/híbrida sin ciudad clara: pasa (ambiguo → se incluye)
3. RoleFilter       → pasa si matchea AL MENOS UNO de los RoleProfiles definidos arriba
```

`Salary` y horario no forman parte del pipeline de filtros: `Salary` viaja en el modelo `JobOffer` como campo opcional y se muestra en el resumen si el sitio lo informa, sin afectar el filtrado.

---

## 5. Formato de archivo de configuración

Para que sea editable sin recompilar, este `UserProfile` vive como `appsettings.json` (o `profile.json` separado) dentro de `src/JobScraperBot.App/`, cargado por `IOptions<UserProfile>` de .NET. Cualquier ajuste futuro (agregar un rol, sumar una ciudad, cambiar un término de exclusión) es editar el JSON y commitear — no requiere tocar el código del pipeline.
