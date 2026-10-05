# Project completion report - Nexus Service Marketing System

## Specification coverage
| Requirement (from the problem statement) | Status |
|---|---|
| Database: plans, retail shops, employees, customers, vendors, orders, products, materials issued, connections, bills, payments, feedback | Done - 19 tables, 5 EF migrations |
| Separate logins: Admin, Accounts, Technical, Retail employee, Customer | Done (role-based areas) |
| Admin maintains employees, stock, vendors, retail shops, plans (insert / update / delete / search) | Done - plan search and delete added |
| Order id 11 chars (D/B/T + 10 digits) and account id 16 chars (type + 3-digit city + 12-digit serial) | Done (generated server-side, DB check constraints) |
| Only retail staff (and customers themselves) place orders | Done |
| Feasibility check rules (landline + internet, or internet only if Nexus landline exists) | Done |
| Technical: update orders, create connections, temporary / permanent inactive, equipment | Done - equipment & stock page added for Technical |
| Accounts generate bills; 12.24 % service tax; deposit; bulk discount (25/50/75/100 %) | Done |
| Payments by retail staff and accounts; paid and due shown | Done |
| Retail staff track orders, connections, billing and payments till date | Done - Connections and Payment history pages added |
| Search order by order id, connection by account id | Done - public Track page added |
| Advanced search: id, name, connection type, date / period, contact number | Done - date-range inputs were missing from the page, now added |
| Customer: profile, place order, track order, check bills, account status | Done - "My connections" page added |
| Customer documents filed by year and city | Done |
| Feedback collection | Done |
| Plan details visible to staff and customers | Done - public Plans & prices page |

## Fixed / added in this delivery
1. Home page, Privacy page, layout and CSS were still the empty ASP.NET template - redesigned (landing page, role menus, dashboards, tables, forms).
2. Public Plans & prices catalogue and public Track page (order / account status; amounts only after sign-in).
3. Admin plans: search box and safe delete (plans already used are protected).
4. Admin advanced search: date range inputs (applied from / through) now work for orders and connections.
5. Customer "My connections", Retail "Connections" and "Payment history", Technical "Equipment & stock".
6. Bug: a customer with no e-mail could not save their profile ("That email is already used"). Fixed.
7. Money formatted in US dollars (en-US) on every server, as the specification quotes all prices in $.
8. README with SQL Server migration commands, SQL script (`Database/`), migration batch file, login credentials.

No database change was needed: the existing migrations are unchanged.

## Known limits
- Yearly STD landline plan has no price in the specification; it is seeded inactive (price 0). Admin must set its price before activating it.
- Dial-up orders require an active Nexus landline connection for the customer (as the specification says).
- No online payment gateway (payments are recorded by staff).

## Added after the second audit
- Technical > Connections: **Change plan** (same connection type, future bills use the new plan) and a Deposit column (refundable when permanently closed).
- Customer > Orders > Details: **Cancel order** (until the connection is created).

## Final audit (against the problem statement and the project specification)
Checked: all 19 tables (entity classes = EF snapshot = SQL script, 44 CHECK constraints), every plan price/deposit, the
12.24 % tax, bulk discount bands, order / account ID formats, all five role areas, documents and feedback.

Fixed / added in this final delivery
1. **Advanced search for Retail, Technical and Accounts** (Search menu). Retail sees only its own shop. Admin keeps its own page.
2. **Technical > Orders and feasibility**: finished orders (not feasible / connected / cancelled) stay visible, so staff can track every order.
3. **Technical > Connections > Replace spoiled equipment**: issues a replacement from stock, marks the old unit returned,
   records it as a replacement (the replacement charge is added to the next bill by Accounts).
4. **Admin > Reports > Equipment demand**: modems/routers needed by open internet orders vs stock, with the shortfall to purchase from vendors.
5. **Order forms**: plans are grouped by service type, show the price, and the list filters to the chosen service.
6. **Retail > Collect payment** lists only bills the shop is allowed to collect (before, it listed bills the system then refused).
7. **Feasibility form**: distance is now recorded for both check types, and the form no longer loses its order details after a validation error.
8. **Provision form**: no longer loses the order number / quantity after a validation error.
9. README: the default database is SQL Server Express (`.\SQLEXPRESS`), as in appsettings.json.

No database change: the 4 migrations and `Database\NexusServiceMarketingDb-schema.sql` are unchanged and complete.

## Known limits (decisions left to the Admin / group)
- Yearly STD landline plan has no price in the specification: it is seeded inactive (price 0). Set the price in Admin > Plans, then activate.
- A dial-up order needs an active Nexus landline connection of the customer (create a Telephone order first if the customer has none).
- No online payment gateway: payments are recorded by Retail / Accounts staff.

## Specification gap fixes (October 2026)
Reviewed again against the problem statement; these points were missing or only partly done and are now implemented.
New migration `SpecGapFixes` (adds columns only; existing data is back-filled) and an updated `Database\NexusServiceMarketingDb-schema.sql`.

1. **Telephone + dial-up applied for together.** Before, a dial-up order from a customer without a Nexus landline ran both
   feasibility checks but could never be connected. The order now carries a landline plan; Technical enters one phone number per
   line and the system creates the telephone line(s) and the linked dial-up connection(s).
2. **Previous dues on the bill.** Each bill records the unpaid amount of the connection's earlier bills ("brought forward"),
   shown on the bill lists, the customer's bills page and the payment forms.
3. **Charges calculated from the plan.** Call charges = minutes x the plan's local / STD / mobile rates (rejected when the plan has
   no such rate, or for internet connections). Accounts sees a preview of every charge before generating the bill.
4. **Replacement charges billed automatically.** A replacement issued by Technical records the product's replacement charge;
   the next bill includes it and marks it billed, so it is charged exactly once.
5. **Plan validity respected.** The plan fee is charged once per validity period instead of on every bill.
6. **Postpaid connection status.** Technical > Overdue lists active connections with overdue bills ("Suspend" / "Suspend all")
   and suspended connections whose bills are paid ("Reactivate").
7. **Bulk discount counts all the customer's connections**, not only the quantity of one order.
8. **Bug fix - customer documents were saved corrupted**: the first bytes of every uploaded file (read for the type check)
   were dropped, so stored PDFs/images could not be opened. Files are now stored byte-for-byte.
9. **Retail > Collect payment** form could be opened for a bill of another shop; it is now limited to the shop's own bills.
10. **Coding standard ("every code block must have comments")**: every controller, service and form model now has comments;
    one-line packed controllers (Operations, Portal, Documents, Purchases, Technical connections, Retail orders / payments)
    were reformatted.

Tested end to end on SQL Server 2022 (49 scripted checks over all five roles: combined order, feasibility, provisioning,
billing amounts and tax, plan validity, replacement charge, brought-forward balance, suspension / reactivation, bulk discount,
document upload, shop isolation), plus the SQL script on an empty database.

Still to do before submission (cannot be done in code): fill the [placeholders] in the documents, refresh the Word table of
contents, take the `.bak` database backup, and name the ZIP Batch_Group_Title.

## UI redesign - 3D motion, parallax and micro-interactions (October 2026)
Design direction produced with the "UI UX Pro Max" design-intelligence skill (style, palette, typography and motion
searches), adapted where the generated output did not fit a telecom portal:
- Style: Soft UI Evolution for the work pages + a dark parallax "stage" (home hero, login / register, dashboard headers).
- Palette: telecom sky blue / emerald; primary darkened to #0369A1 so white button text meets 4.5:1 contrast.
- Fonts: Space Grotesk (headings) + DM Sans (body), with system-font fallback when offline.
- Motion: first-visit loader (3D orbiting logo), page cross-fade with top progress bar, word-by-word headline reveal,
  staggered scroll reveal, scroll + pointer parallax, CSS 3D globe, 3D tilt cards with glare, magnetic and ripple buttons,
  submit spinners, password show/hide, self-closing success alerts, count-up figures, back-to-top with scroll ring,
  click-to-copy order numbers and account IDs.
- No new library: plain CSS + JavaScript (wwwroot/css/site.css, wwwroot/js/site.js); transform/opacity only; every effect
  is switched off for prefers-reduced-motion, and hover effects are skipped on touch screens.
- Detail pass: live key figures on every dashboard (per role), icons on all dashboard tiles, pricing cards on the
  Plans page, "Why Nexus" and city-coverage sections on the home page, a full footer, page title bars with breadcrumbs,
  coloured status badges in every table, forms shown as cards, and the two fonts bundled locally (works offline).
