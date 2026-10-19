# Demo script (about 8 minutes)

Preparation: close the app, run `scripts\reset-database.ps1`, start `AetherDesktop.exe` (or `dotnet run --project src/Aether.Desktop`). The first start imports the case data.

Say once at the start: "The numbers are historical records from the case data. They are not savings produced by this app, and this is not live plant monitoring."

## 1. Dashboard (1 min)
- Point to the four cards: 380 incidents, recorded downtime 2,261.1 h, recorded actual loss k USD 61,886 (potential loss shown separately), 20 actions.
- Show the three charts (status, plant, per month) and the latest incidents. Note the card text about incidents with no recorded value.
- "All of this is a database query each time the page opens." Keep this page in mind; you come back to it at step 6.

## 2. Incidents: search, filter, details (1 min)
- Search `KO-3201`: one row. Select it and read the details panel (note "Pre-Risk class (source)" and "Total loss (calculated)").
- Search `zzzz`: the "No incidents match" message. Clear filters. Filter Plant `ZCU`, then Status `RISK CLOSED`.

## 3. Incidents: create, validate, edit (2 min)
- New incident. Click Save with the form empty: red messages, nothing saved.
- Fill Title `Demo: coupling noise`, Tag `BL-5702`, Plant `OPP`, keep today's date, Status `NEW REGISTERED`. Type `abc` in Downtime: error. Clear Downtime (leave it empty = not recorded), set Actual loss `100`. Save: success message and the row appears.
- Edit it: change Status to `RCA PROCESS`, save.

## 4. Asset Monitoring (1 min)
- Banner: 5 assets only. Select `KO-3201`, parameter `DE Radial Vibration`: line chart, dashed alarm 45 and trip 75 micron lines, weekly table with units and dates.
- Switch to `Lube Oil Supply Press` (a "low" limit). "Nothing here is filled in or predicted; these are the recorded weekly values."

## 5. Incident Priority (1 min)
- Expander "How the priority is calculated". Select the KO-3201 incident (Critical): downtime 3, loss 3, condition 2 (trip limit reached), and the "Pre-Risk II (source)" text.
- Select your demo incident: downtime was left empty, so that factor shows "Not scored" and the result is flagged "Partial data". "Missing is never counted as zero. This is a rule, not AI, and not a root cause."

## 6. Action Tracker and integrity (1.5 min)
- Action Tracker: 20 imported actions. Search `KO-3201`: 4 actions (1 Closed, 1 In Progress, 2 Open).
- New action for your demo incident: Description `Inspect coupling`, Owner `ROT-01`, date, Status `Open`; save. Edit to `In Progress`.
- Go to Incidents, delete the demo incident: the dialog says its action will go too. Confirm.
- Back to Dashboard: totals are back to 380 and the action count to 20.

## 7. Persistence (30 s)
- Add one incident, close the app, reopen it: the incident is still there, the import did not duplicate anything. Mention the database file under `%LOCALAPPDATA%\AetherDesktop`.

Reset after the demo: `scripts\reset-database.ps1`.
