# External entity link rule

- Store all external identifiers in `integration.external_entity_links` via `ExternalEntityLink`.
- Do not add `ExternalSystem`, `ExternalId`, or `SyncedAt` columns to MDM entities.
- For new integrations, set `EntityType = nameof(Entity)`, `ExternalEntity = <source table>`, `ExternalId = <source key>`, and keep `SyncedAt` updated.
- Overwrite cleanup must be driven by `ExternalEntityLink` only.
- Legacy `counterparty_external_links` and per-entity external fields are deprecated and must not be used.
