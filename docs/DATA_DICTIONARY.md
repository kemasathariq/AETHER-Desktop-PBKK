# Data dictionary and source mapping

Source: `references/aether_source/bundle.js` (`window.CALIBER_DATA`, CALIBER 2026 Case 2 data). The file is read-only
input. `scripts/extract_seed.py` converts it to `src/Aether.Infrastructure/Seed/aether_seed.json`, which is embedded
in the application and imported once. Nothing is invented: a field that is not in the source is not in the database.

## What the source contains

| Source block | Content | Used by the app |
|---|---|---|
| `incidents` | 380 incidents, 12 plants, 2024-01-04 to 2026-07-25 | Yes: `Incidents` |
| `equipment_master` | 5 assets (nameplate, criticality, linked AR number) | Yes: `Assets` |
| `weekly_condition` | 5 assets x 26 weeks x 4 parameters = 520 values, with weekly Health Status | Yes: `ConditionRecords` |
| `hero_cases[tag].limits` | Alarm/trip limit and direction per weekly parameter | Yes: limit columns of `ConditionRecords` |
| `hero_cases[tag].corrective` / `proactive` | 20 RCA actions with owner (PIC), plan date, status | Yes: `ActionItems` |
| `hourly_readings` | 720 hourly PI-tag values per asset, z-scores, lead-time analysis | No (see D-09) |
| `fleet_summary` | Totals precomputed by the prototype | No (the app calculates its own) |
| `hero_cases` narrative (RCA text, 4M, PM schedule, `preventive`) | Free text and actions without a status | No |

Coverage limits that the UI states: condition data exists for 5 of 380 assets; RCA actions exist for 5 incidents.

## Incident (`Incidents`)

| App field / column | Source field | Type | Notes |
|---|---|---|---|
| `Id` | none | integer PK | Generated. |
| `SourceSerialNo` | `serial_no` | integer, unique, null for user rows | The reliable source key. |
| `Reference` | `mto_no` | text | **Not unique in the source** (`MTO-2026-OPP-0096` appears twice), so it is not a key. |
| `ArNo` | `ar_no` | text | Links an incident to its RCA data. |
| `Title` | `risk_case_title` | text, required | |
| `Plant` | `plant` | text, required | 3-letter plant code. |
| `TagNumber` | `tag_number` | text, required | Equipment tag. Joins to `Assets.Tag` by value; no foreign key, because most incident tags have no asset record. |
| `EquipmentClass` | `eq_class` | text | A/B/C. |
| `EquipmentType` | `eq_type` | text | Code as in the source. |
| `Discipline` | `discipline` | text | ROT/INS/STA/ELE. |
| `Component` | `component` | text | |
| `FailureMechanism` | `f_mechanism` | text | |
| `HighestImpact` | `highest_impact` | text | |
| `PreRisk` | `pre_risk` | text I-IV | **Source classification**, never used in the calculated priority. |
| `RiskScore` | `risk_score` | real >= 0 | Source value tied to Pre-Risk. |
| `PicRca` | `pic_rca` | text | Role code, not a person. |
| `Status` | `overall_status` | one of 6 values | `NEW REGISTERED`, `RCA PROCESS`, `CA/PA EXECUTION`, `MONITORING RESULT`, `RISK CLOSED`, `RISK CANCELED`. CHECK constraint. |
| `OccurredOn` | `date_occur` | ISO date text `yyyy-MM-dd` | |
| `RcaDueDate` | `rca_due_date` | ISO date, nullable | Empty in 197 source rows, stored as NULL. |
| `DowntimeHours` | `downtime_hrs` | real >= 0, nullable | Hours. 80 source rows record 0. |
| `ActualLossKUsd` | `act_loss_k_usd` | real >= 0, nullable | Thousand USD. |
| `PotentialLossKUsd` | `pot_loss_k_usd` | real >= 0, nullable | Thousand USD. |
| `TotalLossKUsd` | `total_loss_k_usd` | **derived**, not stored | Actual + potential. Matches the source total on all 380 rows. |
| `Origin` | none | `Source` or `User` | `Source` = came from the case data (it may have been edited since). |

Not imported: `priority_score`, `priority_rank` (ranking of the original prototype; its method is not described in
the data, so it is not mixed with this app's documented rule), `f_mechanism_corrected` (prototype data-cleaning flag),
`month_year` (derivable from the date).

## Asset (`Assets`)

| App field | Source field (`equipment_master`) |
|---|---|
| `Tag` (unique) | key and `Equipment Tag` |
| `Name` | `Equipment Name` |
| `Type` | `Equipment Type` |
| `EquipmentClass` | `Equipment Class` |
| `PlantUnit` | `Plant / Unit` |
| `Discipline` | `Discipline` |
| `Criticality` | `Criticality` |
| `DesignLife` | `Design Life` |
| `MonitoringMethod` | `Monitoring Method` |
| `LinkedArNo` | `Linked RCA / AR No.` |
| `DominantFailureMode` | `Dominant Failure Mode` |

`Failure Date` is not imported; the incident record already holds the date.

## Condition record (`ConditionRecords`)

One row per asset, week and parameter. The source stores a wide table (one column per parameter); the importer
splits the column header "Name (unit)" into `Parameter` and `Unit`.

| App field | Source |
|---|---|
| `AssetId` | asset key of `weekly_condition` |
| `WeekNo` | `Week` |
| `MeasuredOn` | `Date` |
| `Parameter`, `Unit` | column header, for example `DE Radial Vibration (micron)` |
| `Value` | the cell value |
| `AlarmLimit`, `TripLimit`, `LimitDirection` | `hero_cases[tag].limits` entry with the same parameter (`alarm`, `trip`, `direction` high/low). NULL when the source has no limit for that parameter. |
| `HealthStatus` | weekly `Health Status` (NORMAL/ALARM/TRIP). It describes the asset in that week and is repeated on each parameter row of the week. |
| `Remark` | weekly `Remark`, NULL when empty |

Unique on (`AssetId`, `MeasuredOn`, `Parameter`).

## Action item (`ActionItems`)

| App field | Source / rule |
|---|---|
| `IncidentId` | foreign key to `Incidents.Id`, `ON DELETE CASCADE`. Imported actions are matched through the hero case `AR Number` = incident `ar_no`. |
| `Description` | `action` |
| `Owner` | `pic` (role code such as `REL-05`) |
| `TargetDate` | `plan_date` converted from `30-Apr-2026` to ISO |
| `Status` | `Open`, `In Progress`, `Closed` (CHECK constraint). Source `status` values already use these words. |
| `Category` | `Corrective` or `Proactive` (source list name). NULL for user rows. |
| `RootCauseCode` | `rc` |
| `Origin` | `Source` or `User` |

`preventive` entries are not imported because they have no status in the source.

## Conventions

- **Missing values:** NULL means "not recorded". Never replaced by zero in storage, calculations or display
  (the UI shows "Not recorded" or a dash; dashboard notes say how many incidents lack a value).
- **Dates:** ISO text `yyyy-MM-dd`, no time component. An unreadable stored date is read as "not recorded".
- **Units:** downtime in hours; money in thousand USD (`k USD`), as in the source; measurements in the unit parsed from
  the source header.
- **Origin:** `Source` rows come from the case data, `User` rows were entered in the application. No illustrative
  or sample rows exist in the database.
- **Data kinds:** recorded source data (everything imported), derived calculations (`TotalLossKUsd`, dashboard sums,
  calculated priority), and none of the original deck's AI indications or illustrative values are used.
