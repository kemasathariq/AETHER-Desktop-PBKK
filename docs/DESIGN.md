# UI design

Desktop layout follows the user's SnowUI reference: a light gray workspace, minimal sidebar with white icon tiles, rounded white panels, alternating blue and black statistic cards, and a purple area chart. All displayed figures remain database results.

## Layout

- Fixed 230 px light sidebar with five navigation items, gray selection, keyboard focus, and a note about the local database. A compact header shows the current page breadcrumb.
- Content area per page: title and one-line purpose, controls, table, right-hand panel (details or form) on the list screens, a status message line at the bottom.
- Window: opens at 1360x820, minimum 1100x680. Wide tables scroll; side panels are 400-440 px.

## Palette and type (`Themes/Styles.xaml`)

| Use | Color |
|---|---|
| Sidebar / page | `#F6F6F6` |
| Accent / chart line | `#006FE6` / purple `#A43CE8` |
| Cards | white; statistic cards use blue and black gradients |
| Text / muted text | `#161616` / `#666666` |
| Danger | `#C62828` |
| Priority badges | Critical `#B91C1C`, High `#9A3412`, Medium `#6D28D9`, Low `#166534`, Not rated `#4B5563` (white text) |
| Keyboard focus | purple border; blue focus on text inputs |

Text/background pairs are chosen for at least 4.5:1 contrast. Font Segoe UI: 13 body, 15 section titles, 24 page titles, 30 large numbers. Navigation icons use Segoe MDL2
Assets glyphs (present on Windows 10/11); if a glyph looks wrong, change the code point in `MainViewModel`.

## Components

| Component | Behaviour |
|---|---|
| Buttons | Rounded default, primary blue, danger red; visible keyboard focus; disabled state at 45% opacity |
| Tables | Read-only, single selection, header row, alternating rows, missing numbers shown as a dash, units in the column header |
| Forms | Required fields marked `*`; field errors appear under the field in red after Save; nothing is saved while any error exists |
| Status line | Green "Done:" or red "Problem:" message, announced as a polite live region |
| Empty states | Text in the table area ("No incidents match..." vs "No incidents are stored yet..."), dashboard empty card, asset and chart messages |
| Confirmation | Yes/No message box (default No) before deleting an incident or action; it names the item and the number of actions that go with an incident |
| Badges | Priority and incident/action status pills; words remain visible, so color is never the only signal |

## Screens

1. **Dashboard:** four alternating blue/black cards (total incidents, recorded downtime, recorded actual loss, actions), purple monthly incident line with transparent area, neutral gray status/plant bars, latest eight incidents. Lines connect recorded values without smoothing; one month is displayed as one point. Missing-value and loss notes are also available below the charts and in card tooltips.
2. **Incidents:** search (title, tag, references, plant, component, failure mechanism), status/plant filters, hide closed, table, details or form panel.
3. **Asset Monitoring:** asset list, source information, parameter selector, line chart with dashed alarm/trip lines, measurement table, coverage note.
4. **Incident Priority:** ranked table (calculated priority vs source Pre-Risk shown in separate columns), detail panel with factors, points and basis, "How it is calculated" expander.
5. **Action Tracker:** action table with filters, details or form with incident picker, owner, target date, status.

## Truthful labelling

Recorded values, calculated values and source classifications are labelled where they appear ("Pre-Risk (source)", "Priority (calculated)", "Total loss (calculated)").
The dashboard and asset pages state that data is historical and not live monitoring and that figures are not savings.
