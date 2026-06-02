# Web Blazor Development Instructions

## Project focus
This project is a **web application built with Blazor**.

## Language requirements
The application must support **multilanguage mode** with these languages:
- English
- Slovak
- Czech

### Rules
- All user-visible text must be localizable.
- Do not hardcode UI strings directly in components.
- Use shared localization resources for labels, buttons, messages, validation texts, and notifications.
- Keep translation keys consistent across all supported languages.
- If a translation is missing, fall back to English.
- New features must include localization updates for:
  - `en`
  - `sk`
  - `cs`

## Change presentation process
When implementing a change, always present the output in this structure:

### 1. List of changes
Provide a list of implemented changes.  
Each item must contain:
- **Change title**
- **Link to the change**
- **Short description**

Example:
- Added localized navigation labels — [link]
  - Added translation keys and updated shared navigation component.
- Improved form validation messages — [link]
  - Validation messages now support English, Slovak, and Czech.

### 2. Summary
At the end, provide a short summary containing:
- What was changed
- Impacted areas
- Translation/resource updates
- Any follow-up recommendations

### 3. Possible issues and blockers
Always include a final section named **Possible Issues / Blockers**.

This section should identify:
- Missing translations
- Inconsistent localization keys
- UI layout issues caused by longer translated text
- Culture-specific formatting problems for dates, times, and numbers
- Dependencies on unfinished backend or API changes
- Missing test coverage
- Accessibility concerns caused by dynamic or translated content

## Development expectations
- Keep components clean and reusable.
- Prefer separation of UI, business logic, and localization resources.
- Follow existing project structure and naming conventions.
- Add or update tests where appropriate.
- Preserve backward compatibility unless the change explicitly requires breaking updates.

## Output format for every delivered change
Use this template:

### List of Changes
- **[Change name]** — [link]
  - Short description of the change.

### Summary
- Short overall summary of completed work.
- Notes about localization updates in English, Slovak, and Czech.

### Possible Issues / Blockers
- Issue or blocker 1
- Issue or blocker 2
- None, if no known issues exist.